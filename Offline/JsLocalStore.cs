using System.Buffers;
using System.Text;
using System.Text.Json;
using Microsoft.JSInterop;

namespace Healthspan.Offline;

/// <summary>
/// SQLite-WASM backend: bridges to the persistent OPFS database exposed by
/// wwwroot/js/sqlite/offline-db.js (window.hsdb).
/// </summary>
public sealed class JsLocalStore : SqlLocalStoreBase
{
    private readonly IJSRuntime _js;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private Task<bool>? _opened;

    public JsLocalStore(IJSRuntime js)
    {
        _js = js;
    }

    public override async Task<bool> IsAvailableAsync()
    {
        await _initLock.WaitAsync();
        try
        {
            if (_opened is null || _opened.Status != TaskStatus.RanToCompletion)
            {
                _opened = OpenAsync();
            }

            return await _opened;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private async Task<bool> OpenAsync()
    {
        try
        {
            return await _js.InvokeAsync<bool>("hsdb.ready");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[offline] SQLite WASM unavailable: {ex.Message}");
            return false;
        }
    }

    private async Task EnsureOpenAsync()
    {
        if (!await IsAvailableAsync())
        {
            throw new InvalidOperationException("Local SQLite store is not available.");
        }
    }

    protected override async Task<List<Dictionary<string, string?>>> QueryAsync(
        string sql, params object?[] args)
    {
        await EnsureOpenAsync();
        var json = await _js.InvokeAsync<string>("hsdb.exec", sql, ToParamsJson(args));
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        var rows = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(json)
                   ?? [];
        return rows
            .Select(row => row.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ValueKind == JsonValueKind.Null
                    ? null
                    : pair.Value.ToString()))
            .ToList();
    }

    protected override async Task<LocalStoreChanges> ExecuteAsync(
        string sql, params object?[] args)
    {
        await EnsureOpenAsync();
        var result = await _js.InvokeAsync<ChangesResult>(
            "hsdb.changes", sql, ToParamsJson(args));
        return new LocalStoreChanges(result.Changes, result.LastInsertRowId);
    }

    protected override async Task ExecuteBatchAsync(
        IEnumerable<(string Sql, object?[] Args)> statements)
    {
        await EnsureOpenAsync();

        // Hand-written JSON instead of JsonSerializer: the reflection-based writer
        // over anonymous types with object[] binds cost ~2s for a 160-row catalog
        // page in interpreted WASM, and it ran on the main thread (t_81a400b3).
        var buffer = new ArrayBufferWriter<byte>(64 * 1024);
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartArray();
            foreach (var (sql, args) in statements)
            {
                writer.WriteStartObject();
                writer.WriteString("sql", sql);
                writer.WriteStartArray("bind");
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case null:
                            writer.WriteNullValue();
                            break;
                        case bool flag:
                            writer.WriteNumberValue(flag ? 1 : 0);
                            break;
                        case int number:
                            writer.WriteNumberValue(number);
                            break;
                        case long number:
                            writer.WriteNumberValue(number);
                            break;
                        case double number:
                            writer.WriteNumberValue(number);
                            break;
                        default:
                            writer.WriteStringValue(arg.ToString());
                            break;
                    }
                }

                writer.WriteEndArray();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
        }

        var payload = Encoding.UTF8.GetString(buffer.WrittenSpan);
        await _js.InvokeAsync<ChangesResult>("hsdb.tx", payload);
    }

    private static string ToParamsJson(object?[] args)
        => JsonSerializer.Serialize(
            args.Select(a => a switch
            {
                null => null,
                bool b => b ? 1 : 0,
                _ => a
            }));

    private sealed record ChangesResult(int Changes, long LastInsertRowId);
}
