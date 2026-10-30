/// <summary>
/// Single source for exercise photos (t_b67627aa decision).
///
/// The URL comes ONLY from the record's ImageRoute. An empty ImageRoute means "no
/// photo": no URL is built and the caller renders the CSS placeholder instead.
/// Deriving a path from the exercise name (the old `ExerciseImages/{Name}.jpg`)
/// is forbidden — it 404s on every card whose name has no matching asset, and a
/// name-based convention breaks wholesale on any rename or catalog import.
/// </summary>
public static class ExercisePhoto
{
    public static string? Url(string? imageRoute)
        => string.IsNullOrWhiteSpace(imageRoute) ? null : imageRoute;
}
