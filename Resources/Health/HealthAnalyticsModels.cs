public static class HealthRoutes
{
    public const string BloodTests = "/BloodTests";
    public const string BloodTestsPendingReview = "/BloodTests/pending-review";
    public const string BloodTestUpload = "/BloodTests/upload";
    public const string BloodTestManual = "/BloodTests/manual";
    public const string BiomarkerSnapshot = "/BloodTests/markers/latest";
    public const string BiomarkerTrendPrefix = "/BloodTests/markers";
    public const string GalpinCatalog = "/GalpinAssessments/catalog";
    public const string GalpinAssessments = "/GalpinAssessments";
    public const string GalpinLatest = "/GalpinAssessments/latest";
    public const string GalpinTrendPrefix = "/GalpinAssessments/trends";
    public const string DashboardSummary = "/Dashboard/summary";
    public const string HealthProfile = "/HealthProfile";
}

public enum BloodTestProcessingStatus
{
    PendingReview = 0,
    Processed = 1,
    ExtractionFailed = 2,
    Processing = 3
}

public sealed class BloodTestDocumentContent
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}

public enum MarkerStatus
{
    Unknown = 0,
    InRange = 1,
    Low = 2,
    High = 3,
    RequiresReview = 4
}

public enum GalpinAdaptationType
{
    Skill = 0,
    Speed = 1,
    Power = 2,
    Strength = 3,
    Hypertrophy = 4,
    MuscularEndurance = 5,
    AnaerobicCapacity = 6,
    MaximalAerobicPower = 7,
    LongDurationEndurance = 8
}

public class GetBloodTestResource
{
    public Guid Id { get; set; }
    public DateTime TestDate { get; set; }
    public string? LabName { get; set; }
    public string? SourceFileName { get; set; }
    public BloodTestProcessingStatus ProcessingStatus { get; set; }
    public string? ProcessingNotes { get; set; }
    public bool RequiresReview { get; set; }
    public List<BloodTestMarkerResource>? Markers { get; set; }
}

public class BloodTestMarkerResource
{
    public string? Name { get; set; }
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public string? RawValueText { get; set; }
    public double? Confidence { get; set; }
    public MarkerStatus Status { get; set; }
    public double? ReferenceMinValue { get; set; }
    public double? ReferenceMaxValue { get; set; }
}

public class AddBloodTestResource
{
    public DateTime TestDate { get; set; } = DateTime.Today;
    public string? LabName { get; set; }
    public List<BloodTestMarkerResource>? Markers { get; set; }
}

public class BloodTestUploadRequest
{
    public DateTime? TestDate { get; set; } = DateTime.Today;
    public string? LabName { get; set; }
}

public class ReviewBloodTestResource
{
    public DateTime TestDate { get; set; } = DateTime.Today;
    public string? LabName { get; set; }
    public List<BloodTestMarkerResource>? Markers { get; set; }
}

public class LatestMarkerSnapshotResource
{
    public string? MetricKey { get; set; }
    public string? DisplayName { get; set; }
    public double? Value { get; set; }
    public string? Unit { get; set; }
    public MarkerStatus Status { get; set; }
    public DateTime TestDate { get; set; }
    public double? ReferenceMinValue { get; set; }
    public double? ReferenceMaxValue { get; set; }
}

public class GetMarkerTrendResource
{
    public string? MetricKey { get; set; }
    public List<GetMarkerTrendPointResource>? Points { get; set; }
}

public class GetMarkerTrendPointResource
{
    public DateTime Time { get; set; }
    public double Value { get; set; }
    public string? Unit { get; set; }
    public MarkerStatus Status { get; set; }
}

public class GalpinAdaptationCatalogItemResource
{
    public GalpinAdaptationType Adaptation { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
}

public class GalpinAssessmentScoreResource
{
    public GalpinAdaptationType Adaptation { get; set; }
    public double? Score { get; set; }
    public string? EvidenceNotes { get; set; }
}

public class AddGalpinAssessmentResource
{
    public DateTime AssessedOnUtc { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<GalpinAssessmentScoreResource>? Scores { get; set; }
}

public class GetGalpinAssessmentResource
{
    public Guid Id { get; set; }
    public DateTime AssessedOnUtc { get; set; }
    public string? Notes { get; set; }
    public List<GalpinAssessmentScoreResource>? Scores { get; set; }
}

public class GetGalpinAdaptationTrendResource
{
    public GalpinAdaptationType Adaptation { get; set; }
    public List<GetGalpinAdaptationTrendPointResource>? Points { get; set; }
}

public class GetGalpinAdaptationTrendPointResource
{
    public DateTime Time { get; set; }
    public double Score { get; set; }
}

public class GetHealthDashboardSummaryResource
{
    public int RecentWorkoutCount { get; set; }
    public int BloodTestCount { get; set; }
    public DateTime? LatestBloodTestDate { get; set; }
    public int OutOfRangeMarkersInLatestBloodTest { get; set; }
    public DateTime? LatestAssessmentDate { get; set; }
    public double? LatestAssessmentAverageScore { get; set; }
}

public class TrendSeriesResource
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public string? Color { get; set; }
    public List<TrendPointResource> Points { get; set; } = new();
}

public class TrendPointResource
{
    public DateTime Date { get; set; }
    public decimal? Value { get; set; }
}
