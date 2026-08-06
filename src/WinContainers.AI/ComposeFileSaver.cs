using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace WinContainers.AI;

/// <summary>
/// Validates and saves compose (docker-compose) YAML files to a fixed
/// directory so the agent never writes outside a safe location.
/// </summary>
public sealed class ComposeFileSaver
{
    private readonly string _directory;

    public ComposeFileSaver(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        _directory = directory;
    }

    /// <summary>
    /// Validates <paramref name="yaml"/> and saves it as a .yaml file under the
    /// configured directory. Returns the full path to the saved file.
    /// </summary>
    public string Save(string filename, string yaml)
    {
        if (string.IsNullOrWhiteSpace(yaml))
            throw new InvalidOperationException("The compose file is empty.");

        ValidateYaml(yaml);

        var baseName = Path.GetFileNameWithoutExtension(filename);
        var safeName = string.Concat(baseName.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '_'));
        if (safeName.Length == 0)
            safeName = "compose";

        Directory.CreateDirectory(_directory);
        var path = Path.Combine(_directory, $"{safeName}.yaml");
        File.WriteAllText(path, yaml);
        return path;
    }

    private static void ValidateYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            _ = deserializer.Deserialize<object?>(yaml);
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"The compose YAML is not valid: {ex.Message}");
        }
    }
}
