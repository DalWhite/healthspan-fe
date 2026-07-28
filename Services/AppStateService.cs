using System.Net.Http.Json;

/// <summary>
/// Singleton-scoped service that caches commonly fetched data
/// (exercise types, muscles, user roles) so they are fetched once per session.
/// </summary>
public class AppStateService
{
    private readonly HttpClient _http;
    private static readonly List<Muscle> FallbackMuscles =
    [
        new() { Id = 1, Slug = "chest", Name = "Chest", Side = "front", MuscleGroup = "Chest", SortOrder = 10 },
        new() { Id = 2, Slug = "front-delts", Name = "Front delts", Side = "front", MuscleGroup = "Shoulders", SortOrder = 20 },
        new() { Id = 3, Slug = "side-delts", Name = "Side delts", Side = "both", MuscleGroup = "Shoulders", SortOrder = 30 },
        new() { Id = 4, Slug = "rear-delts", Name = "Rear delts", Side = "back", MuscleGroup = "Shoulders", SortOrder = 40 },
        new() { Id = 5, Slug = "lats", Name = "Lats", Side = "back", MuscleGroup = "Back", SortOrder = 50 },
        new() { Id = 6, Slug = "upper-back", Name = "Upper back", Side = "back", MuscleGroup = "Back", SortOrder = 60 },
        new() { Id = 7, Slug = "traps", Name = "Traps", Side = "back", MuscleGroup = "Back", SortOrder = 70 },
        new() { Id = 8, Slug = "biceps", Name = "Biceps", Side = "front", MuscleGroup = "Arms", SortOrder = 80 },
        new() { Id = 9, Slug = "triceps", Name = "Triceps", Side = "back", MuscleGroup = "Arms", SortOrder = 90 },
        new() { Id = 10, Slug = "forearms", Name = "Forearms", Side = "both", MuscleGroup = "Arms", SortOrder = 100 },
        new() { Id = 11, Slug = "abs", Name = "Abs", Side = "front", MuscleGroup = "Core", SortOrder = 110 },
        new() { Id = 12, Slug = "obliques", Name = "Obliques", Side = "front", MuscleGroup = "Core", SortOrder = 120 },
        new() { Id = 13, Slug = "serratus", Name = "Serratus", Side = "front", MuscleGroup = "Core", SortOrder = 130 },
        new() { Id = 14, Slug = "hip-flexors", Name = "Hip flexors", Side = "front", MuscleGroup = "Legs", SortOrder = 140 },
        new() { Id = 15, Slug = "glutes", Name = "Glutes", Side = "back", MuscleGroup = "Glutes", SortOrder = 150 },
        new() { Id = 16, Slug = "quads", Name = "Quads", Side = "front", MuscleGroup = "Legs", SortOrder = 160 },
        new() { Id = 17, Slug = "hamstrings", Name = "Hamstrings", Side = "back", MuscleGroup = "Legs", SortOrder = 170 },
        new() { Id = 18, Slug = "adductors", Name = "Adductors", Side = "front", MuscleGroup = "Legs", SortOrder = 180 },
        new() { Id = 19, Slug = "abductors", Name = "Abductors", Side = "back", MuscleGroup = "Legs", SortOrder = 190 },
        new() { Id = 20, Slug = "calves", Name = "Calves", Side = "both", MuscleGroup = "Legs", SortOrder = 200 }
    ];

    private List<GetExerciseTypeResource>? _exerciseTypes;
    private List<Muscle>? _muscles;
    private string[]? _userRoles;
    public ExerciseLibraryViewState ExerciseLibraryView { get; } = new();

    public AppStateService(HttpClient http)
    {
        _http = http;
    }

    // ── Exercise Types ─────────────────────────────────────────────────────────

    public async Task<List<GetExerciseTypeResource>> GetExerciseTypesAsync()
    {
        if (_exerciseTypes != null) return _exerciseTypes;

        try
        {
            _exerciseTypes = await _http.GetFromJsonAsync<List<GetExerciseTypeResource>>("/ExerciseTypes")
                             ?? new List<GetExerciseTypeResource>();
        }
        catch
        {
            _exerciseTypes = new List<GetExerciseTypeResource>();
        }

        return _exerciseTypes;
    }

    public void InvalidateExerciseTypes() => _exerciseTypes = null;

    // ── Muscles ────────────────────────────────────────────────────────────────

    public async Task<List<Muscle>> GetMusclesAsync()
    {
        if (_muscles != null) return _muscles;

        try
        {
            _muscles = await _http.GetFromJsonAsync<List<Muscle>>("api/muscles")
                       ?? new List<Muscle>();
        }
        catch
        {
            _muscles = FallbackMuscles.Select(CloneMuscle).ToList();
        }

        _muscles = _muscles
            .OrderBy(muscle => muscle.SortOrder <= 0 ? int.MaxValue : muscle.SortOrder)
            .ThenBy(muscle => muscle.Name)
            .ToList();

        return _muscles;
    }

    public string GetMuscleName(int? muscleId)
    {
        if (muscleId == null || _muscles == null) return "Unspecified";
        return _muscles.FirstOrDefault(m => m.Id == muscleId)?.Name ?? "Unspecified";
    }

    public IReadOnlyDictionary<int, Muscle> GetMuscleLookup()
        => (_muscles ?? [])
            .Where(muscle => muscle.Id.HasValue)
            .ToDictionary(muscle => muscle.Id!.Value, CloneMuscle);

    public List<string> GetMuscleGroups()
        => (_muscles ?? [])
            .Select(muscle => muscle.MuscleGroup)
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Select(group => group!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => group)
            .ToList();

    public string FormatExerciseTypeMuscleLine(GetExerciseTypeResource? exerciseType, string fallback = "Muscle target not mapped")
        => ExerciseMuscleAssignmentHelper.FormatMuscleLine(exerciseType?.MuscleAssignments, GetMuscleLookup(), fallback);

    // ── User Roles ─────────────────────────────────────────────────────────────

    public async Task<string[]> GetUserRolesAsync()
    {
        if (_userRoles != null) return _userRoles;

        try
        {
            _userRoles = await _http.GetFromJsonAsync<string[]>("/Users/roles")
                         ?? Array.Empty<string>();
        }
        catch
        {
            _userRoles = Array.Empty<string>();
        }

        return _userRoles;
    }

    public void InvalidateUserRoles() => _userRoles = null;

    // ── Search Helpers ─────────────────────────────────────────────────────────

    public static bool MatchesSearch(GetExerciseTypeResource item, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        return (item.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Tags?.Any(tag =>
                tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                tag.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false)
            || (item.Variations?.Any(variation =>
                (variation.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (variation.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (variation.Tags?.Any(tag =>
                    tag.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                    tag.Category.Contains(query, StringComparison.OrdinalIgnoreCase)) ?? false)) ?? false);
    }

    public static bool MatchesSearch(GetExerciseResource item, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string? dateStr = item.Workout?.Date?.ToString("dd/MM/yyyy");
        return (dateStr?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Type?.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false)
            || (item.Type?.Description?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    public static bool MatchesSearch(GetWorkoutResource item, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string dateStr = item.Date?.ToString("dd/MM/yyyy") ?? "";
        return dateStr.Contains(query, StringComparison.OrdinalIgnoreCase)
            || (item.Type?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static Muscle CloneMuscle(Muscle muscle)
        => new()
        {
            Id = muscle.Id,
            Slug = muscle.Slug,
            Name = muscle.Name,
            Position = muscle.Position,
            Side = muscle.Side,
            MuscleGroup = muscle.MuscleGroup,
            SortOrder = muscle.SortOrder
        };
}

public sealed class ExerciseLibraryViewState
{
    public string Search { get; set; } = string.Empty;
    public string? MuscleGroup { get; set; }
    public string? Equipment { get; set; }
    public string? Movement { get; set; }
    public bool FavouritesOnly { get; set; }
    public bool FiltersExpanded { get; set; }
}
