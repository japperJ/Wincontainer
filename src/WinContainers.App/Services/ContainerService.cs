using System.Text.Json;
using WinContainers.Core.Models;
using WinContainers.Runtime.Models;
using WinContainers.Runtime;

namespace WinContainers_App.Services;

public sealed class ContainerService
{
    public List<ContainerCardData> ParseContainerEntries(string rawOutput)
        => WslcContainerParser.ParseContainers(rawOutput ?? "");

    public List<FileEntryData> ParseFileEntries(string rawOutput)
    {
        var entries = new List<FileEntryData>();
        if (string.IsNullOrWhiteSpace(rawOutput))
            return entries;

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
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

        return entries.OrderBy(e => e.Type != "dir" ? 1 : 0)
                      .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
                      .ToList();
    }

    public string NormalizeStatusForComparison(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "";
        return status.Trim();
    }

    public static bool IsRunningStatus(string status)
        => status.StartsWith("Up", StringComparison.OrdinalIgnoreCase) || status.StartsWith("Running", StringComparison.OrdinalIgnoreCase);

    public static bool IsExitedStatus(string status)
        => status.StartsWith("Exited", StringComparison.OrdinalIgnoreCase) || status.StartsWith("Stopped", StringComparison.OrdinalIgnoreCase);

    public static bool IsPausedStatus(string status)
        => status.StartsWith("Paused", StringComparison.OrdinalIgnoreCase);

    public List<string> GetInUseImageNames(List<ContainerCardData> containers)
    {
        var names = new List<string>();
        foreach (var c in containers)
        {
            var image = c.Image ?? "";
            // Strip registry prefix (e.g. docker.io/library/nginx:latest -> library/nginx:latest)
            var idx = image.IndexOf('/');
            if (idx >= 0 && (image.IndexOf('.') >= 0 && image.IndexOf('.') < idx || image.IndexOf(':') >= 0 && image.IndexOf(':') < idx))
                image = image[(idx + 1)..];
            // Strip Docker Hub library/ prefix (nginx is stored as library/nginx:latest internally)
            if (image.StartsWith("library/", StringComparison.OrdinalIgnoreCase))
                image = image["library/".Length..];
            // Default tag when omitted (e.g. "nginx" -> "nginx:latest")
            if (!image.Contains(':'))
                image += ":latest";
            names.Add(image);
        }
        return names.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }
}
