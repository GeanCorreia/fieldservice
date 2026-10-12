using System.Text.Json;

namespace FieldService.Form.Extensions;

internal static class FormSubmissionJsonPathExtensions
{
    public static string[] ToPathSegments(this string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var segments = new List<string>();

        foreach (var rawSegment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var segment = rawSegment.Trim();
            if (segment.Length == 0)
                continue;

            var bracketIndex = segment.IndexOf('[');
            if (bracketIndex < 0)
            {
                segments.Add(segment);
                continue;
            }

            var propertyName = segment[..bracketIndex];
            if (!string.IsNullOrWhiteSpace(propertyName))
                segments.Add(propertyName);

            var cursor = bracketIndex;
            while (cursor >= 0 && cursor < segment.Length)
            {
                var openIndex = segment.IndexOf('[', cursor);
                if (openIndex < 0)
                    break;

                var closeIndex = segment.IndexOf(']', openIndex + 1);
                if (closeIndex <= openIndex + 1)
                    throw new ArgumentException($"Invalid JSON path segment '{segment}'.", nameof(path));

                var indexText = segment[(openIndex + 1)..closeIndex].Trim();
                if (!int.TryParse(indexText, out var index) || index < 0)
                    throw new ArgumentException($"Invalid JSON array index in segment '{segment}'.", nameof(path));

                segments.Add(index.ToString());
                cursor = closeIndex + 1;
            }
        }

        if (segments.Count == 0)
            throw new ArgumentException("Path must contain at least one valid segment.", nameof(path));

        return segments.ToArray();
    }

    public static string ToPostgresTextArrayExpression(this IEnumerable<string> segments)
    {
        ArgumentNullException.ThrowIfNull(segments);

        var quotedSegments = segments
            .Select(segment => $"'{segment.Replace("'", "''")}'")
            .ToArray();

        if (quotedSegments.Length == 0)
            throw new ArgumentException("Path must contain at least one segment.", nameof(segments));

        return $"ARRAY[{string.Join(",", quotedSegments)}]";
    }

    public static bool TryGetValueByPath(this JsonElement root, string path, out JsonElement value)
    {
        value = root;

        foreach (var rawSegment in path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!TryReadSegment(value, rawSegment, out value))
                return false;
        }

        return true;
    }

    public static object? ToObjectValue(this JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue,
            JsonValueKind.Number when element.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonValueKind.Object => element.EnumerateObject()
                .ToDictionary(property => property.Name, property => property.Value.ToObjectValue()),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(item => item.ToObjectValue())
                .ToList(),
            _ => element.ToString()
        };
    }

    private static bool TryReadSegment(JsonElement source, string segment, out JsonElement value)
    {
        value = source;

        var bracketIndex = segment.IndexOf('[');
        if (bracketIndex < 0)
        {
            return source.ValueKind == JsonValueKind.Object
                   && source.TryGetProperty(segment, out value);
        }

        var propertyName = segment[..bracketIndex];
        if (source.ValueKind != JsonValueKind.Object || !source.TryGetProperty(propertyName, out var arrayNode))
            return false;

        if (arrayNode.ValueKind != JsonValueKind.Array)
            return false;

        var closeBracketIndex = segment.IndexOf(']', bracketIndex + 1);
        if (closeBracketIndex <= bracketIndex + 1)
            return false;

        var indexText = segment[(bracketIndex + 1)..closeBracketIndex];
        if (!int.TryParse(indexText, out var index) || index < 0)
            return false;

        var i = 0;
        foreach (var item in arrayNode.EnumerateArray())
        {
            if (i == index)
            {
                value = item;
                return true;
            }

            i++;
        }

        return false;
    }
}

