using System.Text.Json;
using WinContainers.Runtime.Models;

namespace WinContainers.Runtime;

public static class WslcResourceParser
{
    public static IReadOnlyList<ResourceEntryData> ParseVolumes(string? output)
    {
        var jsonEntries = ParseJsonLines(output, element =>
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

        return jsonEntries.Count > 0
            ? jsonEntries
            : ParseLines(output, tokens => new ResourceEntryData
            {
                Name = tokens[^1],
                Details = string.Join(" ", tokens[..^1])
            }, line => line.Contains("DRIVER", StringComparison.OrdinalIgnoreCase));
    }

    public static IReadOnlyList<ResourceEntryData> ParseNetworks(string? output)
    {
        var jsonEntries = ParseJsonLines(output, element =>
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

        return jsonEntries.Count > 0
            ? jsonEntries
            : ParseLines(output, tokens => new ResourceEntryData
            {
                Name = tokens.Length >= 4 ? tokens[1] : tokens[^1],
                Details = string.Join(" ", tokens)
            }, line => line.Contains("NETWORK ID", StringComparison.OrdinalIgnoreCase));
    }

    private static IReadOnlyList<ResourceEntryData> ParseJsonLines(
        string? output,
        Func<JsonElement, ResourceEntryData?> map)
    {
        if (string.IsNullOrWhiteSpace(output))
            return [];

        var entries = new List<ResourceEntryData>();
        foreach (var line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var document = JsonDocument.Parse(line);
                if (document.RootElement.ValueKind == JsonValueKind.Object && map(document.RootElement) is { } entry)
                    entries.Add(entry);
            }
            catch (JsonException)
            {
                // This is table output; the fallback parser handles it below.
            }
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
