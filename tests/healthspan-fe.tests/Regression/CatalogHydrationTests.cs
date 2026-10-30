using FluentAssertions;
using Healthspan.Offline;
using RichardSzalay.MockHttp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The catalog is 1.39MB / 979 rows and now arrives in pages (t_81a400b3). These pin
/// what the pagination must keep: every row exactly once, the description that search
/// and the offline mirror read, and a catalog that survives being invalidated while a
/// hydration chunk is in flight.
/// </summary>
public class CatalogHydrationTests
{
    private const int Total = 90;
    private const int FirstPageSize = 40;

    /// <summary>Catalog mirror key the offline handler writes (Offline/OfflineHttpHandler.cs).</summary>
    private const string CatalogKind = "exercisetypes";

    private static string PageJson(int skip, int take, bool full)
    {
        var items = new List<object>(take);
        for (var id = skip + 1; id <= Math.Min(skip + take, Total); id++)
        {
            items.Add(full
                ? new
                {
                    id,
                    name = $"Exercise {id:D3}",
                    description = $"Generated description {id:D3}",
                    parentExerciseTypeId = (int?)null,
                    isFavourite = false,
                    weightType = 0,
                }
                : new
                {
                    id,
                    name = $"Exercise {id:D3}",
                    parentExerciseTypeId = (int?)null,
                    isFavourite = false,
                    weightType = 0,
                });
        }

        return JsonSerializer.Serialize(items);
    }

    /// <summary>
    /// Serves the endpoint the way the API does (page slice + X-Total-Count) and records
    /// the query of every call, so a test can prove the first screen asked for one slim
    /// page instead of the whole catalog.
    /// </summary>
    private sealed class PagedCatalog : IDisposable
    {
        public HttpClient Http { get; }
        public List<string> Queries { get; } = [];

        public PagedCatalog(TimeSpan? chunkDelay = null)
        {
            var mock = new MockHttpMessageHandler();
            mock.When(HttpMethod.Get, "/ExerciseTypes").Respond(async request =>
            {
                var query = request.RequestUri.Query;
                Queries.Add(query);
                var skip = QueryInt(query, "skip");
                var take = QueryInt(query, "take");
                var full = query.Contains("fields=full");

                // Only the background chunks are slow: the paint page must stay fast,
                // so a test cannot pass by quietly waiting for the whole catalog.
                if (full && chunkDelay is { } delay)
                {
                    await Task.Delay(delay);
                }

                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(PageJson(skip, take, full), Encoding.UTF8, "application/json"),
                };
                response.Headers.Add("X-Total-Count", Total.ToString());
                return response;
            });

            Http = mock.ToHttpClient();
            Http.BaseAddress = new Uri("http://localhost/");
        }

        public void Dispose() => Http.Dispose();
    }

    private static int QueryInt(string query, string name)
    {
        foreach (var pair in query.TrimStart('?').Split('&'))
        {
            var parts = pair.Split('=');
            if (parts.Length == 2 && parts[0] == name && int.TryParse(parts[1], out var value))
            {
                return value;
            }
        }

        return 0;
    }

    [Fact]
    public async Task GetExerciseTypesAsync_ReturnsOneScreen_AndHydrationAddsTheRestExactlyOnce()
    {
        using var catalog = new PagedCatalog();
        var state = new AppStateService(catalog.Http);

        var firstCall = await state.GetExerciseTypesAsync();
        catalog.Queries[0].Should().Contain("fields=minimal")
            .And.Contain("skip=0").And.Contain($"take={FirstPageSize}");
        firstCall.Count.Should().BeGreaterThanOrEqualTo(FirstPageSize);
        state.ExerciseTypesTotal.Should().Be(Total);

        var full = await state.GetFullExerciseTypesAsync();
        full.Should().HaveCount(Total);
        full.Select(row => row.Id).Should().OnlyHaveUniqueItems();
        full.Select(row => row.Id).Should().Contain(Enumerable.Range(1, Total));
        state.ExerciseTypesHydrated.Should().BeTrue();
    }

    [Fact]
    public async Task HydrationKeepsDescription_SoSearchByDescriptionStillWorks()
    {
        using var catalog = new PagedCatalog();
        var state = new AppStateService(catalog.Http);

        // The paint page asks for the slim projection on purpose; the description that
        // search and the offline mirror need arrives with the full-projection chunks.
        _ = await state.GetExerciseTypesAsync();
        catalog.Queries[0].Should().Contain("fields=minimal");

        var full = await state.GetFullExerciseTypesAsync();
        full.Single(row => row.Id == 1).Description.Should().Be("Generated description 001");
        full.Single(row => row.Id == Total).Description.Should().Be($"Generated description {Total:D3}");

        AppStateService.MatchesSearch(
            full.Single(row => row.Id == Total),
            "generated description 090").Should().BeTrue();
    }

    [Fact]
    public async Task InvalidateDuringHydration_LeavesNoHolesAndNoDuplicates()
    {
        using var catalog = new PagedCatalog(TimeSpan.FromMilliseconds(250));
        var state = new AppStateService(catalog.Http);

        // Start a load, then invalidate while its chunk is still on the wire: the
        // in-flight chunk must not patch the list this call just cleared.
        _ = state.GetExerciseTypesAsync();
        await Task.Delay(50);
        state.InvalidateExerciseTypes();

        var full = await state.GetFullExerciseTypesAsync();
        full.Select(row => row.Id).Should().OnlyHaveUniqueItems();
        full.Should().HaveCount(Total);
    }

    [Fact]
    public async Task MirroredPagesKeepEveryRowWithItsDescription()
    {
        var store = new InMemoryLocalStore();
        foreach (var skip in new[] { 0, 40 })
        {
            var take = skip == 0 ? FirstPageSize : Total - FirstPageSize;
            await store.SaveCatalogBodyAsync(
                CatalogKind,
                PageJson(skip, take, full: true),
                "id");
        }

        var payloads = await store.GetCatalogPayloadsAsync(CatalogKind);
        payloads.Should().HaveCount(Total);

        // The mirror stores raw API rows; the offline search reads them back.
        var ids = new List<int>(payloads.Count);
        var descriptions = new List<string?>();
        foreach (var payload in payloads)
        {
            using var doc = JsonDocument.Parse(payload);
            ids.Add(doc.RootElement.GetProperty("id").GetInt32());
            descriptions.Add(doc.RootElement.TryGetProperty("description", out var description)
                ? description.GetString()
                : null);
        }

        ids.Should().OnlyHaveUniqueItems();
        ids.Should().Contain(Enumerable.Range(1, Total));
        descriptions.Should().OnlyContain(description => !string.IsNullOrEmpty(description));
    }
}
