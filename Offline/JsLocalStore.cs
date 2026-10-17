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
        var payload = JsonSerializer.Serialize(
            statements.Select(s => new
            {
                sql = s.Sql,
                bind = s.Args.Select(a => a switch
                {
                    null => null,
                    bool b => b ? 1 : 0,
                    _ => a
                }).ToArray()
            }));
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
