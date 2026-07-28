using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages.Health;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

public class ProfilePageTests : BlazorTestBase
{
    [Fact]
    public void Renders_Profile_With_Muscle_Load_And_Radar_Snapshots()
    {
        MockHttp.When("/Workouts/weekly-summary?weeks=12")
            .Respond("application/json", """
                {
                  "weeks": [
                    {
                      "weekStart": "2026-04-06T00:00:00",
                      "totalLoad": 5440,
                      "workoutCount": 1,
                      "exerciseCount": 2,
                      "regions": [
                        {
                          "key": "chest",
                          "label": "Chest",
                          "side": "front",
                          "group": "Chest",
                          "load": 1792,
                          "relativeLoad": 1
                        },
                        {
                          "key": "upper-back",
                          "label": "Upper back",
                          "side": "back",
                          "group": "Back",
                          "load": 2016,
                          "relativeLoad": 0.9
                        }
                      ],
                      "sessions": [
                        {
                          "key": "2e7c1835-2ac3-4b0d-9875-2ab39ce3c001",
                          "date": "2026-04-07T00:00:00",
                          "label": "Upper strength",
                          "totalLoad": 5440,
                          "exerciseCount": 2,
                          "focusGroups": [ "Chest", "Back" ],
                          "exercises": [
                            {
                              "name": "Bench Press",
                              "prescription": "4 x 8 @ 80 kg",
                              "sessionLabel": "Upper strength",
                              "sessionDate": "2026-04-07T00:00:00",
                              "primaryMuscles": [ "Chest" ],
                              "secondaryMuscles": [ "Front delts", "Triceps" ],
                              "load": 2560,
                              "maxRepetition": 96.5,
                              "tags": [ "Barbell", "Compound" ]
                            },
                            {
                              "name": "Deadlift",
                              "prescription": "4 x 6 @ 120 kg",
                              "sessionLabel": "Upper strength",
                              "sessionDate": "2026-04-07T00:00:00",
                              "primaryMuscles": [ "Upper back" ],
                              "secondaryMuscles": [ "Hamstrings", "Glutes" ],
                              "load": 2880,
                              "tags": []
                            }
                          ]
                        }
                      ]
                    }
                  ]
                }
            """);

        MockHttp.When("/GalpinAssessments/catalog")
            .Respond("application/json", """
                [
                  { "adaptation": 0, "displayName": "Strength" },
                  { "adaptation": 1, "displayName": "Power" },
                  { "adaptation": 2, "displayName": "Speed" },
                  { "adaptation": 3, "displayName": "Hypertrophy" },
                  { "adaptation": 4, "displayName": "Muscular Endurance" },
                  { "adaptation": 5, "displayName": "Anaerobic Capacity" },
                  { "adaptation": 6, "displayName": "Max Aerobic Output" },
                  { "adaptation": 7, "displayName": "Long Duration Endurance" },
                  { "adaptation": 8, "displayName": "Mobility & Stability" }
                ]
            """);

        MockHttp.When("/GalpinAssessments")
            .Respond("application/json", """
                [
                  {
                    "id": "8141cad2-9f2f-4c71-b498-4bf4a0a2f120",
                    "assessedOnUtc": "2025-01-15T00:00:00",
                    "scores": [
                      { "adaptation": 0, "score": 5.2 },
                      { "adaptation": 1, "score": 5.8 },
                      { "adaptation": 2, "score": 5.4 },
                      { "adaptation": 3, "score": 6.1 },
                      { "adaptation": 4, "score": 6.0 },
                      { "adaptation": 5, "score": 5.9 },
                      { "adaptation": 6, "score": 5.5 },
                      { "adaptation": 7, "score": 5.7 },
                      { "adaptation": 8, "score": 6.2 }
                    ]
                  },
                  {
                    "id": "aa3de6ff-0b16-4b61-a1d6-d934c026d17f",
                    "assessedOnUtc": "2026-01-20T00:00:00",
                    "scores": [
                      { "adaptation": 0, "score": 6.3 },
                      { "adaptation": 1, "score": 6.5 },
                      { "adaptation": 2, "score": 6.1 },
                      { "adaptation": 3, "score": 6.8 },
                      { "adaptation": 4, "score": 6.4 },
                      { "adaptation": 5, "score": 6.2 },
                      { "adaptation": 6, "score": 6.0 },
                      { "adaptation": 7, "score": 5.9 },
                      { "adaptation": 8, "score": 6.6 }
                    ]
                  },
                  {
                    "id": "b0c4f6e9-905f-4719-b857-b9b7719f7130",
                    "assessedOnUtc": "2026-04-08T00:00:00",
                    "scores": [
                      { "adaptation": 0, "score": 7.1 },
                      { "adaptation": 1, "score": 7.0 },
                      { "adaptation": 2, "score": 6.7 },
                      { "adaptation": 3, "score": 7.4 },
                      { "adaptation": 4, "score": 6.9 },
                      { "adaptation": 5, "score": 6.8 },
                      { "adaptation": 6, "score": 6.5 },
                      { "adaptation": 7, "score": 6.4 },
                      { "adaptation": 8, "score": 7.2 }
                    ]
                  }
                ]
            """);

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("Weekly muscle load");
            cut.Markup.Should().Contain("Workouts this week");
            cut.Markup.Should().Contain("Carry this into next week");
            cut.Markup.Should().Contain("Upper strength");
            cut.Markup.Should().Contain("Bench Press");
            cut.Markup.Should().Contain("Deadlift");
            cut.Markup.Should().Contain("4 x 8 @ 80 kg");
            cut.Markup.Should().Contain("Barbell");
            cut.Markup.Should().Contain("Chest");
            cut.Markup.Should().Contain("Physical adaptations");
            cut.Markup.Should().Contain("Last year");
            cut.Markup.Should().Contain("2 months ago");
            cut.Markup.Should().Contain("Now");
        });
    }

    [Fact]
    public void Renders_Empty_Profile_States_When_No_Data_Is_Returned()
    {
        MockHttp.When("/Workouts/weekly-summary?weeks=12")
            .Respond("application/json", """{ "weeks": [] }""");
        MockHttp.When("/GalpinAssessments/catalog")
            .Respond("application/json", "[]");
        MockHttp.When("/GalpinAssessments")
            .Respond("application/json", "[]");

        var cut = RenderComponent<Profile>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.Should().Contain("No workout history yet");
            cut.Markup.Should().Contain("No workouts logged in this week");
            cut.Markup.Should().Contain("No adaptation assessments yet");
        });
    }
}
