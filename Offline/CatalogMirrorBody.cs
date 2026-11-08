using System.Text.Json;

namespace Healthspan.Offline;

/// <summary>
/// Splits a catalog response body into (id, raw payload) rows for the offline
/// mirror. Kept out of the handler so the stores that cannot push the split into
/// the SQLite worker (in-memory, tests) share one implementation.
/// </summary>
internal static class CatalogMirrorBody
{
    public static List<(string Id, string Payload)> Split(string body, string idProperty)
    {
        var items = new List<(string Id, string Payload)>();
        if (string.IsNullOrWhiteSpace(body))
        {
            return items;
        }

        using var doc = JsonDocument.Parse(body);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            return items;
        }

        foreach (var element in doc.RootElement.EnumerateArray())
        {
            var id = element.TryGetProperty(idProperty, out var idElement)
                ? idElement.ToString()
                : Guid.NewGuid().ToString();
            items.Add((id, element.GetRawText()));
        }

        return items;
    }
}
