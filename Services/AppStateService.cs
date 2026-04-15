using System.Net.Http.Json;

/// <summary>
/// Singleton-scoped service that caches commonly fetched data
/// (exercise types, muscles, user roles) so they are fetched once per session.
/// </summary>
public class AppStateService
{
    private readonly HttpClient _http;

    private List<GetExerciseTypeResource>? _exerciseTypes;
    private List<Muscle>? _muscles;
    private string[]? _userRoles;

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
            _muscles = new List<Muscle>
            {
                new() { Id = -1, Name = "Chest",     MuscleGroup = "Chest" },
                new() { Id = -2, Name = "Back",       MuscleGroup = "Back" },
                new() { Id = -3, Name = "Legs",       MuscleGroup = "Legs" },
                new() { Id = -4, Name = "Shoulders",  MuscleGroup = "Shoulders" },
                new() { Id = -5, Name = "Arms",       MuscleGroup = "Arms" },
                new() { Id = -6, Name = "Core",       MuscleGroup = "Core" },
            };
        }

        return _muscles;
    }

    public string GetMuscleName(int? muscleId)
    {
        if (muscleId == null || _muscles == null) return "Unspecified";
        return _muscles.FirstOrDefault(m => m.Id == muscleId)?.Name ?? "Unspecified";
    }

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
}
