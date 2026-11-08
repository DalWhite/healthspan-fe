using FluentAssertions;
using Healthspan.Offline;
using RichardSzalay.MockHttp;
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The hydrated catalog mirror is only useful offline if it survives a cold
/// start mid-hydration: chunks must be durable, progress/cancellation must be
/// persisted next to them, and invalidation must never leave a stale 'complete'
/// over an empty mirror. These pin that storage contract (mirror_meta).
/// </summary>
public class OfflineMirrorPersistenceTests
{
    private const string Kind = "exercise_types";

    private static OfflineHttpHandler BuildHandler(InMemoryLocalStore store, HttpMessageHandler inner)
    {
        var status = new SyncStatusService();
        var mutator = new LocalMutator(store, status);
        return new OfflineHttpHandler(store, mutator, status, new SyncCoordinator());
    }

    private static string Page(int skip, int take)
    {
        var items = new System.Text.StringBuilder("[");
        for (var id = skip + 1; id <= skip + take; id++)
        {
            if (id > skip + 1) items.Append(',');
            items.Append($"{{\"id\":{id},\"name\":\"Exercise {id:D3}\",\"description\":\"d {id}\"}}");
        }
        items.Append(']');
        return items.ToString();
    }

    /// <summary>Serves /ExerciseTypes pages with X-Total-Count, like the API.</summary>
    private static MockHttpMessageHandler PagedApi(int total, int failAfterSkip = int.MaxValue)
    {
        var mock = new MockHttpMessageHandler();
        mock.When(HttpMethod.Get, "/ExerciseTypes").Respond(request =>
        {
            var query = request.RequestUri.Query;
            var skip = QueryInt(query, "skip");
            var take = QueryInt(query, "take");
            if (skip >= failAfterSkip)
            {
                // Synchronous throw: MockHttp never completes a callback that
                // returns a faulted Task, so the request would hang until the
                // HttpClient timeout instead of failing fast.
                throw new HttpRequestException("network dropped mid-hydration");
            }

            var takeActual = Math.Min(take, total - skip);
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    Page(skip, takeActual), System.Text.Encoding.UTF8, "application/json"),
            };
            response.Headers.Add("X-Total-Count", total.ToString());
            return Task.FromResult(response);
        });
        return mock;
    }

    private static int QueryInt(string query, string name)
    {
        foreach (var part in query.TrimStart('?').Split('&'))
        {
            var pair = part.Split('=');
            if (pair.Length == 2 && pair[0] == name && int.TryParse(pair[1], out var v))
            {
                return v;
            }
        }
        return 0;
    }

    private static async Task<HttpResponseMessage> GetAsync(
        OfflineHttpHandler handler, HttpMessageHandler inner, int skip, int take)
    {
        var response = await handler.ProcessAsync(
            new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7278/ExerciseTypes?skip={skip}&take={take}"),
            inner,
            CancellationToken.None);
        // Mirror writes are detached from the request path; wait for them.
        await handler.LastCacheWrite;
        return response;
    }

    // ── chunk persistence + observational progress ────────────────────────────

    [Fact]
    public async Task Handler_PersistsChunksAndProgress_AsHydrationAdvances()
    {
        var store = new InMemoryLocalStore();
        var api = PagedApi(total: 90);

        var handler = BuildHandler(store, api);
        await GetAsync(handler, api, skip: 0, take: 40);

        var afterFirst = await store.GetMirrorStateAsync(Kind);
        afterFirst!.Status.Should().Be(OfflineMirrorState.Hydrating,
            "a partial mirror is not a finished snapshot");
        afterFirst.HydratedThrough.Should().Be(40);
        afterFirst.TargetTotal.Should().Be(90);
        (await store.CountCatalogAsync(Kind)).Should().Be(40, "the fetched chunk is durable NOW, not at the end");

        await GetAsync(handler, api, skip: 40, take: 40);
        var afterSecond = await store.GetMirrorStateAsync(Kind);
        afterSecond!.Status.Should().Be(OfflineMirrorState.Hydrating);
        afterSecond.HydratedThrough.Should().Be(80);

        await GetAsync(handler, api, skip: 80, take: 40);
        await handler.LastCacheWrite;
        var done = await store.GetMirrorStateAsync(Kind);
        done!.Status.Should().Be(OfflineMirrorState.Complete);
        done.HydratedThrough.Should().Be(90);
        (await store.CountCatalogAsync(Kind)).Should().Be(90);
    }

    [Fact]
    public async Task NetworkDrop_MidHydration_KeepsChunksAndProgressConsistent()
    {
        var store = new InMemoryLocalStore();
        var api = PagedApi(total: 90, failAfterSkip: 80);

        var handler = BuildHandler(store, api);
        await GetAsync(handler, api, skip: 0, take: 40);
        await GetAsync(handler, api, skip: 40, take: 40);
        // The third page's network read fails; the handler answers from its
        // response cache or offline status. Either way it must NOT touch the
        // mirror with data it never received.
        await GetAsync(handler, api, skip: 80, take: 40);

        // The two delivered chunks are intact; progress never ran ahead of them.
        (await store.CountCatalogAsync(Kind)).Should().Be(80);
        var state = await store.GetMirrorStateAsync(Kind);
        state!.Status.Should().Be(OfflineMirrorState.Hydrating);
        state.HydratedThrough.Should().Be(80,
            "a resume must not skip rows the dropped chunk never wrote");
    }

    [Fact]
    public async Task ColdStart_ResumesFromPersistedPausedState_WithoutLosingMirror()
    {
        var store = new InMemoryLocalStore();

        // Session 1 hydrates one chunk, then the user cancels: the scheduler
        // persists 'paused' after the chunk write (contract t_780be3a9).
        var api = PagedApi(total: 90);
        var handler = BuildHandler(store, api);
        await GetAsync(handler, api, skip: 0, take: 40);
        await store.SaveMirrorStateAsync(new OfflineMirrorState
        {
            Kind = Kind,
            Status = OfflineMirrorState.Paused,
            TargetTotal = 90,
            HydratedThrough = 40,
        });

        // Session 2 (cold start): state and rows are still there and agree.
        var resumed = await store.GetMirrorStateAsync(Kind);
        resumed!.Status.Should().Be(OfflineMirrorState.Paused);
        resumed.HydratedThrough.Should().Be(40);
        (await store.CountCatalogAsync(Kind)).Should().Be(40);

        // A resuming fetch starting at 40 completes the mirror with no dupes.
        var api2 = PagedApi(total: 90);
        var handler2 = BuildHandler(store, api2);
        await GetAsync(handler2, api2, skip: 40, take: 50);
        (await store.CountCatalogAsync(Kind)).Should().Be(90);
        var done = await store.GetMirrorStateAsync(Kind);
        done!.Status.Should().Be(OfflineMirrorState.Complete);
    }

    [Fact]
    public async Task Invalidate_ClearsRowsAndStateTogether()
    {
        var store = new InMemoryLocalStore();
        var api = PagedApi(total: 90);
        var handler = BuildHandler(store, api);
        await GetAsync(handler, api, skip: 0, take: 40);

        await store.InvalidateCatalogAsync(Kind);

        (await store.HasCatalogAsync(Kind)).Should().BeFalse();
        (await store.GetMirrorStateAsync(Kind)).Should().BeNull(
            "a stale complete/paused state over an empty mirror would poison the next cold start");
    }

    [Fact]
    public async Task Handler_Heals_LegacyMirror_ThatHasRowsButNoState()
    {
        var store = new InMemoryLocalStore();
        // Mirror written by an older build (no mirror_meta existed).
        await store.SaveCatalogAsync(Kind,
            [(("1"), "{\"id\":1}"), (("2"), "{\"id\":2}")]);

        var api = PagedApi(total: 2);
        var handler = BuildHandler(store, api);
        await GetAsync(handler, api, skip: 0, take: 2);

        var state = await store.GetMirrorStateAsync(Kind);
        state!.Status.Should().Be(OfflineMirrorState.Complete,
            "the rows prove the snapshot is done; refetching 1.4MB to discover that is waste");
    }

    [Fact]
    public async Task SaveMirrorState_IsAnUpsertPerKind()
    {
        var store = new InMemoryLocalStore();
        await store.SaveMirrorStateAsync(new OfflineMirrorState
        {
            Kind = Kind, Status = OfflineMirrorState.Hydrating, TargetTotal = 90, HydratedThrough = 40,
        });
        await store.SaveMirrorStateAsync(new OfflineMirrorState
        {
            Kind = Kind, Status = OfflineMirrorState.Paused, TargetTotal = 90, HydratedThrough = 40,
        });

        var state = await store.GetMirrorStateAsync(Kind);
        state!.Status.Should().Be(OfflineMirrorState.Paused);
        (await store.GetMirrorStateAsync("muscles")).Should().BeNull();
    }
}
