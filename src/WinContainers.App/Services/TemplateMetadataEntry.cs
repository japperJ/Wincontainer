using System.Text.Json.Serialization;

namespace WinContainers_App.Services;

public sealed class TemplateMetadataEntry
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("image")] public string Image { get; set; } = "";
    [JsonPropertyName("category")] public string Category { get; set; } = "";
    [JsonPropertyName("description")] public string Description { get; set; } = "";
    [JsonPropertyName("container_name")] public string ContainerName { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("access")] public AccessInfo Access { get; set; } = new();
    [JsonPropertyName("environment")] public List<EnvironmentVar> Environment { get; set; } = [];
    [JsonPropertyName("credentials")] public List<CredentialInfo> Credentials { get; set; } = [];
    [JsonPropertyName("volumes")] public List<VolumeInfo> Volumes { get; set; } = [];
    [JsonPropertyName("setup_notes")] public List<string> SetupNotes { get; set; } = [];
    [JsonPropertyName("documentation_urls")] public List<string> DocumentationUrls { get; set; } = [];
    [JsonPropertyName("verification")] public VerificationInfo Verification { get; set; } = new();
}

public sealed class AccessInfo
{
    [JsonPropertyName("urls")] public List<string> Urls { get; set; } = [];
    [JsonPropertyName("ports")] public List<PortInfo> Ports { get; set; } = [];
}

public sealed class PortInfo
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("host")] public int? Host { get; set; }
    [JsonPropertyName("container")] public int? Container { get; set; }
    [JsonPropertyName("protocol")] public string Protocol { get; set; } = "tcp";
    [JsonPropertyName("url")] public string? Url { get; set; }
}

public sealed class EnvironmentVar
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("value")] public object? RawValue { get; set; }
    public string Value => RawValue?.ToString() ?? "";
}

public sealed class CredentialInfo
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("value")] public string Value { get; set; } = "";
    [JsonPropertyName("provenance")] public string Provenance { get; set; } = "";
    [JsonPropertyName("insecure_demo_default")] public bool InsecureDemoDefault { get; set; }
}

public sealed class VolumeInfo
{
    [JsonPropertyName("service")] public string Service { get; set; } = "";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("target")] public string Target { get; set; } = "";
    [JsonPropertyName("read_only")] public bool ReadOnly { get; set; }
}

public sealed class VerificationInfo
{
    [JsonPropertyName("status")] public string Status { get; set; } = "unknown";
    [JsonPropertyName("source")] public string Source { get; set; } = "";
    [JsonPropertyName("runtime_test")] public string RuntimeTest { get; set; } = "";
}
