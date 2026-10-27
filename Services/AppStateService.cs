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

    private readonly List<GetExerciseTypeResource> _exerciseTypes = new();
    private readonly Dictionary<int, int> _exerciseTypesById = new();
    private Task? _exerciseTypesFirstPage;
    private Task? _catalogHydration;
    private int _catalogGeneration;
    private List<Muscle>? _muscles;
    private string[]? _userRoles;
    public ExerciseLibraryViewState ExerciseLibraryView { get; } = new();

    public AppStateService(HttpClient http)
    {
        _http = http;
    }

    // ── Exercise Types ─────────────────────────────────────────────────────────

    /// <summary>
    /// First page of the catalog, then the rest in the background. Parsing the
    /// whole 549KB catalog in WASM costs ~2.5s of main-thread time on a cold
    /// start, which is what kept the library on its skeleton for 9s (t_81a400b3).
    /// Callers get a usable list immediately (one screen of cards) and subscribe
    /// to <see cref="ExerciseTypesUpdated"/> to re-render as the rest arrives.
    /// </summary>
    public async Task<List<GetExerciseTypeResource>> GetExerciseTypesAsync()
    {
        _exerciseTypesFirstPage ??= LoadFirstCatalogPageAsync();
        await _exerciseTypesFirstPage;
        return _exerciseTypes;
    }

    /// <summary>Copy of the rows that arrived so far (safe to hold across chunks).</summary>
    public GetExerciseTypeResource[] GetExerciseTypesSnapshot() => _exerciseTypes.ToArray();

    /// <summary>
    /// The whole catalog, waiting for the background hydration to settle. Only for
    /// screens that cannot work on one page: the parent-type picker and the
    /// validation behind it (t_81a400b3).
    /// </summary>
    public async Task<GetExerciseTypeResource[]> GetFullExerciseTypesAsync()
    {
        await GetExerciseTypesAsync();
        if (_catalogHydration is { } hydration)
        {
            await hydration;
        }

        return GetExerciseTypesSnapshot();
    }

    /// <summary>Total rows the server has, known from the first page.</summary>
    public int ExerciseTypesTotal { get; private set; }

    public bool ExerciseTypesHydrated { get; private set; }

    /// <summary>Raised on the sync context after each catalog chunk lands.</summary>
    public event Action? ExerciseTypesUpdated;

    private const int CatalogFirstPageSize = 40;
    private const int CatalogChunkSize = 160;

    private async Task LoadFirstCatalogPageAsync()
    {
        var generation = _catalogGeneration;
        try
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var page = await ExerciseCatalogJson.GetPageAsync(_http, 0, CatalogFirstPageSize);
            if (generation != _catalogGeneration)
            {
                return;
            }

            MergeCatalog(page.Items);
            ExerciseTypesTotal = page.Total;
            Console.WriteLine($"HSPERF catalog first page={sw.ElapsedMilliseconds}ms rows={page.Items.Count}/{page.Total}");
            _catalogHydration ??= HydrateCatalogAsync(generation, page.Total);
        }
        catch
        {
            ExerciseTypesHydrated = true;
        }
    }

    private async Task HydrateCatalogAsync(int generation, int total)
    {
        var loaded = 0;
        try
        {
            while (loaded < total)
            {
                // Yield first: the page that opened the library must finish its
                // render before this chunk claims the main thread, and every chunk
                // is fetched with the full projection so the description that
                // search reads is not lost on the way (t_81a400b3).
                await Task.Yield();
                var chunkWatch = System.Diagnostics.Stopwatch.StartNew();
                var page = await ExerciseCatalogJson.GetPageAsync(_http, loaded, CatalogChunkSize, full: true);
                if (generation != _catalogGeneration || page.Items.Count == 0)
                {
                    break;
                }

                MergeCatalog(page.Items);
                loaded += page.Items.Count;
                Console.WriteLine($"HSPERF catalog chunk at={loaded - page.Items.Count} rows={page.Items.Count} ms={chunkWatch.ElapsedMilliseconds} total={total}");
                ExerciseTypesUpdated?.Invoke();
            }
        }
        catch
        {
            // A partial catalog still lists and searches what arrived.
        }
        finally
        {
            if (generation == _catalogGeneration)
            {
                ExerciseTypesHydrated = true;
                ExerciseTypesUpdated?.Invoke();
            }
        }
    }

    /// <summary>
    /// Rows arrive twice: the paint page is slim, the hydration chunks are full.
    /// Keep one row per id, replacing the slim copy in place with the full one so
    /// the first screen keeps its order (t_81a400b3).
    /// </summary>
    private void MergeCatalog(IReadOnlyList<GetExerciseTypeResource> items)
    {
        foreach (var item in items)
        {
            if (_exerciseTypesById.TryGetValue(item.Id, out var index))
            {
                _exerciseTypes[index] = item;
                continue;
            }

            _exerciseTypesById[item.Id] = _exerciseTypes.Count;
            _exerciseTypes.Add(item);
        }
    }

    public void InvalidateExerciseTypes()
    {
        // Bumping the generation makes a hydration that is already in flight drop
        // its chunks instead of patching a list this call just cleared.
        _catalogGeneration++;
        _exerciseTypes.Clear();
        _exerciseTypesById.Clear();
        _exerciseTypesFirstPage = null;
        _catalogHydration = null;
        ExerciseTypesTotal = 0;
        ExerciseTypesHydrated = false;
    }

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
    public int Page { get; set; } = 1;
}
