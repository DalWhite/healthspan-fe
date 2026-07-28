using System.Text.Json;

public static class HttpProblemDetailsReader
{
    public static async Task<string> ReadMessageAsync(
        HttpResponseMessage response,
        string fallback)
    {
        try
        {
            using var problem = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync());
            var validationError = ReadFirstValidationError(
                problem.RootElement);
            if (!string.IsNullOrWhiteSpace(validationError))
            {
                return validationError;
            }

            var title = ReadString(problem.RootElement, "title");
            var detail = ReadString(problem.RootElement, "detail");

            if (!string.IsNullOrWhiteSpace(title)
                && !string.IsNullOrWhiteSpace(detail))
            {
                return $"{title} {detail}";
            }

            if (!string.IsNullOrWhiteSpace(title))
            {
                return title;
            }

            if (!string.IsNullOrWhiteSpace(detail))
            {
                return detail;
            }
        }
        catch (JsonException)
        {
        }

        return fallback;
    }

    private static string? ReadString(JsonElement root, string propertyName)
        => root.TryGetProperty(propertyName, out var value)
            ? value.GetString()
            : null;

    private static string? ReadFirstValidationError(JsonElement root)
    {
        if (!root.TryGetProperty("errors", out var errors)
            || errors.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in errors.EnumerateObject())
        {
            if (property.Value.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var message in property.Value.EnumerateArray())
            {
                if (message.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(message.GetString()))
                {
                    return message.GetString();
                }
            }
        }

        return null;
    }
}
