using System.Text.Json;
using WinContainers.Runtime.Models;

namespace WinContainers.Runtime;

public static class WslcContainerParser
{
    public static List<ContainerCardData> ParseContainers(string rawOutput)
    {
        var entries = new List<ContainerCardData>();
        if (string.IsNullOrWhiteSpace(rawOutput))
            return entries;

        try
        {
            using var doc = JsonDocument.Parse(rawOutput);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        entries.Add(ParseContainer(item));
                }
                return entries;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                entries.Add(ParseContainer(doc.RootElement));
                return entries;
            }
        }
        catch { }

        // wslc container ps outputs newline-delimited JSON objects
        foreach (var line in rawOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    entries.Add(ParseContainer(doc.RootElement));
            }
            catch { }
        }

        return entries;
    }

    private static ContainerCardData ParseContainer(JsonElement el)
    {
        var ports = GetPorts(el);
        var status = GetField(el, "Status");
        if (string.IsNullOrWhiteSpace(status))
            status = GetStateStatus(el);
        if (string.IsNullOrWhiteSpace(status))
            status = "(unknown)";
        var id = GetField(el, "ID", "(unknown)");
        var image = GetField(el, "Image", "(unknown)");
        var name = GetField(el, "Names");

        if (string.IsNullOrWhiteSpace(name) || name == "(unknown)")
            name = GetField(el, "Name", id);

        var labels = ParseLabels(el);
        var mounts = GetMounts(el);

        return new ContainerCardData
        {
            Id = id,
            Name = string.IsNullOrWhiteSpace(name) ? id : name,
            Status = status,
            Image = image,
            Ports = string.IsNullOrWhiteSpace(ports) ? "No ports" : ports,
            CreatedAt = GetField(el, "CreatedAt", ""),
            PortLinks = ContainerCardData.ParsePortLinksStatic(ports),
            Labels = labels.Count > 0 ? labels : null,
            MountInfos = mounts
        };
    }

    public static List<ImageEntryData> ParseImages(string rawOutput)
    {
        var entries = new List<ImageEntryData>();
        if (string.IsNullOrWhiteSpace(rawOutput))
            return entries;

        try
        {
            using var doc = JsonDocument.Parse(rawOutput);
            if (doc.RootElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                        entries.Add(ParseImage(item));
                }
                return entries;
            }
            else if (doc.RootElement.ValueKind == JsonValueKind.Object)
            {
                entries.Add(ParseImage(doc.RootElement));
                return entries;
            }
        }
        catch { }

        // wslc image ls outputs newline-delimited JSON objects
        foreach (var line in rawOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            try
            {
                using var doc = JsonDocument.Parse(line);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                    entries.Add(ParseImage(doc.RootElement));
            }
            catch { }
        }

        return entries;
    }

    private static ImageEntryData ParseImage(JsonElement el)
    {
        var created = GetField(el, "CreatedAt");
        if (string.IsNullOrEmpty(created))
            created = GetField(el, "CreatedSince", "(unknown)");
        return new ImageEntryData
        {
            Repository = GetField(el, "Repository", "(none)"),
            Tag = GetField(el, "Tag", "(none)"),
            ID = GetField(el, "ID", "(unknown)"),
            CreatedAt = created,
            Size = GetField(el, "Size", "(unknown)")
        };
    }

    private static Dictionary<string, string> ParseLabels(JsonElement container)
    {
        var labels = new Dictionary<string, string>();
        if (container.TryGetProperty("Labels", out var labelsElement) &&
            labelsElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in labelsElement.EnumerateObject())
                labels[prop.Name] = prop.Value.GetString() ?? "";
        }
        return labels;
    }

    private static string GetStateStatus(JsonElement element)
    {
        JsonElement state = default;
        var found = false;
        foreach (var property in element.EnumerateObject())
        {
            if (!string.Equals(property.Name, "State", StringComparison.OrdinalIgnoreCase))
                continue;

            state = property.Value;
            found = true;
            break;
        }

        if (!found)
            return "";

        if (state.ValueKind == JsonValueKind.Number && state.TryGetInt32(out var numericState))
        {
            return numericState switch
            {
                2 => "Up",
                1 => "Created",
                _ => "Stopped"
            };
        }

        var value = state.ValueKind == JsonValueKind.String ? state.GetString() : state.ToString();
        return value switch
        {
            "2" => "Up",
            "1" => "Created",
            _ when !string.IsNullOrWhiteSpace(value) => value!,
            _ => ""
        };
    }

    private static List<MountInfo> GetMounts(JsonElement element)
    {
        var mounts = new List<MountInfo>();
        if (!element.TryGetProperty("Mounts", out var mountsElement) || mountsElement.ValueKind != JsonValueKind.Array)
            return mounts;

        foreach (var mount in mountsElement.EnumerateArray())
        {
            if (mount.ValueKind != JsonValueKind.Object)
                continue;

            var source = GetField(mount, "Source");
            if (string.IsNullOrWhiteSpace(source))
                source = GetField(mount, "SourcePath");

            var name = GetField(mount, "Name");
            var target = GetField(mount, "Destination");
            if (string.IsNullOrWhiteSpace(target))
                target = GetField(mount, "Target");

            var effectiveSource = !string.IsNullOrWhiteSpace(name) && GetField(mount, "Type").Equals("volume", StringComparison.OrdinalIgnoreCase)
                ? name
                : source;

            if (!string.IsNullOrWhiteSpace(effectiveSource) || !string.IsNullOrWhiteSpace(target))
                mounts.Add(new MountInfo(effectiveSource ?? "", target ?? ""));
        }

        return mounts;
    }

    public static List<MountInfo> ParseMountsFromInspect(string rawOutput)
    {
        var mounts = new List<MountInfo>();
        if (string.IsNullOrWhiteSpace(rawOutput))
            return mounts;

        var cleaned = rawOutput.Trim();
        if (cleaned == "[]") return mounts;

        try
        {
            using var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;
            if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
                root = root[0];

            if (root.ValueKind == JsonValueKind.Object)
                mounts.AddRange(GetMounts(root));
        }
        catch { }

        return mounts;
    }

    private static string GetPorts(JsonElement element)
    {
        if (!element.TryGetProperty("Ports", out var ports) || ports.ValueKind == JsonValueKind.Null)
            return "";

        if (ports.ValueKind == JsonValueKind.String)
            return ports.GetString() ?? "";

        if (ports.ValueKind != JsonValueKind.Array)
            return ports.ToString();

        var values = new List<string>();
        foreach (var port in ports.EnumerateArray())
        {
            if (port.ValueKind != JsonValueKind.Object)
                continue;

            var binding = GetField(port, "BindingAddress", "0.0.0.0");
            var hostPort = GetField(port, "HostPort");
            var containerPort = GetField(port, "ContainerPort");
            var protocol = GetField(port, "Protocol", "6") switch
            {
                "6" => "tcp",
                "17" => "udp",
                var text => text
            };

            if (!string.IsNullOrWhiteSpace(hostPort) && !string.IsNullOrWhiteSpace(containerPort))
                values.Add($"{binding}:{hostPort}->{containerPort}/{protocol}");
        }

        return string.Join(", ", values);
    }

    private static string GetField(JsonElement element, string propertyName, string fallback = "")
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            foreach (var candidate in element.EnumerateObject())
            {
                if (!string.Equals(candidate.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                    continue;

                property = candidate.Value;
                break;
            }
        }

        if (property.ValueKind == JsonValueKind.Undefined || property.ValueKind == JsonValueKind.Null)
            return fallback;

        var value = property.ValueKind == JsonValueKind.String
            ? property.GetString()
            : property.ToString();

        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}
