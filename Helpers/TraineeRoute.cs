// Single source of truth for trainee query fragments and trainee routes.
// The canonical FE name is traineeId; the Exercises/ExerciseTypes API family
// still expects userId, so that mapping is encapsulated here instead of being
// re-implemented (and drifting) on every trainee page.
public static class TraineeRoute
{
    public static string Escape(string? traineeId)
        => string.IsNullOrWhiteSpace(traineeId)
            ? string.Empty
            : Uri.EscapeDataString(traineeId);

    // Canonical "traineeId=…" fragment: Workouts API and FE routes.
    public static string TraineeQuery(string? traineeId, bool first = true)
        => string.IsNullOrWhiteSpace(traineeId)
            ? string.Empty
            : $"{(first ? "?" : "&")}traineeId={Uri.EscapeDataString(traineeId)}";

    // "userId=…" fragment for the Exercises/ExerciseTypes API family.
    public static string ApiUserQuery(string? traineeId, bool first = true)
        => string.IsNullOrWhiteSpace(traineeId)
            ? string.Empty
            : $"{(first ? "?" : "&")}userId={Uri.EscapeDataString(traineeId)}";

    // Raw "userId=…" part for queries assembled from a parts list.
    public static string ApiUserParam(string? traineeId)
        => string.IsNullOrWhiteSpace(traineeId)
            ? string.Empty
            : $"userId={Uri.EscapeDataString(traineeId)}";

    // Own user opens the library; a trainee opens the scoped details route.
    public static string TypeDetailsRoute(int exerciseTypeId, string? traineeId)
        => string.IsNullOrWhiteSpace(traineeId)
            ? $"/training/library/{exerciseTypeId}"
            : $"/exercises/types/{exerciseTypeId}/details/{Uri.EscapeDataString(traineeId)}";
}
