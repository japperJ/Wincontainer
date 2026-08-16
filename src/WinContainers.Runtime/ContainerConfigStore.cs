using System.Text.Json;

namespace WinContainers.Runtime;

/// <summary>
/// Stores container creation configuration locally so it can be retrieved
/// during image update recreation, since WSLC's <c>container inspect</c>
/// does not expose mount or environment information.
/// </summary>
public static class ContainerConfigStore
{
    private static readonly string StorageDir;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false
    };

    static ContainerConfigStore()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        StorageDir = Path.Combine(baseDir, "WinContainers", "container-configs");
        Directory.CreateDirectory(StorageDir);
    }

    private static string GetFilePath(string containerName)
    {
        // Sanitize the name so it's safe as a file name
        var safe = string.Join("_", containerName.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(StorageDir, $"{safe}.json");
    }

    public static void SaveConfig(string containerName, ContainerRunConfig config)
    {
        var path = GetFilePath(containerName);
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }

    public static ContainerRunConfig? LoadConfig(string containerName)
    {
        var path = GetFilePath(containerName);
        if (!File.Exists(path))
            return null;

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ContainerRunConfig>(json, JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static void DeleteConfig(string containerName)
    {
        var path = GetFilePath(containerName);
        if (File.Exists(path))
            File.Delete(path);
    }
}

public sealed record ContainerRunConfig
{
    public string Image { get; init; } = string.Empty;
    public List<string> Ports { get; init; } = [];
    public List<string> Volumes { get; init; } = [];
    public List<string> Env { get; init; } = [];
    public string? Network { get; init; }
    public bool AllowLocalNetworkAccess { get; init; }
}
