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
    public void Renders_Insights_With_Profile_Aware_Muscle_Load()
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

        MockHttp.When("/HealthProfile")
            .Respond("application/json", """
                {
                  "biologicalSex": 1,
                  "heightCm": 168,
                  "weightKg": 64
                }
                """);

        var cut = RenderComponent<Profile>();

        // Polling wait instead of render-event WaitForAssertion (flaky under
        // full-suite load; see BlazorTestBase wait-ceiling note).
        cut.WaitForState(() =>
            cut.Markup.Contains("Insights") && cut.Markup.Contains("Bench Press"));

        cut.Markup.Should().Contain("Insights");
        cut.Markup.Should().Contain("Carga muscular");
        cut.Markup.Should().Contain("Sesiones");
        cut.Markup.Should().Contain("Ejercicios");
        cut.Markup.Should().Contain("Bench Press");
        cut.Markup.Should().Contain("Deadlift");
        cut.Markup.Should().Contain("4 x 8 @ 80 kg");
        cut.Markup.Should().Contain("Chest");
        cut.Markup.Should().Contain("data-body-profile=\"female\"");
    }

    [Fact]
    public void Renders_Empty_Profile_States_When_No_Data_Is_Returned()
    {
        MockHttp.When("/Workouts/weekly-summary?weeks=12")
            .Respond("application/json", """{ "weeks": [] }""");
        MockHttp.When("/HealthProfile")
            .Respond("application/json", """{ "biologicalSex": 0 }""");
        var cut = RenderComponent<Profile>();

        cut.WaitForState(() => cut.Markup.Contains("Aún no hay datos de entrenamiento"));

        cut.Markup.Should().Contain("Aún no hay datos de entrenamiento");
        cut.Markup.Should().Contain(
            "Registra un entreno para empezar a ver la carga semanal");
    }
}
