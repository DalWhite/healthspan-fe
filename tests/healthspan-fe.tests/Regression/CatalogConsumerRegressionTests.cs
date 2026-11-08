using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using Bunit;
using FluentAssertions;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Components;
using healthspan_fe_wasm_pwa.Pages.Exercises;
using Microsoft.Extensions.DependencyInjection;
using MudBlazor;
using RichardSzalay.MockHttp;
using Xunit;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Review t_81a400b3 round 2, point 2: the catalog contract changed from
/// "complete list" to "first page + background hydration", and the two
/// consumers that assume the full list each got a fix. These tests pin the
/// fixed behavior so the contract change can never silently break them again.
/// </summary>
public class CatalogConsumerRegressionTests : BlazorTestBase
{
    private const int TotalTypes = 90;
    private const int FirstPageSize = 40;

    private static string PageJson(int skip, int take, bool slim)
    {
        var end = Math.Min(skip + take, TotalTypes);
        var sb = new StringBuilder("[");
        for (var id = skip + 1; id <= end; id++)
        {
            if (id > skip + 1) sb.Append(',');
            sb.Append("{\"id\":").Append(id)
              .Append(",\"name\":\"Exercise ").Append(id.ToString("D3"))
              .Append("\",\"weightType\":0");
            if (!slim)
            {
                sb.Append(",\"description\":\"desc ").Append(id)
                  .Append("\"")
                  .Append(",\"parentExerciseTypeId\":null,\"isFavourite\":false,\"imageRoute\":null");
            }
            sb.Append('}');
        }
        return sb.Append(']').ToString();
    }

    /// <summary>
    /// Slim first page answers instantly; full pages answer after a delay, so
    /// the test can act while hydration is still in flight — the window where
    /// both bugs from review round 1 lived.
    /// </summary>
    private void MockPagedCatalog(MockHttpMessageHandler mock, TimeSpan fullPageDelay)
    {
        mock.When(HttpMethod.Get, "/ExerciseTypes").Respond(async req =>
        {
            var url = req.RequestUri!.Query;
            var slim = url.Contains("minimal");
            var skip = url.Contains("skip=") ? int.Parse(url.Split("skip=")[1].Split('&')[0]) : 0;
            var take = url.Contains("take=") ? int.Parse(url.Split("take=")[1].Split('&')[0]) : 50;
            if (!slim && fullPageDelay > TimeSpan.Zero)
                await Task.Delay(fullPageDelay);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(PageJson(skip, take, slim), Encoding.UTF8, "application/json"),
                Headers = { { "X-Total-Count", TotalTypes.ToString() } }
            };
        });
        mock.When(HttpMethod.Get, "/api/muscles").Respond("application/json", "[]");
    }

    [Fact]
    public async Task Parent_Blur_MidHydration_Does_Not_Falsely_Reject_Valid_Parent()
    {
        // Review finding 3: an edit form whose parent type sits beyond page 1
        // used to validate the blur against the first page only and flag
        // "The selected parent exercise type does not exist." over valid data.
        MockPagedCatalog(MockHttp, TimeSpan.FromMilliseconds(400));

        var cut = RenderComponent<ExerciseTypeForm>(parameters => parameters
            .Add(component => component.Model, new UpdateExerciseTypeResource
            {
                Id = 5,
                Name = "Romanian Sit-up",
                ParentExerciseTypeId = 77, // beyond the 40-row first page
            }));

        var appState = Services.GetRequiredService<AppStateService>();
        (await WaitUntil(() => appState.GetExerciseTypesSnapshot().Length == FirstPageSize))
            .Should().BeTrue("the slim first page must paint before hydration completes");

        var parentInput = cut.FindAll(".mud-select")
            .First(select => select.TextContent.Contains("Variation Of"))
            .QuerySelector("input")!;
        parentInput.Blur();

        // The discriminator: the old handler rejected the parent synchronously
        // against the first-page snapshot. The fixed handler awaits the full
        // catalog before validating, so right after blur nothing is flagged.
        var parentError = typeof(ExerciseTypeForm)
            .GetField("_parentError", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(cut.Instance);
        parentError.Should().BeNull("blur must not validate against a partial catalog");

        (await WaitUntil(() => appState.ExerciseTypesHydrated))
            .Should().BeTrue("background hydration must finish");
        cut.Markup.Should().NotContain("does not exist",
            "a parent resolved from the full catalog must never show a parent error");
    }

    [Fact]
    public async Task Filter_Dialog_After_Partial_Hydration_Offer_Options_Beyond_First_Page()
    {
        // Review finding 1: the Training History filter dialog used to freeze
        // its options at the first page; hydration chunks never reached it.
        MockPagedCatalog(MockHttp, TimeSpan.FromMilliseconds(300));
        MockHttp.When("/Exercises*").Respond("application/json", """
            { "items": [], "totalCount": 0, "pageSize": 20, "currentPage": 1, "totalPages": 0 }
        """);

        var dialogs = RenderComponent<MudDialogProvider>();
        var cut = RenderComponent<Exercises>();
        var appState = Services.GetRequiredService<AppStateService>();

        // Open the dialog once hydration has produced at least one chunk
        // beyond page 1 but while the catalog may still be filling in.
        // Polled, not WaitForState: the page rebuilds its option list without
        // re-rendering, so bUnit's render-driven wait would never re-check.
        (await WaitUntil(() => appState.ExerciseTypesHydrated))
            .Should().BeTrue("hydration must complete in background");

        cut.FindAll("button")
            .First(button => button.TextContent.Contains("Filter"))
            .Click();

        ExerciseFilterDialog dialog = null!;
        dialogs.WaitForState(() =>
        {
            try
            {
                dialog = dialogs.FindComponent<ExerciseFilterDialog>().Instance;
                return dialog is not null;
            }
            catch (Exception)
            {
                return false;
            }
        }, TimeSpan.FromSeconds(5));

        dialog.ExerciseTypes.Should().Contain(option => option.Id == 77,
            "the filter dialog must receive the hydrated catalog, not just page 1");
    }

    private static async Task<bool> WaitUntil(Func<bool> condition, int timeoutMs = 10_000)
    {
        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (condition()) return true;
            await Task.Delay(50);
        }
        return condition();
    }
}
