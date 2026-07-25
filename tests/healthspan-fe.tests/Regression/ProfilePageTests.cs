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
        MockHttp.When("/Exercises?sortBy=date&descending=true")
            .Respond("application/json", """
                [
                  {
                    "id": "a8a0d49f-53c0-4d36-8d28-4d2983d28f11",
                    "series": 4,
                    "repetitions": 8,
                    "weight": 80,
                    "maxRepetition": 96.5,
                    "volume": 2560,
                    "type": {
                      "id": 101,
                      "name": "Bench Press",
                      "muscleAssignments": [
                        { "muscleId": 1, "role": "Primary", "sortOrder": 1 },
                        { "muscleId": 2, "role": "Secondary", "sortOrder": 1 },
                        { "muscleId": 9, "role": "Secondary", "sortOrder": 2 }
                      ],
                      "tags": [
                        { "name": "Barbell", "category": "equipment" },
                        { "name": "Compound", "category": "type" }
                      ]
                    },
                    "workout": {
                      "id": "2e7c1835-2ac3-4b0d-9875-2ab39ce3c001",
                      "type": "Upper strength",
                      "date": "2026-04-07T00:00:00"
                    }
                  },
                  {
                    "id": "04f1ef38-66dd-4585-a4aa-8af6b51cbb47",
                    "series": 4,
                    "repetitions": 6,
                    "weight": 120,
                    "volume": 2880,
                    "type": {
                      "id": 102,
                      "name": "Deadlift",
                      "muscleAssignments": [
                        { "muscleId": 6, "role": "Primary", "sortOrder": 1 },
                        { "muscleId": 17, "role": "Secondary", "sortOrder": 1 },
                        { "muscleId": 15, "role": "Secondary", "sortOrder": 2 }
                      ]
                    },
                    "workout": {
                      "id": "2e7c1835-2ac3-4b0d-9875-2ab39ce3c001",
                      "type": "Upper strength",
                      "date": "2026-04-07T00:00:00"
                    }
                  },
                  {
                    "id": "8f5671d2-c5d4-4029-a46f-53a6048f3981",
                    "series": 3,
                    "repetitions": 12,
                    "weight": 50,
                    "volume": 1800,
                    "type": {
                      "id": 103,
                      "name": "Pull Up",
                      "muscleAssignments": [
                        { "muscleId": 5, "role": "Primary", "sortOrder": 1 },
                        { "muscleId": 8, "role": "Secondary", "sortOrder": 1 }
                      ]
                    },
                    "workout": {
                      "id": "cc084af1-8d67-49db-a48d-1fb0ee40da81",
                      "date": "2026-03-30T00:00:00"
                    }
                  }
                ]
            """);

        MockHttp.When("/api/muscles")
            .Respond("application/json", """
                [
                  { "id": 1, "slug": "chest", "name": "Chest", "side": "front", "muscleGroup": "Chest", "sortOrder": 10 },
                  { "id": 2, "slug": "front-delts", "name": "Front delts", "side": "front", "muscleGroup": "Shoulders", "sortOrder": 20 },
                  { "id": 5, "slug": "lats", "name": "Lats", "side": "back", "muscleGroup": "Back", "sortOrder": 50 },
                  { "id": 6, "slug": "upper-back", "name": "Upper back", "side": "back", "muscleGroup": "Back", "sortOrder": 60 },
                  { "id": 8, "slug": "biceps", "name": "Biceps", "side": "front", "muscleGroup": "Arms", "sortOrder": 80 },
                  { "id": 9, "slug": "triceps", "name": "Triceps", "side": "back", "muscleGroup": "Arms", "sortOrder": 90 },
                  { "id": 15, "slug": "glutes", "name": "Glutes", "side": "back", "muscleGroup": "Glutes", "sortOrder": 150 },
                  { "id": 17, "slug": "hamstrings", "name": "Hamstrings", "side": "back", "muscleGroup": "Legs", "sortOrder": 170 }
                ]
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
        MockHttp.When("/Exercises?sortBy=date&descending=true")
            .Respond("application/json", "[]");
        MockHttp.When("/api/muscles")
            .Respond("application/json", "[]");
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
