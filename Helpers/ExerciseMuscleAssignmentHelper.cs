public static class ExerciseMuscleAssignmentHelper
{
    public static IReadOnlyList<ExerciseTypeMuscleAssignmentResource> OrderAssignments(IEnumerable<ExerciseTypeMuscleAssignmentResource>? assignments)
        => (assignments ?? [])
            .Where(assignment => assignment.MuscleId > 0)
            .OrderBy(assignment => NormalizeRole(assignment.Role))
            .ThenBy(assignment => assignment.SortOrder)
            .ThenBy(assignment => assignment.MuscleId)
            .ToList();

    public static IReadOnlyList<ExerciseTypeMuscleAssignmentResource> GetAssignmentsByRole(
        IEnumerable<ExerciseTypeMuscleAssignmentResource>? assignments,
        string role)
        => OrderAssignments(assignments)
            .Where(assignment => string.Equals(NormalizeRole(assignment.Role), NormalizeRole(role), StringComparison.OrdinalIgnoreCase))
            .ToList();

    public static IReadOnlyList<string> GetMuscleNames(
        IEnumerable<ExerciseTypeMuscleAssignmentResource>? assignments,
        IReadOnlyDictionary<int, Muscle> musclesById)
        => OrderAssignments(assignments)
            .Select(assignment => musclesById.TryGetValue(assignment.MuscleId, out var muscle)
                ? muscle.Name
                : $"Muscle {assignment.MuscleId}")
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static string FormatMuscleLine(
        IEnumerable<ExerciseTypeMuscleAssignmentResource>? assignments,
        IReadOnlyDictionary<int, Muscle> musclesById,
        string fallback = "Muscle target not mapped")
    {
        var primaryMuscles = GetMuscleNames(GetAssignmentsByRole(assignments, "Primary"), musclesById);
        var secondaryMuscles = GetMuscleNames(GetAssignmentsByRole(assignments, "Secondary"), musclesById);

        if (primaryMuscles.Count > 0 && secondaryMuscles.Count > 0)
        {
            return $"{string.Join(", ", primaryMuscles)} · {string.Join(", ", secondaryMuscles)}";
        }

        if (primaryMuscles.Count > 0)
        {
            return string.Join(", ", primaryMuscles);
        }

        if (secondaryMuscles.Count > 0)
        {
            return string.Join(", ", secondaryMuscles);
        }

        return fallback;
    }

    public static IReadOnlyList<MuscleLoadShare> DistributeLoad(
        IEnumerable<ExerciseTypeMuscleAssignmentResource>? assignments,
        double load)
    {
        if (load <= 0)
        {
            return Array.Empty<MuscleLoadShare>();
        }

        var primaryAssignments = GetAssignmentsByRole(assignments, "Primary");
        var secondaryAssignments = GetAssignmentsByRole(assignments, "Secondary");
        var distributedLoads = new List<MuscleLoadShare>();

        if (primaryAssignments.Count > 0 && secondaryAssignments.Count > 0)
        {
            distributedLoads.AddRange(DistributeRoleLoad(primaryAssignments, "Primary", load * 0.7d));
            distributedLoads.AddRange(DistributeRoleLoad(secondaryAssignments, "Secondary", load * 0.3d));
            return distributedLoads;
        }

        if (primaryAssignments.Count > 0)
        {
            return DistributeRoleLoad(primaryAssignments, "Primary", load);
        }

        if (secondaryAssignments.Count > 0)
        {
            return DistributeRoleLoad(secondaryAssignments, "Secondary", load);
        }

        return Array.Empty<MuscleLoadShare>();
    }

    private static IReadOnlyList<MuscleLoadShare> DistributeRoleLoad(
        IReadOnlyList<ExerciseTypeMuscleAssignmentResource> assignments,
        string role,
        double load)
    {
        if (assignments.Count == 0 || load <= 0)
        {
            return Array.Empty<MuscleLoadShare>();
        }

        var loadPerMuscle = load / assignments.Count;
        return assignments
            .Select(assignment => new MuscleLoadShare(assignment.MuscleId, role, loadPerMuscle))
            .ToList();
    }

    private static string NormalizeRole(string? role)
        => string.Equals(role, "Secondary", StringComparison.OrdinalIgnoreCase)
            ? "Secondary"
            : "Primary";
}

public readonly record struct MuscleLoadShare(int MuscleId, string Role, double Load);
