using System.Diagnostics;
using System.Text.Json;
using WinContainers.Runtime.Models;

namespace WinContainers.Runtime;

public static class WslcFileParser
{
    public static List<FileEntryData> Parse(string rawOutput)
    {
        var entries = new List<FileEntryData>();
        if (string.IsNullOrEmpty(rawOutput))
            return entries;

        foreach (var record in rawOutput.Split('\0', StringSplitOptions.RemoveEmptyEntries))
        {
            var separator = record.IndexOf('\t');
            if (separator <= 0 || separator == record.Length - 1)
                continue;

            var type = record[0];
            if (type is not ('d' or 'f'))
                continue;

            var name = record[(separator + 1)..];
            if (name is "." or ".." || name.Length == 0)
                continue;

            entries.Add(new FileEntryData
            {
                Name = name,
                Type = type == 'd' ? "dir" : "file"
            });
        }

        return entries.OrderBy(entry => entry.Type != "dir" ? 1 : 0)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static List<FileEntryData> ParseFileEntries(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return [];

        if (rawOutput.Contains('\0'))
            return Parse(rawOutput);

        var entries = new List<FileEntryData>();

        var cleaned = rawOutput.Trim();
        if (cleaned.Length == 0 || cleaned == "[]") return entries;

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var type = item.TryGetProperty("type", out var t) ? t.GetString() ?? "file" : "file";
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    entries.Add(new FileEntryData
                    {
                        Name = name,
                        Type = type
                    });
                }
            }
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"[WslcFileParser] ParseFileEntries JSON parse failed: {ex.Message}");
        }

        return entries.OrderBy(e => e.Type != "dir" ? 1 : 0)
                      .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                      .ToList();
    }
}
