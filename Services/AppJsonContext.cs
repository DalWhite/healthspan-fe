using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

/// <summary>
/// Source-generated metadata for the hot JSON paths (t_81a400b3). The WASM
/// runtime is interpreted: deserialising the exercise catalog through
/// reflection cost seconds of main-thread time, and the catalog is fetched on
/// every cold library / session open. Source generation emits the readers ahead
/// of time and removes the reflection lookups.
/// Only add types here that are actually hot — everything else keeps the
/// default reflection-based resolver.
/// </summary>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(List<GetExerciseTypeResource>))]
[JsonSerializable(typeof(GetExerciseTypeResource[]))]
internal partial class AppJsonContext : JsonSerializerContext
{
}

public static class ExerciseCatalogJson
{
    /// <summary>
    /// Same origin, same auth handler, same offline cache — the payload is the
    /// slim, paged one (no exercise instructions, no nested variations).
    /// </summary>
    public static string PageUrl(int skip, int take) => $"/ExerciseTypes?fields=minimal&skip={skip}&take={take}";

    public static async Task<(List<GetExerciseTypeResource> Items, int Total)> GetPageAsync(
        HttpClient http, int skip, int take)
    {
        using var response = await http.GetAsync(PageUrl(skip, take));
        response.EnsureSuccessStatusCode();

        var total = response.Headers.TryGetValues("X-Total-Count", out var values)
                    && int.TryParse(values.FirstOrDefault(), out var parsedTotal)
            ? parsedTotal
            : -1;

        await using var stream = await response.Content.ReadAsStreamAsync();
        var items = await JsonSerializer.DeserializeAsync(stream, GetListTypeInfo())
                    ?? new List<GetExerciseTypeResource>();

        return (items, total < 0 ? items.Count : total);
    }

    private static JsonTypeInfo<List<GetExerciseTypeResource>> GetListTypeInfo()
        => (JsonTypeInfo<List<GetExerciseTypeResource>>)AppJsonContext.Default
            .GetTypeInfo(typeof(List<GetExerciseTypeResource>))!;
}
