using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace healthspan_fe.tests.Regression;

/// <summary>
/// Mock builders for the merged Today page endpoint (t_101e2b0a). Tests that
/// used to mock /Workouts/today/sessions + /Workouts?pageSize=14 +
/// /Workouts/{id} x2 + /HealthProfile now mock ONE today/page response; these
/// helpers wrap the old fixture bodies into the new page shape, with the same
/// fill/placeholder semantics the API applies server-side.
/// </summary>
internal static class TodayPageMocks
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Wraps one or more JSON array fixtures (sessions first, recent workouts
    /// second) into a single TodayWorkoutPage JSON. Non-array arguments are
    /// passed through untouched (e.g. malformed payloads a test injects).
    /// </summary>
    public static string Page(params string[] bodies)
    {
        var sessions = new JsonArray();
        var recent = new JsonArray();
        foreach (var body in bodies)
        {
            JsonNode? node;
            try
            {
                node = JsonNode.Parse(JoinArrays(body));
            }
            catch (JsonException)
            {
                return body;
            }

            if (node is JsonArray array)
            {
                var isRecent = array.Any(item =>
                    item is not null && (item.AsObject().ContainsKey("leadingMuscles")
                        || item.AsObject().ContainsKey("exerciseCount")));
                foreach (var item in array.Select(x => x!.DeepClone()))
                {
                    if (isRecent) recent.Add(item); else sessions.Add(item);
                }
            }
            else
            {
                return body;
            }
        }

        return BuildPage(sessions, recent);
    }

    private static string BuildPage(JsonArray sessions, JsonArray recent)
    {
        var filled = recent
            .Where(item => item is not null && item.AsObject()["exercises"] is JsonArray ex && ex.Count > 0)
            .ToList();

        var previousLoads = new JsonObject();
        foreach (var workout in filled.Take(2))
        {
            foreach (var exerciseNode in workout!.AsObject()["exercises"]!.AsArray())
            {
                if (exerciseNode is null) continue;
                var exercise = exerciseNode.AsObject();
                var typeId = exercise["typeId"]?.GetValue<int>() ?? 0;
                if (previousLoads.ContainsKey(typeId.ToString())) continue;
                if (Int(exercise, "series") > 0 || Int(exercise, "repetitions") > 0 || Num(exercise, "weight") > 0)
                {
                    previousLoads[typeId.ToString()] = new JsonObject
                    {
                        ["id"] = exercise["id"]?.DeepClone(),
                        ["typeId"] = typeId,
                        ["name"] = exercise["name"]?.DeepClone() ?? "Exercise",
                        ["series"] = exercise["series"]?.DeepClone(),
                        ["repetitions"] = exercise["repetitions"]?.DeepClone(),
                        ["weight"] = exercise["weight"]?.DeepClone(),
                        ["bodyWeight"] = exercise["bodyWeight"]?.DeepClone(),
                        ["addedWeight"] = exercise["addedWeight"]?.DeepClone(),
                        ["attempts"] = exercise["attempts"]?.DeepClone()
                            ?? Math.Max(exercise["series"]?.GetValue<int>() ?? 0, 1),
                    };
                }
            }
        }

        var page = new JsonObject
        {
            ["sessions"] = sessions,
            ["recentWorkouts"] = recent,
            ["previousLoads"] = previousLoads,
            ["profileWeightKg"] = null,
        };
        return page.ToJsonString(Options);
    }

    /// <summary>
    /// Merges consecutive top-level JSON array literals ("[...][...]") into one
    /// array. Dynamic fixtures compose session+recent bodies by concatenation.
    /// </summary>
    public static string JoinArrays(string body)
    {
        var sb = new System.Text.StringBuilder(body.Length + 8);
        var depth = 0;
        var inString = false;
        var escape = false;
        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];
            if (inString)
            {
                sb.Append(c);
                if (escape) escape = false;
                else if (c == '\\') escape = true;
                else if (c == '"') inString = false;
                continue;
            }

            switch (c)
            {
                case '"':
                    inString = true;
                    sb.Append(c);
                    break;
                case '[':
                    if (depth == 0 && sb.Length > 0)
                    {
                        // Trailing data after a completed array: merge into it.
                        sb.Length--;
                        sb.Append(',');
                    }

                    depth++;
                    sb.Append(c);
                    break;
                case ']':
                    depth--;
                    sb.Append(c);
                    break;
                default:
                    sb.Append(c);
                    break;
            }
        }

        return sb.ToString();
    }

    private static int Int(JsonObject obj, string key)
        => obj[key] is JsonValue value && value.TryGetValue<int>(out var parsed) ? parsed : 0;

    private static double Num(JsonObject obj, string key)
        => obj[key] is JsonValue value && value.TryGetValue<double>(out var parsed) ? parsed : 0;
}
