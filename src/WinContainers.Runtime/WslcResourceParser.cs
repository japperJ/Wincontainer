using System.Text.Json;
using WinContainers.Runtime.Models;

namespace WinContainers.Runtime;

public static class WslcResourceParser
{
    public static IReadOnlyList<ResourceEntryData> ParseVolumes(string? output)
    {
        if (OutputLooksLikeJson(output))
        {
            return ParseJsonLines(output, element =>
            {
                var name = GetString(element, "Name");
                return string.IsNullOrWhiteSpace(name) ? null : new ResourceEntryData
                {
                    Name = name,
                    Details = string.Join(" · ", new[]
                    {
                        GetString(element, "Driver"),
                        GetString(element, "Scope"),
                        GetString(element, "Mountpoint")
                    }.Where(value => !string.IsNullOrWhiteSpace(value)))
                };
            });
        }

        return ParseLines(output, tokens => new ResourceEntryData
        {
            Name = tokens[^1],
            Details = string.Join(" ", tokens[..^1])
        }, line => line.Contains("DRIVER", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<ResourceEntryData> ParseNetworks(string? output)
    {
        if (OutputLooksLikeJson(output))
        {
            return ParseJsonLines(output, element =>
            {
                var name = GetString(element, "Name");
                return string.IsNullOrWhiteSpace(name) ? null : new ResourceEntryData
                {
                    Name = name,
                    CanDelete = !IsBuiltInNetwork(name),
                    Details = string.Join(" · ", new[]
                    {
                        GetString(element, "ID"),
                        GetString(element, "Driver"),
                        GetString(element, "Scope")
                    }.Where(value => !string.IsNullOrWhiteSpace(value)))
                };
            });
        }

        return ParseLines(output, tokens => new ResourceEntryData
        {
            Name = tokens.Length >= 4 ? tokens[1] : tokens[^1],
            Details = string.Join(" ", tokens)
        }, line => line.Contains("NETWORK ID", StringComparison.OrdinalIgnoreCase));
    }

    private static bool OutputLooksLikeJson(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return false;

        if (TryParseJson(output, out _))
            return true;

        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParseJson(line, out _))
                return true;
        }

        return false;
    }

    private static IReadOnlyList<ResourceEntryData> ParseJsonLines(
        string? output,
        Func<JsonElement, ResourceEntryData?> map)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        // Parse the whole output as a single JSON document first. wslc emits a
        // pretty-printed, multi-line JSON array (or object) with --format json.
        // A line-by-line parse would split each object across lines and lose it.
        // When the output is valid JSON we trust it and never fall through to
        // the text tokenizer, so raw brackets/commas are never shown to the user.
        if (TryParseJson(output, out var whole))
        {
            using (whole)
            {
                return MapRoot(whole.RootElement, map);
            }
        }

        // JSON Lines fallback: one value per line.
        var lineEntries = new List<ResourceEntryData>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (TryParseJson(line, out var lineDoc))
            {
                using (lineDoc)
                {
                    lineEntries.AddRange(MapRoot(lineDoc.RootElement, map));
                }
            }
        }

        return lineEntries;
    }

    private static bool TryParseJson(string text, out JsonDocument document)
    {
        try
        {
            document = JsonDocument.Parse(text);
            return true;
        }
        catch (JsonException)
        {
            document = null!;
            return false;
        }
    }

    private static List<ResourceEntryData> MapRoot(JsonElement root, Func<JsonElement, ResourceEntryData?> map)
    {
        var entries = new List<ResourceEntryData>();
        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.Object && map(item) is { } entry)
                    entries.Add(entry);
            }
        }
        else if (root.ValueKind == JsonValueKind.Object && map(root) is { } entry)
        {
            entries.Add(entry);
        }

        return entries;
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
                return property.Value.GetString();
        }

        return null;
    }

    private static bool IsBuiltInNetwork(string name) =>
        name.Equals("bridge", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("host", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("none", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<ResourceEntryData> ParseLines(
        string? output,
        Func<string[], ResourceEntryData> map,
        Func<string, bool> isHeader)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var entries = new List<ResourceEntryData>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (line.StartsWith("error", StringComparison.OrdinalIgnoreCase) || isHeader(line))
                continue;

            var tokens = line.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                continue;

            entries.Add(map(tokens));
        }

        return entries;
    }
}
