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
}
