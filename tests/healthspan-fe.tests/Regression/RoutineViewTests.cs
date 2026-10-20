using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Bunit;
using Bunit.TestDoubles;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using healthspan_fe_wasm_pwa.Shared;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// The Next workout view: what to train next from the user's routines, with
/// the exercise explanations, and the pointer that surfaces it on Today
/// (t_bbd0674b).
/// </summary>
public class RoutineViewTests : BlazorTestBase
{
    private static readonly Guid RoutineId =
        Guid.Parse("00000000-0000-0000-0000-0000000000a1");

    private static string NextWorkoutJson(bool pendingNow, string date)
        => $$"""
            {
              "date": "{{date}}",
              "pendingNow": {{pendingNow.ToString().ToLowerInvariant()}},
              "routineId": "{{RoutineId}}",
              "routineName": "Push / Pull",
              "templateId": 7,
              "type": "Push",
              "frequencyPerWeek": 2,
              "exercises": [
                {
                  "exerciseTypeId": 21,
                  "exerciseTypeName": "Bench Press",
                  "description": "Barbell bench press",
                  "order": 1,
                  "sets": 4,
                  "reps": 8,
                  "loadKg": 80
                },
                {
                  "exerciseTypeId": 34,
                  "exerciseTypeName": "Row",
                  "description": "Chest-supported row",
                  "notes": "Squeeze at the top",
                  "order": 2,
                  "sets": 3,
                  "reps": 10,
                  "loadKg": null
                }
              ]
            }
            """;

    // The local day, the same convention TodayWorkout uses for its own
    // session. Asserting against the UTC date here would hide the bug the
    // review caught: the two pages must agree on which day is "today".
    private static string Today => DateOnly.FromDateTime(DateTime.Today).ToString("yyyy-MM-dd");

    [Fact]
    public void Routine_View_Shows_The_Next_Workout_With_Its_Explanations()
    {
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", NextWorkoutJson(pendingNow: true, Today));

        var cut = RenderComponent<RoutineView>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Push");
            cut.Markup.Should().Contain("2x per week");
            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().Contain("4 x 8");
            cut.Markup.Should().Contain("80 kg");
            // Library description is the explanation the trainee needs.
            cut.Markup.Should().Contain("Barbell bench press");
            // The coach's own wording wins over the library description.
            cut.Markup.Should().Contain("Squeeze at the top");
            cut.Markup.Should().NotContain("Chest-supported row");
            cut.Markup.Should().Contain("Bodyweight");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Routine_View_Starts_The_Workout_In_One_Tap()
    {
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", NextWorkoutJson(pendingNow: true, Today));

        string? postedBody = null;
        MockHttp.When(HttpMethod.Post, "/workouts/apply-template")
            .Respond(async request =>
            {
                postedBody = await request.Content.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.Created)
                {
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                };
            });

        var navigationManager = Services.GetRequiredService<FakeNavigationManager>();
        var cut = RenderComponent<RoutineView>();

        // The activation action is on the page before any decision is asked.
        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Start today's workout")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start today's workout"))
            .Click();

        cut.WaitForAssertion(() =>
        {
            postedBody.Should().NotBeNull();
            postedBody.Should().Contain(RoutineId.ToString());
            postedBody.Should().MatchRegex(@"(?i)templateid\W{0,4}7");
            postedBody.Should().Contain(Today);
            navigationManager.ToBaseRelativePath(navigationManager.Uri)
                .Should().StartWith("training/today");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Routine_View_Hides_The_Start_Action_When_The_Weekly_Quota_Is_Met()
    {
        var nextMonday = "2099-01-05";

        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", NextWorkoutJson(pendingNow: false, nextMonday));

        var cut = RenderComponent<RoutineView>();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("button")
                .Should().NotContain(button => button.TextContent.Contains("Start today's workout"));
            cut.Markup.Should().Contain("Go to today's training");
            cut.Markup.Should().Contain("weekly quota");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Routine_View_Shows_An_Empty_State_When_The_User_Has_No_Routines()
    {
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond(HttpStatusCode.NoContent);

        var cut = RenderComponent<RoutineView>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='status']").TextContent.Should().Contain("No workout scheduled");
            cut.FindAll("button")
                .Should().NotContain(button => button.TextContent.Contains("Start today's workout"));
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Routine_View_Offers_A_Retry_When_The_Load_Fails()
    {
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<RoutineView>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("could not be loaded");
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Try again"));
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Today_Surfaces_The_Next_Workout_From_The_Routine()
    {
        MockTodayFixtures();
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", NextWorkoutJson(pendingNow: true, Today));

        var cut = RenderComponent<TodayWorkout>();

        cut.WaitForAssertion(() =>
        {
            var link = cut.Find(".hs-today-routine-link");
            link.GetAttribute("href").Should().Contain("training/routine");
            link.TextContent.Should().Contain("Next workout · Push");
            link.TextContent.Should().Contain("due today");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Today_Stays_Usable_When_The_Routine_Load_Fails()
    {
        MockTodayFixtures();
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<TodayWorkout>();

        // The routine pointer is optional context: Today keeps its own action.
        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".hs-today-routine-link").Should().BeEmpty();
            cut.Markup.Should().Contain("Add exercise");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Navigation_Exposes_The_Routine_View_Under_Training()
    {
        var cut = RenderComponent<NavMenu>();

        cut.WaitForAssertion(() =>
        {
            var link = cut.FindAll("a")
                .Single(anchor => anchor.GetAttribute("href")?.Contains("training/routine") == true);
            link.TextContent.Should().Contain("Next workout");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    /// <summary>
    /// The date RoutineView writes to /workouts/apply-template must be the date
    /// TodayWorkout reads its session from. RoutineView used DateTime.UtcNow
    /// while Today uses DateTime.Today: when the two differ, the started workout
    /// lands on the previous day's session and Today shows an empty day.
    ///
    /// The two zones are complementary: Pacific/Kiritimati (UTC+14) diverges
    /// from the UTC date from 10:00 UTC onwards, Pacific/Niue (UTC-11) diverges
    /// before 13:00 UTC, so at least one of the two cases is divergent at any
    /// wall-clock time and a UTC-date regression cannot pass the suite.
    /// </summary>
    [Theory]
    [InlineData("Pacific/Kiritimati")]
    [InlineData("Pacific/Niue")]
    public void Routine_View_And_Today_Agree_On_The_Date_In_Any_Timezone(string timeZone)
    {
        var originalTimeZone = Environment.GetEnvironmentVariable("TZ");
        Environment.SetEnvironmentVariable("TZ", timeZone);
        TimeZoneInfo.ClearCachedData();

        try
        {
            var localToday = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local));
            var utcToday = DateOnly.FromDateTime(DateTime.UtcNow);

            MockHttp.When(HttpMethod.Get, "/routines/next")
                .Respond("application/json", NextWorkoutJson(pendingNow: true, localToday.ToString("yyyy-MM-dd")));

            string? todaySessionDate = null;
            MockHttp.When(HttpMethod.Get, "/Workouts/today/sessions*")
                .Respond(request =>
                {
                    todaySessionDate = QueryValue(request.RequestUri!, "date");
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("[]", Encoding.UTF8, "application/json")
                    };
                });
            MockTodayFixtures();

            var today = RenderComponent<TodayWorkout>();
            today.WaitForAssertion(() =>
                todaySessionDate.Should().Be(localToday.ToString("yyyy-MM-dd")),
                timeout: TimeSpan.FromSeconds(3));

            string? postedBody = null;
            MockHttp.When(HttpMethod.Post, "/workouts/apply-template")
                .Respond(async request =>
                {
                    postedBody = await request.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(HttpStatusCode.Created)
                    {
                        Content = new StringContent("{}", Encoding.UTF8, "application/json")
                    };
                });

            var cut = RenderComponent<RoutineView>();
            cut.WaitForAssertion(() =>
                cut.FindAll("button")
                    .Should().Contain(button => button.TextContent.Contains("Start today's workout")),
                timeout: TimeSpan.FromSeconds(3));

            cut.FindAll("button")
                .Single(button => button.TextContent.Contains("Start today's workout"))
                .Click();

            cut.WaitForAssertion(() =>
            {
                postedBody.Should().NotBeNull();
                var postedDate = DateOnly.Parse(
                    Regex.Match(postedBody!, "\"date\"\\s*:\\s*\"(\\d{4}-\\d{2}-\\d{2})\"").Groups[1].Value);

                postedDate.Should().Be(localToday);
                postedDate.ToString("yyyy-MM-dd").Should().Be(todaySessionDate);

                // The guard bites only when the two conventions differ; the two
                // zones above guarantee at least one case is divergent.
                if (localToday != utcToday)
                {
                    postedDate.Should().NotBe(utcToday);
                }
            }, timeout: TimeSpan.FromSeconds(3));
        }
        finally
        {
            Environment.SetEnvironmentVariable("TZ", originalTimeZone);
            TimeZoneInfo.ClearCachedData();
        }
    }

    [Fact]
    public void Routine_View_Does_Not_Blame_The_Network_When_Starting_Fails()
    {
        MockHttp.When(HttpMethod.Get, "/routines/next")
            .Respond("application/json", NextWorkoutJson(pendingNow: true, Today));
        MockHttp.When(HttpMethod.Post, "/workouts/apply-template")
            .Respond(HttpStatusCode.InternalServerError);

        var cut = RenderComponent<RoutineView>();

        cut.WaitForAssertion(() =>
            cut.FindAll("button")
                .Should().Contain(button => button.TextContent.Contains("Start today's workout")),
            timeout: TimeSpan.FromSeconds(3));

        cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Start today's workout"))
            .Click();

        // A server failure is not a connectivity failure: telling the trainee
        // they are offline sends them looking for a problem they do not have.
        cut.WaitForAssertion(() =>
        {
            cut.Find("[role='alert']").TextContent.Should().Contain("could not be started");
            cut.Markup.Should().NotContain("offline");
        }, timeout: TimeSpan.FromSeconds(3));
    }

    private static string? QueryValue(Uri uri, string key)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0] == key)
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return null;
    }

    private void MockTodayFixtures()
    {
        MockHttp.When(HttpMethod.Get, "/ExerciseTypes")
            .Respond("application/json", """
                [{ "id": 21, "name": "Bench Press", "isFavourite": true, "weightType": 0 }]
                """);
        MockHttp.When(
                HttpMethod.Get,
                $"/Workouts/today/sessions?date={DateTime.Today:yyyy-MM-dd}")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/workouts?page=1&pageSize=10")
            .Respond("application/json", "[]");
        MockHttp.When(HttpMethod.Get, "/HealthProfile")
            .Respond("application/json", "{}");
    }
}
