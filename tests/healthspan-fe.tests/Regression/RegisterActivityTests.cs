using System.Net;
using System.Text;
using System.Text.Json;
using AngleSharp.Dom;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Wellbeing;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Register-activity page (t_78447e2e): inline validation (UX spec §5) and
/// payload mapping to the fixed API contract (api-contract-activities-t_0e23bbe6.md):
/// canonical dimension strings, occurredOnUtc with 'Z', temperatureC→temperatureCelsius,
/// per-type dynamic params inside metadata.
/// </summary>
public class RegisterActivityTests : BlazorTestBase
{
    private static readonly Guid ActivityId = Guid.Parse("3fa85f64-5717-4562-b3fc-2c963f66afa6");

    private static string TodayIso => WellbeingActivityDates.TodayForRegistration().ToString("yyyy-MM-dd");

    private IRenderedComponent<RegisterActivity> _cut = default!;
    private string? _postedBody;
    private string? _putBody;
    private string? _putPath;

    private static string ActivityJson(
        string type = "sauna",
        string dimension = "health",
        string? metadata = "{\"humidity_percent\":15}",
        string? occurredOnUtc = null)
        => $$"""
           {
             "id": "{{ActivityId}}",
             "type": "{{type}}",
             "dimension": "{{dimension}}",
             "occurredOnUtc": "{{occurredOnUtc ?? TodayIso + "T00:00:00Z"}}",
             "durationMinutes": 20,
             "frequencyPerWeek": 3,
             "intensity": 3,
             "temperatureCelsius": 85,
             "cycles": 3,
             "notes": "Sesión post-entreno",
             "metadata": {{metadata ?? "null"}},
             "createdAtUtc": "2026-10-23T10:00:00Z"
           }
           """;

    private void MockActivitiesPost(HttpStatusCode status = HttpStatusCode.Created, string? responseJson = null)
    {
        MockHttp.When(HttpMethod.Post, "/api/activities")
            .Respond(req =>
            {
                _postedBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                var response = new HttpResponseMessage(status);
                var body = responseJson ?? (status == HttpStatusCode.Created ? ActivityJson() : null);
                if (body is not null)
                {
                    response.Content = new StringContent(body, Encoding.UTF8, "application/json");
                }

                return response;
            });
    }

    private void MockActivitiesPut(HttpStatusCode status = HttpStatusCode.NoContent)
    {
        MockHttp.When(HttpMethod.Put, $"/api/activities/{ActivityId}")
            .Respond(req =>
            {
                _putBody = req.Content?.ReadAsStringAsync().GetAwaiter().GetResult();
                _putPath = req.RequestUri?.AbsolutePath;
                return new HttpResponseMessage(status);
            });
    }

    private void Render() => _cut = RenderComponent<RegisterActivity>();

    private void FillSaunaForm(
        string duration = "20",
        string? temperature = "85",
        string? cycles = "3",
        string? intensity = "3",
        string? frequency = "3",
        string? notes = "Sesión post-entreno")
    {
        _cut.Find("#reg-durationMinutes").Change(duration);
        if (temperature is not null)
        {
            _cut.Find("#reg-temperatureC").Change(temperature);
        }
        if (cycles is not null)
        {
            _cut.Find("#reg-cycles").Change(cycles);
        }
        if (intensity is not null)
        {
            FindChip(intensity).Click();
        }
        if (frequency is not null)
        {
            _cut.Find("#reg-frequency").Change(frequency);
        }
        if (notes is not null)
        {
            _cut.Find("#reg-notes").Change(notes);
        }
    }

    private IElement FindChip(string label)
        => _cut.FindAll(".hs-chip-row")
            .SelectMany(row => row.QuerySelectorAll("button"))
            .First(button => button.TextContent.Trim() == label);

    private void PickType(string label) => FindChip(label).Click();

    private void ClickSave() => _cut.Find(".hs-cta-primary").Click();

    private JsonDocument PostedJson() => JsonDocument.Parse(_postedBody!);

    private IElement[] TypeChips()
        => _cut.Find("[aria-label='Tipo de actividad']").QuerySelectorAll("button").ToArray();

    // ===== Default state =====

    [Fact]
    public void Default_State_Presets_Type_Dimension_And_Todays_Date()
    {
        Render();

        var typeChips = TypeChips();
        typeChips[0].GetAttribute("aria-pressed").Should().Be("true", "sauna is the first type");
        typeChips[0].ClassList.Should().Contain("hs-chip-selected");

        var dimChips = _cut.Find("[aria-label='Dimensión']").QuerySelectorAll("button");
        dimChips.Should().HaveCount(3);
        dimChips.First(chip => chip.TextContent.Trim() == "Salud")
            .GetAttribute("aria-pressed").Should().Be("true", "dimension pre-selected by type");

        _cut.Find("#reg-date").GetAttribute("value").Should().Be(TodayIso);
        _cut.Find("#reg-date").GetAttribute("max").Should().Be(
            WellbeingActivityDates.TodayForRegistration().ToString("yyyy-MM-dd"),
            "the picker must not allow a day the API would reject");
        _cut.Find(".hs-cta-primary").TextContent.Should().Contain("Guardar actividad");
    }

    // ===== Inline validation (UX spec §5) =====

    [Fact]
    public void Submit_Marks_Required_Duration_Inline_And_Does_Not_Post()
    {
        Render();

        ClickSave();

        _cut.Find("#err-durationMinutes").ClassList.Should().Contain("hs-field-error-show");
        _cut.Find("#err-durationMinutes").TextContent
            .Should().Contain("Duración (min) es obligatorio.");
        _postedBody.Should().BeNull();
        _cut.Find("#reg-durationMinutes").GetAttribute("aria-invalid").Should().Be("true");
    }

    [Fact]
    public void Out_Of_Range_Duration_Shows_Specific_Range_Message_On_Blur()
    {
        Render();

        _cut.Find("#reg-durationMinutes").Change("2000");

        _cut.Find("#err-durationMinutes").TextContent
            .Should().Contain("Duración (min) debe estar entre 1 y 1440.");
    }

    [Fact]
    public void Out_Of_Range_Temperature_Shows_Range_Message()
    {
        Render();

        _cut.Find("#reg-temperatureC").Change("85");
        _cut.Find("#reg-temperatureC").Change("700");

        _cut.Find("#err-temperatureC").TextContent
            .Should().Contain("Temperatura (°C) debe estar entre -100 y 200.");
    }

    [Fact]
    public void Out_Of_Range_Frequency_Shows_Specific_Message()
    {
        Render();

        _cut.Find("#reg-frequency").Change("9");

        _cut.Find("#err-frequencyPerWeek").TextContent
            .Should().Contain("La frecuencia debe estar entre 1 y 7 sesiones por semana.");
        ClickSave();
        _postedBody.Should().BeNull();
    }

    // ===== Payload mapping to the fixed contract =====

    [Fact]
    public void Sauna_Submit_Sends_Contract_Payload_With_Extra_Fields_In_Metadata()
    {
        MockActivitiesPost();
        Render();

        FillSaunaForm();
        _cut.Find(".hs-chip-more").Click();
        var extraRow = _cut.Find(".hs-extra-row");
        extraRow.QuerySelectorAll("input")[0].Input("humidity_percent");
        extraRow.QuerySelectorAll("input")[1].Input("15");

        ClickSave();

        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        var root = PostedJson().RootElement;
        root.GetProperty("type").GetString().Should().Be("sauna");
        root.GetProperty("dimension").GetString().Should().Be("health", "UI label salud maps to canonical key");
        root.GetProperty("occurredOnUtc").GetString().Should().Be($"{TodayIso}T00:00:00Z");
        root.GetProperty("durationMinutes").GetInt32().Should().Be(20);
        root.GetProperty("temperatureCelsius").GetInt32().Should().Be(85);
        root.GetProperty("cycles").GetInt32().Should().Be(3);
        root.GetProperty("intensity").GetInt32().Should().Be(3);
        root.GetProperty("frequencyPerWeek").GetInt32().Should().Be(3);
        root.GetProperty("notes").GetString().Should().Be("Sesión post-entreno");

        root.TryGetProperty("temperatureC", out _).Should().BeFalse("temperatureC is renamed, never sent raw");
        root.TryGetProperty("date", out _).Should().BeFalse();
        root.TryGetProperty("extra", out _).Should().BeFalse();

        var metadata = root.GetProperty("metadata");
        metadata.GetProperty("humidity_percent").GetInt32().Should().Be(15);

        _cut.Find(".hs-reg-success").TextContent.Should().Contain("Guardado · Sauna");
    }

    // t_0420b752: the saved summary must show the REAL saved day, using the
    // same labels the history uses (Hoy / Ayer / "martes, 20 de octubre"),
    // never an unconditional "hoy".
    [Fact]
    public void SavedSummary_Uses_The_Actual_Saved_Day_Label()
    {
        MockActivitiesPost();
        Render();

        FillSaunaForm();
        var pastDay = DateOnly.FromDateTime(DateTime.Now).AddDays(-5);
        _cut.Find("#reg-date").Change(pastDay.ToString("yyyy-MM-dd"));

        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        var expected = EsFormat.WeekdayDayMonthFull(pastDay);
        _cut.Find(".hs-reg-success-title").TextContent.Trim()
            .Should().Be($"Guardado · Sauna · {expected}");
    }

    [Fact]
    public void SavedSummary_Says_Hoy_For_Today()
    {
        MockActivitiesPost();
        Render();

        FillSaunaForm();
        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        _cut.Find(".hs-reg-success-title").TextContent.Trim()
            .Should().Be("Guardado · Sauna · Hoy");
    }

    [Fact]
    public void Wim_Hof_Breaths_Per_Round_Travels_In_Metadata()
    {
        MockActivitiesPost(responseJson: ActivityJson("wim_hof", metadata: "{\"breathsPerRound\":35}"));
        Render();

        PickType("Respiración Wim Hof");
        _cut.Find("#reg-cycles").Change("4");
        _cut.Find("#reg-durationMinutes").Change("30");
        _cut.Find("#reg-breathsPerRound").Change("35");

        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        var root = PostedJson().RootElement;
        root.GetProperty("type").GetString().Should().Be("wim_hof");
        root.GetProperty("cycles").GetInt32().Should().Be(4);
        root.GetProperty("durationMinutes").GetInt32().Should().Be(30);
        root.TryGetProperty("breathsPerRound", out _).Should().BeFalse("dynamic per-type param is not a root field");
        root.GetProperty("metadata").GetProperty("breathsPerRound").GetInt32().Should().Be(35);
    }

    [Fact]
    public void Meditation_Style_Select_Travels_In_Metadata()
    {
        MockActivitiesPost(responseJson: ActivityJson("meditation", metadata: "{\"style\":\"Guiada\"}"));
        Render();

        PickType("Meditación");
        _cut.Find("#reg-durationMinutes").Change("15");
        _cut.Find("#reg-style").Change("Guiada");

        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        var root = PostedJson().RootElement;
        root.GetProperty("type").GetString().Should().Be("meditation");
        root.TryGetProperty("style", out _).Should().BeFalse();
        root.GetProperty("metadata").GetProperty("style").GetString().Should().Be("Guiada");
    }

    [Fact]
    public void Other_Activity_Name_Travels_In_Metadata_And_Type_Is_other()
    {
        MockActivitiesPost(responseJson: ActivityJson("other", metadata: "{\"name\":\"Yoga\"}"));
        Render();

        PickType("Otra actividad");
        _cut.Find("#reg-name").Change("Yoga");
        _cut.Find("#reg-durationMinutes").Change("45");

        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        var root = PostedJson().RootElement;
        root.GetProperty("type").GetString().Should().Be("other");
        root.GetProperty("metadata").GetProperty("name").GetString().Should().Be("Yoga");
        root.GetProperty("durationMinutes").GetInt32().Should().Be(45);
    }

    [Fact]
    public void Dimension_Can_Be_Overridden_And_Maps_To_Canonical_Key()
    {
        MockActivitiesPost();
        Render();

        FindChip("Movilidad").Click();
        FillSaunaForm(temperature: null, cycles: null, intensity: null, frequency: null, notes: null);

        ClickSave();
        _cut.WaitForState(() => _postedBody is not null, TimeSpan.FromSeconds(5));

        PostedJson().RootElement.GetProperty("dimension").GetString().Should().Be("mobility");
    }

    // ===== States (UX spec §6) =====

    [Fact]
    public void Network_Failure_Shows_Banner_And_Preserves_Input()
    {
        MockHttp.When(HttpMethod.Post, "/api/activities")
            .Respond(_ => throw new HttpRequestException("no connection"));
        Render();

        FillSaunaForm(temperature: null, cycles: null, intensity: null, frequency: null, notes: null);
        ClickSave();

        _cut.WaitForElement(".hs-reg-banner");
        _cut.Find(".hs-reg-banner").TextContent
            .Should().Contain("no hay conexión")
            .And.Contain("intacto");
        _cut.Find("#reg-durationMinutes").GetAttribute("value").Should().Be("20");
        _cut.Find(".hs-cta-primary").TextContent.Should().Contain("Guardar actividad", "CTA re-enabled for retry");
    }

    [Fact]
    public void Server_400_ProblemDetails_Maps_Field_Errors_Inline_With_Banner()
    {
        MockActivitiesPost(
            HttpStatusCode.BadRequest,
            """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Validation error",
              "status": 400,
              "errors": { "durationMinutes": ["Duration must be between 1 and 1440 minutes."] }
            }
            """);
        Render();

        FillSaunaForm(duration: "20");
        ClickSave();

        _cut.WaitForElement(".hs-reg-banner");
        _cut.Find(".hs-reg-banner").TextContent.Should().Contain("El servidor rechazó el registro");
        _cut.Find("#err-durationMinutes").TextContent
            .Should().Contain("Duration must be between 1 and 1440 minutes.");
        _cut.Find("#reg-durationMinutes").GetAttribute("value").Should().Be("20", "input preserved");
    }

    [Fact]
    public void Success_Panel_Offers_Register_Again_Keeping_Type_And_Today()
    {
        MockActivitiesPost();
        Render();

        PickType("Meditación");
        _cut.Find("#reg-durationMinutes").Change("15");
        ClickSave();
        _cut.WaitForElement(".hs-reg-success");

        // Polling click: under full-suite load the render triggered by this click
        // can sit queued behind the success render batch, so a single Find+Click
        // may act on a stale DOM snapshot. Re-click until the success panel is
        // actually gone (RegisterAnother is idempotent).
        _cut.WaitForState(
            () =>
            {
                var again = _cut.FindAll(".hs-reg-success-actions .hs-cta-primary");
                if (again.Count > 0) again[0].Click();
                return _cut.FindAll(".hs-reg-success").Count == 0;
            },
            TimeSpan.FromSeconds(10));

        _cut.Find(".hs-cta-primary").TextContent.Should().Contain("Guardar actividad");
        TypeChips().First(chip => chip.TextContent.Trim() == "Meditación")
            .GetAttribute("aria-pressed").Should().Be("true", "type kept for zero-friction repeat");
        _cut.Find("#reg-date").GetAttribute("value").Should().Be(TodayIso);
        _cut.Find("#reg-durationMinutes").GetAttribute("value").Should().BeNull("values cleared");
    }

    // ===== Edit mode (?edit=id) =====

    [Fact]
    public void Edit_Mode_Loads_Activity_Prefills_And_Updates_With_Put()
    {
        MockHttp.When(HttpMethod.Get, $"/api/activities/{ActivityId}")
            .Respond("application/json", ActivityJson("meditation", metadata: "{\"style\":\"Guiada\"}"));
        MockActivitiesPut();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/wellbeing/register?edit={ActivityId}");
        Render();

        _cut.WaitForElement("#reg-style");
        _cut.Find("#reg-style option[value='Guiada']").HasAttribute("selected")
            .Should().BeTrue("prefill selects the stored style");
        _cut.Find("#reg-durationMinutes").GetAttribute("value").Should().Be("20");
        _cut.Find(".hs-cta-primary").TextContent.Should().Contain("Guardar cambios");

        TypeChips().First(chip => chip.TextContent.Trim() == "Meditación")
            .GetAttribute("aria-pressed").Should().Be("true");

        ClickSave();
        _cut.WaitForState(() => _putBody is not null, TimeSpan.FromSeconds(5));

        _putPath.Should().Be($"/api/activities/{ActivityId}");
        var root = JsonDocument.Parse(_putBody!).RootElement;
        root.GetProperty("type").GetString().Should().Be("meditation");
        root.GetProperty("dimension").GetString().Should().Be("health");
        root.GetProperty("metadata").GetProperty("style").GetString().Should().Be("Guiada");
    }

    // ===== Date contract (t_249c6eec) =====

    [Fact]
    public void Edit_Mode_Prefills_Exactly_The_Stored_Day_No_Timezone_Drift()
    {
        MockHttp.When(HttpMethod.Get, $"/api/activities/{ActivityId}")
            .Respond("application/json", ActivityJson(
                "meditation", metadata: "{\"style\":\"Guiada\"}", occurredOnUtc: "2026-10-24T00:00:00Z"));
        MockActivitiesPut();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/wellbeing/register?edit={ActivityId}");
        Render();

        _cut.WaitForElement("#reg-style");
        _cut.Find("#reg-date").GetAttribute("value").Should().Be(
            "2026-10-24", "a UTC+ device must show the stored day, not the day before");

        // Saving only touches other fields: occurredOnUtc must travel identical.
        ClickSave();
        _cut.WaitForState(() => _putBody is not null, TimeSpan.FromSeconds(5));

        JsonDocument.Parse(_putBody!).RootElement
            .GetProperty("occurredOnUtc").GetString()
            .Should().Be("2026-10-24T00:00:00Z", "edit must not displace the activity day");
    }

    [Fact]
    public void Edit_Mode_Legacy_Response_Without_Z_Parses_As_The_Utc_Day()
    {
        MockHttp.When(HttpMethod.Get, $"/api/activities/{ActivityId}")
            .Respond("application/json", ActivityJson(
                "meditation", metadata: "{\"style\":\"Guiada\"}", occurredOnUtc: "2026-10-24T00:00:00"));
        MockActivitiesPut();

        Services.GetRequiredService<NavigationManager>()
            .NavigateTo($"/wellbeing/register?edit={ActivityId}");
        Render();

        _cut.WaitForElement("#reg-style");
        _cut.Find("#reg-date").GetAttribute("value").Should().Be(
            "2026-10-24", "occurredOnUtc without offset is UTC, never device-local");
    }

    [Fact]
    public void Server_Date_Rejection_Shows_Field_Error_In_Spanish()
    {
        MockActivitiesPost(
            HttpStatusCode.BadRequest,
            """
            {
              "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
              "title": "Validation error",
              "status": 400,
              "code": "activity_date_invalid",
              "errors": { "occurredOnUtc": ["Activity date is required and cannot be in the future."] }
            }
            """);
        Render();

        FillSaunaForm(duration: "20");
        ClickSave();

        _cut.WaitForElement(".hs-reg-banner");
        _cut.Find(".hs-reg-banner").TextContent.Should().Contain("El servidor rechazó el registro");
        _cut.Find("#err-date").TextContent
            .Should().Contain("no puede ser futura")
            .And.NotContain("cannot be in the future", "field copy must match the page language");
    }
}
