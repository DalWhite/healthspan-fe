public static class ExerciseLoadCalculator
{
    public static void NormalizeForSubmission(AddExerciseResource exercise, decimal? defaultBodyWeight = null)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        switch (exercise.WeightType ?? ExerciseWeightType.Standard)
        {
            case ExerciseWeightType.BodyWeight:
                exercise.BodyWeight ??= defaultBodyWeight;
                exercise.AddedWeight ??= 0m;
                exercise.Weight = GetEffectiveLoad(exercise, defaultBodyWeight);
                break;

            case ExerciseWeightType.None:
                exercise.Weight = 0m;
                exercise.BodyWeight = null;
                exercise.AddedWeight = null;
                break;

            default:
                exercise.BodyWeight = null;
                exercise.AddedWeight = null;
                if (exercise.Weight < 0)
                {
                    exercise.Weight = 0m;
                }
                break;
        }
    }

    public static decimal GetEffectiveLoad(AddExerciseResource exercise, decimal? defaultBodyWeight = null)
    {
        ArgumentNullException.ThrowIfNull(exercise);

        return (exercise.WeightType ?? ExerciseWeightType.Standard) switch
        {
            ExerciseWeightType.BodyWeight => (exercise.BodyWeight ?? defaultBodyWeight ?? 0m) + (exercise.AddedWeight ?? 0m),
            ExerciseWeightType.None => 0m,
            _ => exercise.Weight
        };
    }
}
