using Bunit;
using healthspan_fe_wasm_pwa.Components.Training;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using System.Globalization;
using System.Net;
using System.Net.Http;
using Xunit;
using healthspan_fe.tests.Infrastructure;

namespace healthspan_fe.tests.Regression;

public class DashboardTrainingPanelsTests : BlazorTestBase
{
    private static readonly string[] CanonicalShortNames =
    [
        "Habilidad", "Velocidad", "Potencia", "Fuerza", "Hipertrofia",
        "Resistencia muscular", "Anaeróbica", "Aeróbica máxima", "Larga duración"
    ];

    [Fact]
    public void Dashboard_RendersCoverageRadar_WithCanonicalAxesAndLegend()
    {
        MockHttp.When("/Dashboard/summary").Respond("application/json", "{}");
        MockHttp.When("/BloodTests").Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest").Respond("application/json", "[]");
        MockHttp.When("/HealthProfile").Respond("application/json", "null");
        MockHttp.When(CurrentCoverageUrl).Respond("application/json", CoverageJson(CoverageItems(
            ("Skill", 1, 2, 0.5), ("Speed", 0, 1, 0.0), ("Power", 0, 1, 0.0),
            ("Strength", 2, 2, 1.0), ("Hypertrophy", 1, 2, 0.5),
            ("MuscularEndurance", 2, 2, 1.0), ("AnaerobicCapacity", 0, 1, 0.0),
            ("MaximalAerobicPower", 1, 1, 1.0), ("LongDurationEndurance", 0, 2, 0.0))));
        MockHttp.When(PreviousCoverageUrl).Respond("application/json", CoverageJson(CoverageItems(
            ("Skill", 0, 2, 0.0), ("Speed", 1, 1, 1.0), ("Power", 1, 1, 1.0),
            ("Strength", 1, 2, 0.5), ("Hypertrophy", 2, 2, 1.0),
            ("MuscularEndurance", 1, 2, 0.5), ("AnaerobicCapacity", 1, 1, 1.0),
            ("MaximalAerobicPower", 0, 1, 0.0), ("LongDurationEndurance", 2, 2, 1.0))));
        MockHttp.When("/Readiness").Respond("application/json", ReadinessJson(
            6.4, 2.1, 5,
            ("Strength", "Arrastra 6 días sin entrenar fuerza"),
            ("Speed", "Arrastra 5 días sin entrenar velocidad")));

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            // Long axis names wrap into tspans; join them so the label reads as one line.
            var labels = cut.FindAll(".hs-coverage-radar-axis-label")
                .Select(node => string.Join(" ", node.QuerySelectorAll("tspan")
                    .Select(span => span.TextContent.Trim())))
                .ToList();

            Assert.Equal(CanonicalShortNames, labels);
            Assert.Single(cut.FindAll(".hs-coverage-radar-current"));
            Assert.Single(cut.FindAll(".hs-coverage-radar-previous"));
            Assert.Single(cut.FindAll(".hs-coverage-radar-target-ring"));
            Assert.Equal(3, cut.FindAll(".hs-coverage-radar-ring").Count);

            Assert.Contains("Esta semana", cut.Markup);
            Assert.Contains("Semana anterior", cut.Markup);
            Assert.Contains("Objetivo", cut.Markup);
            Assert.Contains("3 de 9 ejes en objetivo · más débil: Velocidad (0/1)",
                cut.Find(".hs-coverage-radar-summary").TextContent);
        });
    }

    [Fact]
    public void Dashboard_RadarOmitsPreviousWeek_WhenPreviousCoverageIsEmpty()
    {
        MockHttp.When("/Dashboard/summary").Respond("application/json", "{}");
        MockHttp.When("/BloodTests").Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest").Respond("application/json", "[]");
        MockHttp.When("/HealthProfile").Respond("application/json", "null");
        MockHttp.When(CurrentCoverageUrl).Respond("application/json", CoverageJson(CoverageItems(
            ("Skill", 2, 2, 1.0), ("Speed", 1, 1, 1.0), ("Power", 1, 1, 1.0),
            ("Strength", 2, 2, 1.0), ("Hypertrophy", 2, 2, 1.0),
            ("MuscularEndurance", 2, 2, 1.0), ("AnaerobicCapacity", 1, 1, 1.0),
            ("MaximalAerobicPower", 1, 1, 1.0), ("LongDurationEndurance", 2, 2, 1.0))));
        MockHttp.When(PreviousCoverageUrl).Respond("application/json", CoverageJson([]));
        MockHttp.When("/Readiness").Respond("application/json", ReadinessJson(8.1, 1.4, 6));

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal(9, cut.FindAll(".hs-coverage-radar-axis-label").Count);
            Assert.Empty(cut.FindAll(".hs-coverage-radar-previous"));
            Assert.DoesNotContain("Semana anterior", cut.Markup);
            Assert.Contains("Los 9 ejes en su objetivo",
                cut.Find(".hs-coverage-radar-summary").TextContent);
        });
    }

    [Fact]
    public void Dashboard_ReadinessCard_ShowsScoreStatusAndTopTwoSuggestions()
    {
        MockHttp.When("/Dashboard/summary").Respond("application/json", "{}");
        MockHttp.When("/BloodTests").Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest").Respond("application/json", "[]");
        MockHttp.When("/HealthProfile").Respond("application/json", "null");
        MockHttp.When(CurrentCoverageUrl).Respond("application/json", CoverageJson(CoverageItems(
            ("Skill", 0, 2, 0.0), ("Speed", 0, 1, 0.0), ("Power", 0, 1, 0.0),
            ("Strength", 0, 2, 0.0), ("Hypertrophy", 0, 2, 0.0),
            ("MuscularEndurance", 1, 2, 0.5), ("AnaerobicCapacity", 1, 1, 1.0),
            ("MaximalAerobicPower", 1, 1, 1.0), ("LongDurationEndurance", 1, 2, 0.5))));
        MockHttp.When(PreviousCoverageUrl).Respond("application/json", CoverageJson([]));
        MockHttp.When("/Readiness").Respond("application/json", ReadinessJson(
            4.2, 3.6, 4,
            ("Strength", "Arrastra 6 días sin entrenar fuerza"),
            ("Hypertrophy", "Arrastra 5 días sin entrenar hipertrofia"),
            ("Skill", "Arrastra 4 días sin entrenar habilidad")));

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Equal("4.2", cut.Find(".hs-readiness-score strong").TextContent.Trim());
            Assert.Contains("Entrenar con cabeza", cut.Markup);

            var suggestions = cut.FindAll(".hs-readiness-suggestion");
            Assert.Equal(2, suggestions.Count);
            Assert.Contains("Fuerza", suggestions[0].TextContent);
            Assert.Contains("Arrastra 6 días sin entrenar fuerza", suggestions[0].TextContent);
            Assert.Contains("Hipertrofia", suggestions[1].TextContent);

            var why = cut.Find(".hs-readiness-why").TextContent;
            Assert.Contains("Por qué:", why);
            Assert.Contains("fatiga 3.6/5", why);
            Assert.Contains("4 sesiones en 7 días", why);
            Assert.Contains("Habilidad y Velocidad por debajo del objetivo", why);
        });
    }

    [Fact]
    public void Dashboard_TrainingPanels_ShowEmptyStateCta_WhenNothingIsLogged()
    {
        MockHttp.When("/Dashboard/summary").Respond("application/json", "{}");
        MockHttp.When("/BloodTests").Respond("application/json", "[]");
        MockHttp.When("/BloodTests/markers/latest").Respond("application/json", "[]");
        MockHttp.When("/HealthProfile").Respond("application/json", "null");
        MockHttp.When(CurrentCoverageUrl).Respond("application/json", CoverageJson(CoverageItems(
            ("Skill", 0, 2, 0.0), ("Speed", 0, 1, 0.0), ("Power", 0, 1, 0.0),
            ("Strength", 0, 2, 0.0), ("Hypertrophy", 0, 2, 0.0),
            ("MuscularEndurance", 0, 2, 0.0), ("AnaerobicCapacity", 0, 1, 0.0),
            ("MaximalAerobicPower", 0, 1, 0.0), ("LongDurationEndurance", 0, 2, 0.0)), sessions: 0));
        MockHttp.When(PreviousCoverageUrl).Respond("application/json", CoverageJson([]));
        MockHttp.When("/Readiness").Respond("application/json", ReadinessJson(10, 0, 0));

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Empty(cut.FindAll(".hs-coverage-radar-shell"));
            Assert.Empty(cut.FindAll(".hs-readiness-card"));
            Assert.Contains("Aún no hay entrenos registrados", cut.Markup);

            var cta = cut.FindAll("button.mud-button")
                .FirstOrDefault(button => button.TextContent.Contains("Registra tu primer entreno"));
            Assert.NotNull(cta);
        });
    }

    [Fact]
    public void Dashboard_KeepsHealthPanels_WhenTrainingEndpointsFail()
    {
        MockHttp.When("/Dashboard/summary").Respond("application/json", """
            { "latestTest": { "id": 1, "status": "completed", "testDate": "2026-10-01T00:00:00Z" } }
            """);
        MockHttp.When("/BloodTests").Respond("application/json", """
            [ { "id": "00000000-0000-0000-0000-000000000001",
                "testDate": "2026-10-01T00:00:00",
                "labName": "Test Lab",
                "processingStatus": 0,
                "requiresReview": true,
                "markerCount": 4 } ]
            """);
        MockHttp.When("/BloodTests/markers/latest").Respond("application/json", "[]");
        MockHttp.When("/HealthProfile").Respond("application/json", "null");
        MockHttp.When(CurrentCoverageUrl).Respond(req => new HttpResponseMessage(HttpStatusCode.NotFound));
        MockHttp.When(PreviousCoverageUrl).Respond(req => new HttpResponseMessage(HttpStatusCode.NotFound));
        MockHttp.When("/Readiness").Respond(req => new HttpResponseMessage(HttpStatusCode.NotFound));

        var cut = RenderComponent<Dashboard>();

        cut.WaitForAssertion(() =>
        {
            Assert.Contains("Training overview unavailable", cut.Markup);
            Assert.Contains("Needs attention", cut.Markup);
            Assert.DoesNotContain("Health information could not be loaded.", cut.Markup);
        });
    }

    [Fact]
    public void CoverageRadar_ClampsOverTargetValues_ToTheGoalRing()
    {
        var cut = RenderComponent<AdaptationCoverageRadar>(parameters => parameters
            .Add(component => component.Current, CoverageItems(
                ("Skill", 6, 2, 3.0), ("Speed", 0, 1, 0.0), ("Power", 2, 1, 2.0),
                ("Strength", 2, 2, 1.0), ("Hypertrophy", 1, 2, 0.5),
                ("MuscularEndurance", 2, 2, 1.0), ("AnaerobicCapacity", 0, 1, 0.0),
                ("MaximalAerobicPower", 1, 1, 1.0), ("LongDurationEndurance", 0, 2, 0.0))));

        var points = cut.Find(".hs-coverage-radar-current")
            .GetAttribute("points")!
            .Split(' ')
            .Select(pair => pair.Split(','))
            .Select(pair => (
                X: double.Parse(pair[0], CultureInfo.InvariantCulture),
                Y: double.Parse(pair[1], CultureInfo.InvariantCulture)))
            .ToList();

        Assert.Equal(9, points.Count);

        foreach (var point in points)
        {
            var distance = Math.Sqrt(Math.Pow(point.X - 230, 2) + Math.Pow(point.Y - 212, 2));
            Assert.True(distance <= 129,
                $"Vertex ({point.X}, {point.Y}) sits {distance:0.0}px from the centre; the goal ring is 128px.");
        }
    }

    private static List<AdaptationCoverageItemResource> CoverageItems(
        params (string Type, int Sessions, int Target, double Ratio)[] rows)
    {
        var today = DateTime.Today;
        var items = new List<AdaptationCoverageItemResource>(rows.Length);

        for (var index = 0; index < rows.Length; index++)
        {
            var (type, sessions, target, ratio) = rows[index];

            items.Add(new AdaptationCoverageItemResource
            {
                AdaptationId = index + 1,
                Order = index + 1,
                Type = type,
                Name = type,
                Color = "#0F5F67",
                Volume = sessions * 100d,
                Sessions = sessions,
                TargetSessionsPerWeek = target,
                CoverageRatio = ratio,
                LastSessionDate = sessions > 0 ? today.AddDays(-1) : null,
                DaysSinceLastSession = sessions > 0 ? 1 : null,
                Trend = sessions > 0 ? "up" : "none"
            });
        }

        return items;
    }

    // The client builds this query in a fixed key order (from, to), so the
    // URL is deterministic for a given day.
    private static string CurrentCoverageUrl
        => CoverageUrl(-6, 0);

    private static string PreviousCoverageUrl
        => CoverageUrl(-13, -7);

    private static string CoverageUrl(int fromOffset, int toOffset)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        return "/Adaptations/coverage"
               + $"?from={today.AddDays(fromOffset):yyyy-MM-dd}"
               + $"&to={today.AddDays(toOffset):yyyy-MM-dd}";
    }

    private static string CoverageJson(
        IReadOnlyList<AdaptationCoverageItemResource> items,
        int? sessions = null)
    {
        var today = DateTime.Today;

        return System.Text.Json.JsonSerializer.Serialize(new
        {
            from = today.AddDays(-6).ToString("yyyy-MM-dd"),
            to = today.ToString("yyyy-MM-dd"),
            sessions = sessions ?? items.Sum(item => item.Sessions),
            items
        });
    }

    private static string ReadinessJson(
        double score,
        double fatigue,
        int sessions,
        params (string Type, string Reason)[] suggestions)
        => System.Text.Json.JsonSerializer.Serialize(new
        {
            date = DateTime.Today.ToString("yyyy-MM-dd"),
            score,
            fatigueLoad7d = fatigue,
            sessions7d = sessions,
            totalVolume7d = sessions * 100d,
            perAdaptation = Array.Empty<object>(),
            suggestions = suggestions.Select((suggestion, index) => new
            {
                adaptationId = index + 1,
                order = index + 1,
                type = suggestion.Type,
                name = suggestion.Type,
                color = "#0F5F67",
                reason = suggestion.Reason
            })
        });
}
