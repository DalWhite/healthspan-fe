using System.ComponentModel.DataAnnotations;

public static class ExerciseTagText
{
    public const int MaximumCount = 5;
    public const int MaximumLength = 30;

    public static List<string> Parse(string? text)
        => (text ?? string.Empty)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries)
            .Select(Normalize)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string Format(IEnumerable<string>? tags)
        => string.Join(", ", Normalize(tags));

    public static List<string> Normalize(IEnumerable<string>? tags)
        => (tags ?? [])
            .Select(Normalize)
            .Where(tag => tag.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static IEnumerable<ValidationResult> Validate(
        IEnumerable<string>? tags)
    {
        var normalized = Normalize(tags);
        if (normalized.Count > MaximumCount)
        {
            yield return new ValidationResult(
                $"Use no more than {MaximumCount} training tags.",
                [nameof(AddExerciseResource.Tags)]);
        }

        if (normalized.Any(tag => tag.Length > MaximumLength))
        {
            yield return new ValidationResult(
                $"Training tags must be {MaximumLength} characters or fewer.",
                [nameof(AddExerciseResource.Tags)]);
        }
    }

    private static string Normalize(string? tag)
        => string.Join(
            " ",
            (tag ?? string.Empty)
                .Trim()
                .TrimStart('#')
                .Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries));
}
