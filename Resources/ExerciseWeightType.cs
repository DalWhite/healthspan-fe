[System.Text.Json.Serialization.JsonConverter(typeof(System.Text.Json.Serialization.JsonStringEnumConverter<ExerciseWeightType>))]
public enum ExerciseWeightType
{
    Standard = 0, // uses only Weight
    BodyWeight = 1, // uses BodyWeight + AddedWeight
    None = 2 // e.g., stretching, no weight
}
