using System.Text;
using System.Text.Json;

namespace WinContainers.Core.Models;

public static class ImageListFormatter
{
    public static string Format(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput) || string.Equals(rawOutput.Trim(), "[]", StringComparison.Ordinal))
        {
            return "No images found.";
        }

        var entries = new List<ImageListEntry>();
        var lines = rawOutput.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (string.Equals(line, "[]", StringComparison.Ordinal))
            {
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(line);

                if (document.RootElement.ValueKind == JsonValueKind.Object)
                {
                    entries.Add(CreateEntry(document.RootElement));
                    continue;
                }

                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in document.RootElement.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.Object)
                        {
                            entries.Add(CreateEntry(item));
                        }
                    }
                }
            }
            catch
            {
                return rawOutput;
            }
        }

        if (entries.Count == 0)
        {
            return "No images found.";
        }

        var builder = new StringBuilder();
        builder.AppendLine($"Images: {entries.Count}");

        foreach (var entry in entries)
        {
            builder.AppendLine();
            builder.AppendLine($"• {entry.Name}");

            if (!string.IsNullOrWhiteSpace(entry.Id))
            {
                builder.AppendLine($"  ID: {entry.Id}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Repository) && !string.Equals(entry.Repository, entry.Name, StringComparison.Ordinal))
            {
                builder.AppendLine($"  Repository: {entry.Repository}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Tag))
            {
                builder.AppendLine($"  Tag: {entry.Tag}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Size))
            {
                builder.AppendLine($"  Size: {entry.Size}");
            }

            if (!string.IsNullOrWhiteSpace(entry.Platform))
            {
                builder.AppendLine($"  Platform: {entry.Platform}");
            }

            if (!string.IsNullOrWhiteSpace(entry.CreatedAt))
            {
                builder.AppendLine($"  Created: {entry.CreatedAt}");
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static ImageListEntry CreateEntry(JsonElement image)
    {
        return new ImageListEntry
        {
            Id = TryGetString(image, "ID"),
            Name = TryGetString(image, "Name", "(unnamed image)"),
            Repository = TryGetString(image, "Repository"),
            Tag = TryGetString(image, "Tag"),
            Size = TryGetString(image, "Size"),
            Platform = TryGetString(image, "Platform"),
            CreatedAt = TryGetString(image, "CreatedAt")
        };
    }

    private static string TryGetString(JsonElement element, string propertyName, string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return fallback;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? fallback;
        }

        return property.ToString();
    }
}

public sealed class ImageListEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Repository { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
