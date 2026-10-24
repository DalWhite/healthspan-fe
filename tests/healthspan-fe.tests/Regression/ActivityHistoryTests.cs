using System.Net;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Wellbeing;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// History page (t_ce09f2ab) against ux-spec-historial-actividades-t_70d5d147:
/// server-side filters/pagination via GET /api/activities query params, day
/// grouping, chip state reflecting the applied filter, empty/error states,
/// confirm-sheet delete with a single-use 6 s "Deshacer" (re-POST), and the
/// 404 "ya no existe" path.
/// </summary>
public class ActivityHistoryTests : BlazorTestBase
{
    private static readonly Guid FirstActivityId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private List<StoreItem> _store = new();
    private readonly List<string> _queries = new();
    private readonly List<string> _undoPosts = new();
    private string? _deletePath;
    private HttpStatusCode _deleteStatus = HttpStatusCode.NoContent;
    private int _getCalls;
    private HttpStatusCode _firstGetStatus = HttpStatusCode.OK;

    private sealed record StoreItem(string Date, string Type, string Dimension, string Json);

    private static string DateIso(int daysAgo)
        => DateTime.Today.AddDays(-daysAgo).ToString("yyyy-MM-dd");

    private static string N(int? value) => value?.ToString() ?? "null";

    private static string ItemJson(
        Guid id,
        string type,
        string dimension,
        int daysAgo,
        int duration = 20,
        int? temperature = null,
        int? cycles = null,
        int? intensity = null,
        string? notes = null,
        string? metadata = null)
    {
        var meta = metadata ?? "null";
        return $$"""
                 {
                   "id": "{{id}}",
                   "type": "{{type}}",
                   "dimension": "{{dimension}}",
                   "occurredOnUtc": "{{DateIso(daysAgo)}}T00:00:00Z",
                   "durationMinutes": {{duration}},
                   "frequencyPerWeek": {{N(intensity is null ? null : 3)}},
                   "intensity": {{N(intensity)}},
                   "temperatureCelsius": {{N(temperature)}},
                   "cycles": {{N(cycles)}},
                   "notes": {{(notes is null ? "null" : $"\"{notes}\"")}},
                   "metadata": {{meta}},
                   "createdAtUtc": "2026-10-20T10:00:00Z"
                 }
                 """;
    }

    /// <summary>22 items mirroring the prototype seed: 2 today, 2 yesterday, rest older.</summary>
    private void BuildSeedStore()
    {
        var offsets = new[] { 0, 0, 1, 1, 2, 3, 4, 5, 6, 7, 9, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30 };
        var cycle = new (string Type, string Dim, int Dur, int? Temp, int? Cycles, int? Intensity, string? Notes, string? Meta)[]
        {
            ("sauna", "health", 20, 85, 3, 3, "Sesión post-entreno", null),
            ("wim_hof", "health", 15, null, 4, null, null, """{"breathsPerRound":35}"""),
            ("meditation", "health", 10, null, null, null, null, """{"style":"Guiada"}"""),
            ("cold_exposure", "health", 5, 8, null, 4, null, null),
            ("other", "strength", 45, null, null, null, null, """{"name":"Peso libre","series":"5x5"}"""),
        };

        _store = new List<StoreItem>();
        for (var i = 0; i < offsets.Length; i++)
        {
            var (type, dimension, duration, temperature, cycles, intensity, notes, metadata) = cycle[i % cycle.Length];
            var id = i == 0 ? FirstActivityId : Guid.NewGuid();
            var json = ItemJson(id, type, dimension, offsets[i], duration, temperature, cycles, intensity, notes, metadata);
            _store.Add(new StoreItem(DateIso(offsets[i]), type, dimension, json));
        }
    }

    private void MockActivitiesList()
    {
        MockHttp.When(HttpMethod.Get, "/api/activities*")
            .Respond(req =>
            {
                _getCalls++;
                var query = ParseQuery(req.RequestUri?.Query);
                _queries.Add(req.RequestUri!.Query);

                if (_getCalls == 1 && _firstGetStatus != HttpStatusCode.OK)
                {
                    return new HttpResponseMessage(_firstGetStatus)
                    {
                        Content = new StringContent(
                            """{"title":"An error occurred.","status":500}""",
                            Encoding.UTF8,
                            "application/problem+json"),
                    };
                }

                var type = query.GetValueOrDefault("type");
                var dimension = query.GetValueOrDefault("dimension");
                var from = query.GetValueOrDefault("from");
                var to = query.GetValueOrDefault("to");
                var page = int.Parse(query.GetValueOrDefault("page", "1"));
                var pageSize = int.Parse(query.GetValueOrDefault("pageSize", "10"));

                var filtered = _store
                    .Where(i => (type is null || i.Type == type)
                        && (dimension is null || i.Dimension == dimension)
                        && (from is null || string.CompareOrdinal(i.Date, from) >= 0)
                        && (to is null || string.CompareOrdinal(i.Date, to) <= 0))
                    .ToList();

                var pageItems = filtered.Skip((page - 1) * pageSize).Take(pageSize).Select(i => i.Json);
                var hasMore = filtered.Count > page * pageSize;
                var body = $$"""
                             {"items":[{{string.Join(",", pageItems)}}],"total":{{filtered.Count}},"page":{{page}},"pageSize":{{pageSize}},"hasMore":{{(hasMore ? "true" : "false")}}}
                             """;

                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(body, Encoding.UTF8, "application/json"),
                };
            });
    }

    private void MockActivitiesDelete()
    {
        MockHttp.When(HttpMethod.Delete, "/api/activities/*")
            .Respond(req =>
            {
                _deletePath = req.RequestUri?.AbsolutePath;
                var response = new HttpResponseMessage(_deleteStatus);
                if (_deleteStatus == HttpStatusCode.NotFound)
                {
                    response.Content = new StringContent(
                        """{"title":"The requested health record was not found.","status":404,"code":"activity_not_found"}""",
                        Encoding.UTF8,
                        "application/problem+json");
                }

                return response;
            });
    }

    private void MockActivitiesPost()
    {
        MockHttp.When(HttpMethod.Post, "/api/activities")
            .Respond(req =>
            {
                _undoPosts.Add(req.Content?.ReadAsStringAsync().GetAwaiter().GetResult() ?? string.Empty);
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent(ItemJson(Guid.NewGuid(), "sauna", "health", 0, 20, 85, 3, 3, "Sesión post-entreno"), Encoding.UTF8, "application/json"),
                };
            });
    }

    private static Dictionary<string, string> ParseQuery(string? query)
        => (query ?? string.Empty)
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .ToDictionary(parts => parts[0], parts => parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty);

    private IRenderedComponent<ActivityHistory> Render() => RenderComponent<ActivityHistory>();

    private static void WaitForLoaded(IRenderedComponent<ActivityHistory> cut)
        => cut.WaitForElements(".hs-hist-row", timeout: TimeSpan.FromSeconds(5));

    /// <summary>Blazor renders twice per async handler (sync part + completion);
    /// wait until the loading skeleton is gone so assertions see the final state.</summary>
    private static void WaitForIdle(IRenderedComponent<ActivityHistory> cut)
        => cut.WaitForState(() => cut.FindAll(".hs-hist-skel").Count == 0, TimeSpan.FromSeconds(5));

    private AngleSharp.Dom.IElement Chip(IRenderedComponent<ActivityHistory> cut, string label)
        => cut.FindAll(".hs-hist-chip").First(chip => chip.TextContent.Trim() == label);

    // ── Initial load + day grouping + pagination ──────────────────────────

    [Fact]
    public void InitialLoad_GroupsByDayAndShowsPaginationHint()
    {
        BuildSeedStore();
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        cut.FindAll(".hs-hist-row").Count.Should().Be(10);
        cut.FindAll(".hs-hist-day-title").Select(h => h.TextContent.Trim()).Take(2)
            .Should().ContainInOrder("Hoy", "Ayer");
        cut.Find(".hs-hist-hint").TextContent.Should().Contain("Mostrando 10 de 22");
        cut.FindAll("#hist-loadmore").Count.Should().Be(1);

        // Row anatomy: title + dimension badge + param badges + duration (spec §4).
        var firstRow = cut.Find(".hs-hist-row");
        firstRow.QuerySelector(".hs-hist-row-title")!.TextContent.Should().Be("Sauna");
        firstRow.QuerySelector(".hs-hist-badge-dim-salud")!.TextContent.Should().Be("Salud");
        firstRow.QuerySelector(".hs-hist-dur")!.TextContent.Should().Be("20 min");
        firstRow.GetAttribute("aria-label").Should().Contain("Sauna").And.Contain("Hoy").And.Contain("20 minutos");
    }

    [Fact]
    public void LoadMore_AppendsPage2AndHidesButtonWhenExhausted()
    {
        BuildSeedStore();
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        // 22 items at pageSize 10 → three pages: 10 + 10 + 2.
        cut.Find("#hist-loadmore").Click();
        cut.WaitForElements(".hs-hist-row", 20, timeout: TimeSpan.FromSeconds(5));
        _queries.Should().Contain(q => q.Contains("page=2"));

        cut.Find("#hist-loadmore").Click();
        cut.WaitForElements(".hs-hist-row", 22, timeout: TimeSpan.FromSeconds(5));

        cut.FindAll(".hs-hist-row").Count.Should().Be(22);
        cut.Find(".hs-hist-hint").TextContent.Should().Contain("Mostrando 22 de 22");
        cut.FindAll("#hist-loadmore").Should().BeEmpty();
        _queries.Should().Contain(q => q.Contains("page=3"));
    }

    [Fact]
    public void SkeletonShownWhileInitialLoadInFlight()
    {
        BuildSeedStore();
        MockHttp.When(HttpMethod.Get, "/api/activities*")
            .Respond(async req =>
            {
                await Task.Delay(150);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        $$"""{"items":[{{string.Join(",", _store.Take(10).Select(i => i.Json))}}],"total":10,"page":1,"pageSize":10,"hasMore":false}""",
                        Encoding.UTF8,
                        "application/json"),
                };
            });

        var cut = Render();
        cut.FindAll(".hs-hist-skel").Count.Should().Be(4);
        WaitForLoaded(cut);
    }

    // ── Filters (server-side, chips reflect state) ────────────────────────

    [Fact]
    public void Chips_QueryBackendCombinedAndReflectAppliedState()
    {
        BuildSeedStore();
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        Chip(cut, "Sauna").Click();
        WaitForIdle(cut);

        _queries[^1].Should().Contain("type=sauna").And.Contain("page=1");
        Chip(cut, "Sauna").GetAttribute("aria-pressed").Should().Be("true");
        Chip(cut, "Todos").GetAttribute("aria-pressed").Should().Be("false");

        Chip(cut, "Salud").Click();
        WaitForIdle(cut);
        _queries[^1].Should().Contain("type=sauna").And.Contain("dimension=health");

        cut.Find(".hs-hist-count").TextContent.Should().Contain("con los filtros aplicados");
        cut.FindAll("#hist-clear").Count.Should().Be(1);

        // Date range travels as from/to query params too (spec §3/§6).
        cut.FindAll(".hs-hist-date-field input")[0].Change(DateIso(10));
        WaitForIdle(cut);
        _queries[^1].Should().Contain($"from={DateIso(10)}");

        // Clear resets everything.
        cut.Find("#hist-clear").Click();
        WaitForIdle(cut);
        _queries[^1].Should().NotContain("type=").And.NotContain("dimension=").And.NotContain("from=");
    }

    [Fact]
    public void EmptyGlobal_ShowsCtaToRegister()
    {
        _store = new List<StoreItem>();
        MockActivitiesList();

        var cut = Render();
        cut.WaitForState(() => cut.FindAll(".hs-hist-empty").Count > 0, TimeSpan.FromSeconds(5));

        cut.Find(".hs-hist-empty-title").TextContent.Should().Contain("Aún no has registrado actividades");
        cut.Find("a.hs-hist-empty-cta").GetAttribute("href").Should().Contain("wellbeing/register");
    }

    [Fact]
    public void EmptyWithFilters_ExplainsAndOffersClear()
    {
        // Store with only sauna items: the Meditación chip must match nothing.
        _store = Enumerable.Range(0, 5)
            .Select(i => new StoreItem(DateIso(i), "sauna", "health", ItemJson(Guid.NewGuid(), "sauna", "health", i, 20, 85, 3, 3)))
            .ToList();
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        Chip(cut, "Meditación").Click();
        cut.WaitForState(() => cut.FindAll(".hs-hist-empty").Count > 0, TimeSpan.FromSeconds(5));

        cut.Find(".hs-hist-empty-title").TextContent.Should().Contain("Ningún resultado con estos filtros");
        cut.FindAll(".hs-hist-empty .hs-hist-clear").Count.Should().Be(1);
    }

    [Fact]
    public void LoadFailure_ShowsBannerAndRetryReloads()
    {
        BuildSeedStore();
        _firstGetStatus = HttpStatusCode.InternalServerError;
        MockActivitiesList();

        var cut = Render();
        cut.WaitForState(() => cut.FindAll(".hs-hist-banner").Count > 0, TimeSpan.FromSeconds(5));

        cut.Find(".hs-hist-banner").GetAttribute("role").Should().Be("alert");
        cut.Find(".hs-hist-banner-text").TextContent.Should().Contain("No se pudo cargar el historial");
        cut.FindAll(".hs-hist-row").Should().BeEmpty();

        cut.Find("#hist-retry").Click();
        WaitForLoaded(cut);
        _getCalls.Should().Be(2);
    }

    // ── Detail sheet + delete + single-use undo ───────────────────────────

    [Fact]
    public void RowOpensDetailSheetWithKeyValues()
    {
        BuildSeedStore();
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        cut.Find(".hs-hist-row").Click();
        cut.WaitForState(() => cut.FindAll(".hs-hist-sheet").Count > 0, TimeSpan.FromSeconds(5));

        var sheet = cut.Find(".hs-hist-sheet");
        sheet.GetAttribute("role").Should().Be("dialog");
        sheet.GetAttribute("aria-modal").Should().Be("true");
        sheet.QuerySelector("#hist-detail-title")!.TextContent.Should().Contain("Sauna");

        var kv = cut.Find(".hs-hist-kv").TextContent;
        kv.Should().Contain("Dimensión").And.Contain("Salud")
            .And.Contain("Duración").And.Contain("20 min")
            .And.Contain("Temperatura").And.Contain("85 °C")
            .And.Contain("Ciclos").And.Contain("Intensidad").And.Contain("3 / 5");
        cut.Find(".hs-hist-notes").TextContent.Should().Contain("Sesión post-entreno");
    }

    [Fact]
    public void Delete_ConfirmSheetToastAndSingleUseUndo()
    {
        BuildSeedStore();
        MockActivitiesList();
        MockActivitiesDelete();
        MockActivitiesPost();

        var cut = Render();
        WaitForLoaded(cut);

        cut.Find(".hs-hist-row").Click();
        cut.WaitForState(() => cut.FindAll("#hist-detail-delete").Count > 0, TimeSpan.FromSeconds(5));
        cut.Find("#hist-detail-delete").Click();

        cut.WaitForState(() => cut.FindAll("#hist-confirm-yes").Count > 0, TimeSpan.FromSeconds(5));
        cut.Find("#hist-confirm-yes").Click();

        cut.WaitForState(() => cut.FindAll(".hs-hist-toast").Count > 0, TimeSpan.FromSeconds(5));
        _deletePath.Should().Be($"/api/activities/{FirstActivityId}");
        cut.Find(".hs-hist-toast").GetAttribute("role").Should().Be("status");
        cut.Find(".hs-hist-toast").TextContent.Should().Contain("«Sauna» eliminada");
        cut.FindAll("#hist-toast-undo").Count.Should().Be(1);

        // Undo re-POSTs the deleted activity with the same payload (spec §5/§6).
        cut.Find("#hist-toast-undo").Click();
        cut.WaitForState(
            () => _undoPosts.Count == 1 && cut.FindAll(".hs-hist-toast").Any(t => t.TextContent.Contains("Registro restaurado")),
            TimeSpan.FromSeconds(5));

        var posted = JsonSerializer.Deserialize<JsonElement>(_undoPosts[0]);
        posted.GetProperty("type").GetString().Should().Be("sauna");
        posted.GetProperty("dimension").GetString().Should().Be("health");
        posted.GetProperty("durationMinutes").GetInt32().Should().Be(20);
        posted.GetProperty("temperatureCelsius").GetInt32().Should().Be(85);
        posted.GetProperty("cycles").GetInt32().Should().Be(3);
        posted.GetProperty("notes").GetString().Should().Be("Sesión post-entreno");

        cut.Find(".hs-hist-toast").TextContent.Should().Contain("Registro restaurado");

        // Single slot: the button is gone and a second click must not duplicate.
        cut.FindAll("#hist-toast-undo").Should().BeEmpty();
        _undoPosts.Count.Should().Be(1);
    }

    [Fact]
    public void Delete404_ToastSaysGoneAndNoUndoOffered()
    {
        BuildSeedStore();
        _deleteStatus = HttpStatusCode.NotFound;
        MockActivitiesList();
        MockActivitiesDelete();
        MockActivitiesPost();

        var cut = Render();
        WaitForLoaded(cut);

        cut.Find(".hs-hist-row").Click();
        cut.WaitForState(() => cut.FindAll("#hist-detail-delete").Count > 0, TimeSpan.FromSeconds(5));
        cut.Find("#hist-detail-delete").Click();
        cut.WaitForState(() => cut.FindAll("#hist-confirm-yes").Count > 0, TimeSpan.FromSeconds(5));
        cut.Find("#hist-confirm-yes").Click();

        cut.WaitForState(() => cut.FindAll(".hs-hist-toast").Count > 0, TimeSpan.FromSeconds(5));
        cut.Find(".hs-hist-toast").TextContent.Should().Contain("ya no existe en tu historial");
        cut.FindAll("#hist-toast-undo").Should().BeEmpty();
        _undoPosts.Should().BeEmpty();
    }

    // ── Robustness (spec §6) ──────────────────────────────────────────────

    [Fact]
    public void UnknownType_RendersRawTypeAsTitleWithoutCrashing()
    {
        _store = new List<StoreItem>
        {
            new(DateIso(0), "yoga_nova", "mobility", ItemJson(Guid.NewGuid(), "yoga_nova", "mobility", 0, 30)),
        };
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        cut.Find(".hs-hist-row-title").TextContent.Should().Be("yoga_nova");
    }

    [Fact]
    public void OtherActivity_UsesMetadataNameAsTitle()
    {
        _store = new List<StoreItem>
        {
            new(DateIso(0), "other", "strength", ItemJson(Guid.NewGuid(), "other", "strength", 0, 45, metadata: """{"name":"Kettlebell"}""")),
        };
        MockActivitiesList();

        var cut = Render();
        WaitForLoaded(cut);

        cut.Find(".hs-hist-row-title").TextContent.Should().Be("Kettlebell");
    }

}
