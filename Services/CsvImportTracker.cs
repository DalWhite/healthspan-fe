using System.Net;
using System.Text.Json;
using Blazored.LocalStorage;

/// <summary>
/// Client side of the async CSV import contract (t_c1f8e6ac):
/// POST /data/import answers 202 {jobId,total} and the FE polls
/// GET /data/import/{jobId} for real progress instead of holding a 300 s
/// request open (ux spec t_d1db9866).
///
/// Singleton so the job survives navigation: Settings renders the inline
/// panel, the layout pill shows progress on other pages, and a reload
/// re-attaches from the localStorage snapshot (key "hs.import.job").
/// A polling network failure is NOT a job failure — the job runs on the
/// server; the UI only loses visibility (spec §3, rule "fallo de red ≠ job
/// fallado").
/// </summary>
public sealed class CsvImportTracker : IDisposable
{
    // Mirrors the backend limits (CsvService.MaxImportBytes / MaxImportRows)
    // so the client preflight and the upload stream agree with the API.
    public const long MaxUploadBytes = 5 * 1024 * 1024;
    public const int MaxUploadRows = 50_000;
    public const string StorageKey = "hs.import.job";

    private static readonly TimeSpan NormalInterval = TimeSpan.FromMilliseconds(1200);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan PollTimeout = TimeSpan.FromSeconds(10);

    private readonly HttpClient _http;
    private readonly ISyncLocalStorageService _storage;

    private CancellationTokenSource? _pollCts;
    private bool _attachTried;
    private DateTime? _lastSampleAt;
    private int _lastSampleProcessed;

    public CsvImportTracker(HttpClient http, ISyncLocalStorageService storage)
    {
        _http = http;
        _storage = storage;
    }

    /// <summary>Subscribed by Settings and the layout pill to re-render.</summary>
    public event Action? Changed;

    public string? JobId { get; private set; }
    public string Status { get; private set; } = "idle";
    public int Total { get; private set; }
    public int Processed { get; private set; }
    public int Skipped { get; private set; }
    public int ErrorCount { get; private set; }
    public IReadOnlyList<ImportRowError> Errors { get; private set; } = [];
    public string? FailureReason { get; private set; }

    /// <summary>Last poll failed; the job itself may still be running.</summary>
    public bool PollingFailed { get; private set; }

    /// <summary>Rows/s measured between consecutive successful polls.</summary>
    public double? RowsPerSecond { get; private set; }

    public DateTime? StartedAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    public bool HasJob => JobId is not null;
    public bool IsBusy => HasJob && Status is "queued" or "running";
    public bool IsTerminal => Status is "completed" or "completed_with_errors"
        or "failed" or "cancelled" or "not_found";
    public double Percent => Total > 0 ? Math.Clamp(Processed * 100.0 / Total, 0, 100) : 0;

    public TimeSpan Elapsed
    {
        get
        {
            if (StartedAt is null)
            {
                return TimeSpan.Zero;
            }
            var elapsed = (FinishedAt ?? DateTime.UtcNow) - StartedAt.Value;
            // Server/client clock skew must never render a negative elapsed.
            return elapsed < TimeSpan.Zero ? TimeSpan.Zero : elapsed;
        }
    }

    /// <summary>
    /// POSTs the file. Returns (started, structuralErrors): a 400 from the
    /// structural preflight (bad header, >50k rows) yields errors and no job;
    /// a 202 starts the poller and persists the snapshot.
    /// </summary>
    public async Task<(bool Started, IReadOnlyList<string> StructuralErrors)> StartAsync(
        Stream fileStream, string fileName, string contentType)
    {
        // A new import replaces whatever terminal job was on screen.
        Dismiss();

        using var content = new MultipartFormDataContent();
        var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            string.IsNullOrWhiteSpace(contentType) ? "text/csv" : contentType);
        content.Add(fileContent, "file", fileName);

        HttpResponseMessage response;
        try
        {
            response = await _http.PostAsync("/data/import", content);
        }
        catch (Exception)
        {
            return (false, ["Could not reach the server. Check your connection and try again."]);
        }

        using (response)
        {
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                return (false, await ReadBadRequestErrorsAsync(response));
            }

            if (!response.IsSuccessStatusCode)
            {
                return (false, ["Import failed. Please check your file and try again."]);
            }

            var body = await response.Content.ReadAsStringAsync();
            var dto = TryParseJson<ImportAcceptedDto>(body);
            if (dto?.JobId is null)
            {
                return (false, ["Import failed. Please check your file and try again."]);
            }

            JobId = dto.JobId;
            Total = dto.Total;
            Status = "queued";
            Processed = 0;
            Skipped = 0;
            ErrorCount = 0;
            Errors = [];
            FailureReason = null;
            PollingFailed = false;
            RowsPerSecond = null;
            StartedAt = null;
            FinishedAt = null;
            _lastSampleAt = null;
            _lastSampleProcessed = 0;

            SaveSnapshot();
            Changed?.Invoke();
            StartPollLoop();
            return (true, []);
        }
    }

    /// <summary>
    /// Re-attach after a reload or on app start: if a non-terminal snapshot is
    /// in localStorage, fetch its current state once and keep polling; if it
    /// is terminal, show the result (cleared only via Done/Dismiss).
    /// Idempotent — Settings and MainLayout both call it.
    /// </summary>
    public async Task AttachAsync()
    {
        if (_attachTried || HasJob)
        {
            return;
        }
        _attachTried = true;

        if (!_storage.ContainKey(StorageKey))
        {
            return;
        }

        JobSnapshot? snapshot;
        try
        {
            snapshot = _storage.GetItem<JobSnapshot>(StorageKey);
        }
        catch (Exception)
        {
            snapshot = null;
        }

        if (snapshot?.JobId is null)
        {
            RemoveSnapshot();
            return;
        }

        JobId = snapshot.JobId;
        Total = snapshot.Total;
        Processed = snapshot.Processed;
        Status = snapshot.Status;
        Changed?.Invoke();

        if (IsTerminal)
        {
            return;
        }

        // First sample immediately (progress visible < 1 s after re-attach).
        await PollOnceAsync(CancellationToken.None);
        Changed?.Invoke();
        if (!IsTerminal)
        {
            StartPollLoop();
        }
    }

    /// <summary>Done / Dismiss: clears the panel and the persisted snapshot.</summary>
    public void Dismiss()
    {
        _pollCts?.Cancel();
        _pollCts = null;
        JobId = null;
        Status = "idle";
        Total = 0;
        Processed = 0;
        Skipped = 0;
        ErrorCount = 0;
        Errors = [];
        FailureReason = null;
        PollingFailed = false;
        RowsPerSecond = null;
        StartedAt = null;
        FinishedAt = null;
        _lastSampleAt = null;
        _lastSampleProcessed = 0;
        RemoveSnapshot();
        Changed?.Invoke();
    }

    private void StartPollLoop()
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        var token = _pollCts.Token;
        _ = Task.Run(() => PollLoopAsync(token), token);
    }

    private async Task PollLoopAsync(CancellationToken token)
    {
        // First poll fires immediately after the 202 (target: progress visible
        // < 1 s); afterwards 1,2 s, backing off 1,2 → 2,4 → 4,8 → 10 s on
        // network errors and resetting to 1,2 s on success (spec §4).
        var backoff = NormalInterval;
        var first = true;
        while (!token.IsCancellationRequested)
        {
            if (!first)
            {
                try
                {
                    await Task.Delay(backoff, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }
            first = false;

            var ok = await PollOnceAsync(token);
            if (IsTerminal || token.IsCancellationRequested)
            {
                return;
            }
            backoff = ok ? NormalInterval : Min(backoff * 2, MaxBackoff);
        }
    }

    private async Task<bool> PollOnceAsync(CancellationToken outer)
    {
        if (JobId is null)
        {
            return false;
        }

        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(outer);
            timeout.CancelAfter(PollTimeout);
            var response = await _http.GetAsync($"/data/import/{JobId}", timeout.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                // Job state is in-memory server-side; after a restart or >24 h
                // the id is gone. Terminal for the UI, not a "failed" import.
                PollingFailed = false;
                Status = "not_found";
                RemoveSnapshot();
                Changed?.Invoke();
                return true;
            }

            if (!response.IsSuccessStatusCode)
            {
                PollingFailed = true;
                Changed?.Invoke();
                return false;
            }

            var body = await response.Content.ReadAsStringAsync(timeout.Token);
            var dto = TryParseJson<ImportJobStatusDto>(body);
            if (dto is null)
            {
                PollingFailed = true;
                Changed?.Invoke();
                return false;
            }

            PollingFailed = false;
            Apply(dto);
            return true;
        }
        catch (OperationCanceledException) when (!outer.IsCancellationRequested)
        {
            PollingFailed = true;
            Changed?.Invoke();
            return false;
        }
        catch (HttpRequestException)
        {
            PollingFailed = true;
            Changed?.Invoke();
            return false;
        }
        catch (TaskCanceledException)
        {
            PollingFailed = true;
            Changed?.Invoke();
            return false;
        }
    }

    private void Apply(ImportJobStatusDto dto)
    {
        Status = dto.Status ?? Status;
        Total = dto.Total ?? Total;
        Processed = dto.Processed ?? Processed;
        Skipped = dto.Skipped ?? Skipped;
        ErrorCount = dto.ErrorCount ?? ErrorCount;
        Errors = dto.Errors ?? [];
        FailureReason = dto.FailureReason;
        StartedAt = dto.StartedAt ?? StartedAt;
        FinishedAt = dto.FinishedAt;

        // Rate measured between consecutive polls; honest, never interpolated
        // (the backend advances in ~1000-row batches).
        var now = DateTime.UtcNow;
        if (_lastSampleAt is not null)
        {
            var dt = (now - _lastSampleAt.Value).TotalSeconds;
            if (dt > 0.5 && Processed >= _lastSampleProcessed)
            {
                RowsPerSecond = (Processed - _lastSampleProcessed) / dt;
            }
        }
        _lastSampleAt = now;
        _lastSampleProcessed = Processed;

        SaveSnapshot();
        Changed?.Invoke();
    }

    private static async Task<IReadOnlyList<string>> ReadBadRequestErrorsAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        var dto = TryParseJson<ImportBadRequestDto>(body);
        if (dto?.Errors is { Count: > 0 })
        {
            return dto.Errors;
        }
        if (!string.IsNullOrWhiteSpace(dto?.Detail))
        {
            return [dto.Detail!];
        }
        return string.IsNullOrWhiteSpace(body)
            ? ["Import failed. Please check your file and try again."]
            : [body.Trim('"', ' ')];
    }

    private void SaveSnapshot()
    {
        if (JobId is null)
        {
            return;
        }
        try
        {
            _storage.SetItem(StorageKey, new JobSnapshot(JobId, Total, Processed, Status));
        }
        catch (Exception)
        {
            // Storage unavailable (private mode, quota): progress still works
            // for this page visit, it just won't survive a reload.
        }
    }

    private void RemoveSnapshot()
    {
        try
        {
            if (_storage.ContainKey(StorageKey))
            {
                _storage.RemoveItem(StorageKey);
            }
        }
        catch (Exception)
        {
        }
    }

    private static T? TryParseJson<T>(string body) where T : class
    {
        try
        {
            return JsonSerializer.Deserialize<T>(body,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a < b ? a : b;

    public void Dispose() => _pollCts?.Cancel();

    /// <summary>localStorage snapshot (key "hs.import.job") used to re-attach.</summary>
    public sealed record JobSnapshot(string JobId, int Total, int Processed, string Status);

    private sealed record ImportAcceptedDto
    {
        public string? JobId { get; init; }
        public int Total { get; init; }
    }

    private sealed record ImportJobStatusDto
    {
        public string? Status { get; init; }
        public int? Total { get; init; }
        public int? Processed { get; init; }
        public int? Skipped { get; init; }
        public int? ErrorCount { get; init; }
        public List<ImportRowError>? Errors { get; init; }
        public DateTime? StartedAt { get; init; }
        public DateTime? FinishedAt { get; init; }
        public string? FailureReason { get; init; }
    }

    private sealed record ImportBadRequestDto
    {
        public List<string>? Errors { get; init; }
        /// <summary>BadRequest(string) in ASP.NET Core surfaces as ProblemDetails.detail.</summary>
        public string? Detail { get; init; }
    }
}

/// <summary>API row error: { row, message } (capped server-side at 100).</summary>
public sealed record ImportRowError
{
    public int Row { get; init; }
    public string Message { get; init; } = string.Empty;
}
