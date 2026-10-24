using System.Net;
using System.Text;
using Blazored.LocalStorage;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using RichardSzalay.MockHttp;
using Xunit;
using healthspan_fe.tests.Infrastructure;
using healthspan_fe_wasm_pwa.Pages;
using healthspan_fe_wasm_pwa.Shared;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// CSV import progress UI (t_7bb5d76a) against the async 202+polling contract
/// (t_c1f8e6ac) and the UX spec (t_d1db9866). Guards the spec's hard rules:
/// preflight limits (5 MB / 50k rows), full-file upload (no legacy 512 KB
/// OpenReadStream cap), real progress from the poll, network failure ≠ job
/// failure, re-attach from the localStorage snapshot, and Hide ≠ cancel.
/// </summary>
public sealed class CsvImportProgressTests : BlazorTestBase
{
    private const string JobId = "3f2a9c1e-1111-2222-3333-444455556666";

    private static string AcceptedJson => $"{{\"jobId\":\"{JobId}\",\"total\":10000}}";

    private static string StatusJson(
        string status, int processed, int errorCount = 0, string errors = "[]", string failureReason = "null")
        => "{\"status\":\"" + status + "\",\"total\":10000,\"processed\":" + processed +
           ",\"skipped\":0,\"errorCount\":" + errorCount + ",\"errors\":" + errors +
           ",\"startedAt\":\"2026-10-24T05:00:00Z\",\"finishedAt\":null,\"failureReason\":" + failureReason + "}";

    private void MockAcceptedImport()
        => MockHttp.When(HttpMethod.Post, "/data/import")
            .Respond("application/json", AcceptedJson);

    private static Bunit.InputFileContent CsvContent(string name, int sizeBytes)
        => Bunit.InputFileContent.CreateFromBinary(new byte[sizeBytes], name, null, "text/csv");

    private static void SelectCsvFile(IRenderedComponent<Settings> cut, string name, int sizeBytes)
        => cut.FindComponent<InputFile>().UploadFiles(CsvContent(name, sizeBytes));

    private static void ClickButtonWithText(IRenderedComponent<Settings> cut, string text)
    {
        var button = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Trim().Contains(text, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"No button containing '{text}'.");
        button.Click();
    }

    [Fact]
    public void Preflight_RejectsFileOver5MB_WithoutCallingApi()
    {
        // Regression: the 5 MB limit must be enforced BEFORE the upload, with
        // an inline message — not a server round-trip and not a silent failure.
        var cut = RenderComponent<Settings>();

        SelectCsvFile(cut, "big.csv", 6 * 1024 * 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-inline-error");
        Assert.Contains("Imports support CSV files up to 5 MB", cut.Find(".hs-import-inline-error").TextContent);
        Assert.Empty(cut.FindAll(".hs-import-panel"));
        // No POST was attempted (MockHttp throws on an unmatched request).
    }

    [Fact]
    public void Preflight_RejectsMoreThan50000Rows_WithoutCallingApi()
    {
        var cut = RenderComponent<Settings>();

        // 50,001 data rows + header, well under 5 MB.
        var sb = new StringBuilder("date,activity,intensity_min\n");
        for (var i = 0; i < 50_001; i++)
        {
            sb.Append("2026-01-01,walk,30\n");
        }
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        cut.FindComponent<InputFile>().UploadFiles(
            Bunit.InputFileContent.CreateFromBinary(bytes, "many.csv", null, "text/csv"));
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-inline-error");
        Assert.Contains("50,000 rows", cut.Find(".hs-import-inline-error").TextContent);
        Assert.Empty(cut.FindAll(".hs-import-panel"));
    }

    [Fact]
    public void Upload_StreamsFilesLargerThanTheLegacy512KBLimit()
    {
        // Regression: the old client used OpenReadStream()'s 512 KB default and
        // failed with IOException on ~600 KB files. A 600 KB CSV must now reach
        // the API (OpenReadStream(5 MB) is what makes the POST happen).
        var posted = false;
        MockHttp.When(HttpMethod.Post, "/data/import")
            .Respond(_ =>
            {
                posted = true;
                return new HttpResponseMessage(HttpStatusCode.Accepted)
                {
                    Content = new StringContent(AcceptedJson, Encoding.UTF8, "application/json"),
                };
            });
        MockHttp.When($"/data/import/{JobId}").Respond("application/json", StatusJson("running", 100));

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "medium.csv", 600 * 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-panel");
        Assert.True(posted, "the 600 KB file must be POSTed to /data/import");
        Assert.Empty(cut.FindAll(".hs-import-inline-error"));
    }

    [Fact]
    public void Import_ShowsRealProgressAndCompletedState_DoneClearsPanel()
    {
        MockAcceptedImport();
        var poll = 0;
        MockHttp.When($"/data/import/{JobId}").Respond(_ =>
        {
            poll++;
            var body = poll == 1
                ? StatusJson("running", 4200)
                : StatusJson("completed", 10000);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json"),
            };
        });

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "exercises.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-panel");

        // Real progress from the poll — never a fake local percentage.
        cut.WaitForState(() => cut.FindAll(".hs-import-count")
            .Any(e => e.TextContent.Contains($"{4200:N0} of {10000:N0}")));

        // Completed state with the exact spec copy.
        cut.WaitForElement(".hs-import-finished");
        Assert.Contains($"{10000:N0} rows added to your history", cut.Find(".hs-import-finished").TextContent);

        // Done dismisses the panel.
        ClickButtonWithText(cut, "Done");
        cut.WaitForState(() => !cut.FindAll(".hs-import-panel").Any());
    }

    [Fact]
    public void CompletedWithErrors_ListsRowErrorsAndCapMessage()
    {
        MockAcceptedImport();
        var errors = "[{\"row\":12,\"message\":\"Invalid date \\\"15/4/18\\\" — use dd/MM/yy (e.g. 15/04/18)\"}," +
                     "{\"row\":40,\"message\":\"Intensity must be between 0 and 24 hours\"}]";
        MockHttp.When($"/data/import/{JobId}")
            .Respond("application/json", StatusJson("completed_with_errors", 9863, 137, errors));

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "exercises.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-finished");
        Assert.Contains($"{9863:N0} rows added, {137:N0} rows skipped",
            cut.Find(".hs-import-finished").TextContent);

        var rows = cut.FindAll(".hs-import-error-row");
        Assert.Equal(2, rows.Count);
        Assert.Contains("Row 12", rows[0].TextContent);
        Assert.Contains("Invalid date", rows[0].TextContent);

        // Cap message when errorCount exceeds the exposed 100-item list (spec §6).
        Assert.Contains($"Showing first 2 of {137:N0} errors", cut.Markup);
    }

    [Fact]
    public void PollNetworkFailure_ShowsReconnectLine_NotAJobFailure()
    {
        // Spec §3: a failed poll costs visibility, never job state.
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}")
            .Respond(_ => throw new HttpRequestException("offline"));

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "exercises.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-reconnect");
        Assert.Contains("No connection — reconnecting", cut.Find(".hs-import-reconnect").TextContent);

        // The job is still "Importing" — no failure copy anywhere.
        Assert.Contains("Importing", cut.Find(".hs-import-chip").TextContent);
        Assert.Empty(cut.FindAll(".hs-import-failure"));
        Assert.DoesNotContain("Stopped", cut.Markup);
    }

    [Fact]
    public void Reattach_ResumesPanelFromLocalStorageSnapshot()
    {
        // Regression: after a reload the panel must re-attach from the
        // "hs.import.job" snapshot and keep polling the same jobId.
        var storage = Substitute.For<ISyncLocalStorageService>();
        storage.ContainKey("hs.import.job").Returns(true);
        storage.GetItem<CsvImportTracker.JobSnapshot>("hs.import.job")
            .Returns(new CsvImportTracker.JobSnapshot(JobId, 10000, 4200, "running"));
        // Last registration wins on resolve — no need to remove the default stub.
        Services.AddSingleton<ISyncLocalStorageService>(storage);

        var polled = false;
        MockHttp.When($"/data/import/{JobId}").Respond(_ =>
        {
            polled = true;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(StatusJson("running", 4200), Encoding.UTF8, "application/json"),
            };
        });

        var cut = RenderComponent<Settings>();

        // Snapshot renders immediately (no fake 0%), then polling continues.
        cut.WaitForElement(".hs-import-panel");
        Assert.Contains($"{4200:N0} of {10000:N0}", cut.Find(".hs-import-count").TextContent);
        cut.WaitForState(() => polled);
    }

    [Fact]
    public void HideProgress_KeepsCompactRow_AndDoesNotCancelTheJob()
    {
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}").Respond("application/json", StatusJson("running", 4200));

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "exercises.csv", 1024);
        ClickButtonWithText(cut, "Import");
        cut.WaitForElement(".hs-import-panel");

        ClickButtonWithText(cut, "Hide progress");
        cut.WaitForState(() => !cut.FindAll(".hs-import-panel").Any());

        // The job stays visible as a compact row with View.
        Assert.NotEmpty(cut.FindAll(".hs-import-compact"));
        Assert.Contains("Import in progress", cut.Find(".hs-import-compact").TextContent);

        // View reopens the panel with the same job state.
        ClickButtonWithText(cut, "View");
        cut.WaitForElement(".hs-import-panel");

        // Hide ≠ cancel: MockHttp has no cancel/DELETE route, so an attempted
        // cancel would have thrown; the job is still busy and importing.
        Assert.Contains("Importing", cut.Find(".hs-import-chip").TextContent);
    }

    [Fact]
    public void Structural400_ListsErrorsInline_WithoutStartingAJob()
    {
        MockHttp.When(HttpMethod.Post, "/data/import")
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"errors\":[\"Missing required columns: date\"]}", Encoding.UTF8, "application/json"),
            });

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "bad.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-structural");
        Assert.Contains("Missing required columns: date", cut.Find(".hs-import-structural").TextContent);
        Assert.Empty(cut.FindAll(".hs-import-panel"));
    }

    [Fact]
    public void Structural400_PlainStringProblem_IsShownInline()
    {
        // Regression: the API may return a plain-string ProblemDetails detail
        // (e.g. "File too large (max 5 MB).") — it must not be lost.
        MockHttp.When(HttpMethod.Post, "/data/import")
            .Respond(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("{\"detail\":\"File too large (max 5 MB).\"}", Encoding.UTF8, "application/json"),
            });

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "big.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-structural");
        Assert.Contains("File too large (max 5 MB).", cut.Find(".hs-import-structural").TextContent);
        Assert.Empty(cut.FindAll(".hs-import-panel"));
    }

    [Fact]
    public void Pill_VisibleOnOtherPages_WhenJobIsRunning()
    {
        UseNavigationFor("/training/today");
        var tracker = RegisterLiveTracker(out var http);
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}").Respond("application/json", StatusJson("running", 4200));

        using (http)
        {
            var started = tracker.StartAsync(new MemoryStream(new byte[1024]), "exercises.csv", "text/csv")
                .GetAwaiter().GetResult();
            Assert.True(started.Started);

            var pill = RenderComponent<ImportProgressPill>();
            pill.WaitForElement(".hs-import-pill");
            Assert.Contains("Importing", pill.Find(".hs-import-pill-text").TextContent);
        }
        tracker.Dispose();
    }

    [Fact]
    public void Pill_HiddenOnSettingsPage_WhereThePanelAlreadyShowsProgress()
    {
        UseNavigationFor("/settings");
        var tracker = RegisterLiveTracker(out var http);
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}").Respond("application/json", StatusJson("running", 4200));

        using (http)
        {
            tracker.StartAsync(new MemoryStream(new byte[1024]), "exercises.csv", "text/csv")
                .GetAwaiter().GetResult();

            var pill = RenderComponent<ImportProgressPill>();
            Assert.Empty(pill.FindAll(".hs-import-pill"));
        }
        tracker.Dispose();
    }

    [Fact]
    public async Task Tracker_Poll404_SetsNotFoundAndClearsSnapshot()
    {
        // Job expired server-side: the UI must say so and the snapshot must go.
        var http = MockHttp.ToHttpClient();
        http.BaseAddress = new Uri("https://localhost:7278/");
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}").Respond(HttpStatusCode.NotFound);

        var storage = Substitute.For<ISyncLocalStorageService>();
        storage.ContainKey("hs.import.job").Returns(true);
        var tracker = new CsvImportTracker(http, storage);

        var (started, errors) = await tracker.StartAsync(new MemoryStream(new byte[1024]), "exercises.csv", "text/csv");
        Assert.True(started);
        Assert.Empty(errors);

        var deadline = DateTime.UtcNow.AddSeconds(8);
        while (tracker.Status != "not_found" && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
        }

        Assert.Equal("not_found", tracker.Status);
        Assert.False(tracker.IsBusy);
        storage.Received(2).RemoveItem("hs.import.job");
        tracker.Dispose();
    }

    [Fact]
    public void FailedJob_CopySaysNoRowsSaved_NoImportedCount()
    {
        // t_4965e7c7: with the single-transaction pipeline, "failed" implies 0
        // committed rows — the copy must state that nothing was saved and point
        // to re-uploading the (idempotent) CSV. The misleading
        // "X of Y rows were imported before the failure" is a regression.
        MockAcceptedImport();
        MockHttp.When($"/data/import/{JobId}")
            .Respond("application/json", StatusJson("failed", 5000, 0, "[]", "\"crash\""));

        var cut = RenderComponent<Settings>();
        SelectCsvFile(cut, "exercises.csv", 1024);
        ClickButtonWithText(cut, "Import");

        cut.WaitForElement(".hs-import-failure");
        var failure = cut.Find(".hs-import-failure").TextContent;
        Assert.Contains("No rows were saved", failure);
        Assert.Contains("Upload the same CSV again", failure);
        Assert.DoesNotContain("were imported", cut.Markup);
        Assert.DoesNotContain("of 10,000 rows", cut.Markup);
        Assert.DoesNotContain("5,000 of 10,000", cut.Markup);
    }

    /// <summary>
    /// Replaces bUnit's fake NavigationManager with a fixed-URI stub so the
    /// pill can be rendered as if the user were on a given page.
    /// </summary>
    private void UseNavigationFor(string pathAndQuery)
    {
        Services.AddSingleton<NavigationManager>(new FixedNavigationManager($"https://localhost{pathAndQuery}"));
    }

    private CsvImportTracker RegisterLiveTracker(out System.Net.Http.HttpClient http)
    {
        http = MockHttp.ToHttpClient();
        http.BaseAddress = new Uri("https://localhost:7278/");
        var tracker = new CsvImportTracker(http, Substitute.For<ISyncLocalStorageService>());
        Services.AddSingleton(tracker);
        return tracker;
    }

    private sealed class FixedNavigationManager : NavigationManager
    {
        public FixedNavigationManager(string uri) => Initialize("https://localhost/", uri);
    }
}
