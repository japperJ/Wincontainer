using System.Text;
using System.Text.Json;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using SvcLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.Services;

public sealed class TemplateCatalogEntry
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "";
    public string Image { get; set; } = "";
    public string Compose { get; set; } = "";
    public string ContainerName { get; set; } = "";
    public string Website { get; set; } = "";
}

public sealed class TemplateCatalogService
{
    private const string RemoteUrl = "https://raw.githubusercontent.com/japperj/wincontainer-templates/main/templates.yaml";
    private const string CacheFileName = "templates.yaml";
    private const string RemoteMetadataUrl = "https://raw.githubusercontent.com/japperj/wincontainer-templates/main/templates.metadata.yaml";
    private const string MetadataCacheFileName = "templates.metadata.yaml";
    private static readonly TimeSpan CacheStaleness = TimeSpan.FromHours(24);
    private static readonly string CacheDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinContainers");

    private static readonly List<TemplateCatalogEntry> SeedTemplates =
    [
        new() { Name="Nginx", Description="Static web server", Category="Web", Image="nginx:latest",
            Compose="services:\n  web:\n    image: nginx:latest\n    ports:\n      - \"8080:80\"",
            ContainerName="nginx-web", Website="https://nginx.org" },
        new() { Name="Redis", Description="Cache and message broker", Category="Databases", Image="redis:latest",
            Compose="services:\n  redis:\n    image: redis:latest\n    ports:\n      - \"6379:6379\"",
            ContainerName="redis", Website="https://redis.io" },
        new() { Name="Postgres", Description="Relational database", Category="Databases", Image="postgres:16",
            Compose="services:\n  db:\n    image: postgres:16\n    environment:\n      POSTGRES_PASSWORD: postgres\n    ports:\n      - \"5432:5432\"",
            ContainerName="postgres", Website="https://postgresql.org" },
        new() { Name="Portainer", Description="Container management UI", Category="Management", Image="portainer/portainer-ce:latest",
            Compose="services:\n  portainer:\n    image: portainer/portainer-ce:latest\n    ports:\n      - \"9000:9000\"\n    volumes:\n      - /var/run/docker.sock:/var/run/docker.sock",
            ContainerName="portainer", Website="https://portainer.io" },
        new() { Name="Home Assistant", Description="Home automation platform", Category="Home", Image="ghcr.io/home-assistant/home-assistant:stable",
            Compose="services:\n  homeassistant:\n    image: ghcr.io/home-assistant/home-assistant:stable\n    ports:\n      - \"8123:8123\"\n    volumes:\n      - ./config:/config",
            ContainerName="homeassistant", Website="https://home-assistant.io" },
        new() { Name="n8n", Description="Workflow automation", Category="Automation", Image="n8n/n8n:latest",
            Compose="services:\n  n8n:\n    image: n8n/n8n:latest\n    ports:\n      - \"5678:5678\"\n    environment:\n      N8N_HOST: localhost\n      N8N_PORT: 5678\n    volumes:\n      - n8n_data:/home/node/.n8n",
            ContainerName="n8n", Website="https://n8n.io" },
    ];

    private readonly IOutputService _output;
    private List<TemplateCatalogEntry>? _cached;
    private Dictionary<string, TemplateMetadataEntry>? _metadataCached;

    public TemplateCatalogService(IOutputService output)
    {
        _output = output;
    }

    public async Task<List<TemplateCatalogEntry>> GetTemplatesAsync()
    {
        if (_cached is not null)
            return _cached;

        var loaded = await TryLoadFromCacheAsync();
        if (loaded is not null)
        {
            _cached = loaded;
            _ = RefreshInBackgroundAsync();
            return _cached;
        }

        loaded = await TryFetchFromRemoteAsync();
        if (loaded is not null)
        {
            _cached = loaded;
            _ = SaveToCacheAsync(loaded);
            return _cached;
        }

        _output.Write("Using built-in seed templates (no network or cache)", SvcLogLevel.Warning);
        _cached = SeedTemplates;
        return _cached;
    }

    public async Task<List<TemplateCatalogEntry>> RefreshAsync()
    {
        _output.Write("Refreshing template catalog...");
        var loaded = await TryFetchFromRemoteAsync();
        if (loaded is not null)
        {
            _cached = loaded;
            _ = SaveToCacheAsync(loaded);
            _output.Write($"Template catalog refreshed ({loaded.Count} templates)");
        }
        else
        {
            _output.Write("Refresh failed — keeping current catalog", SvcLogLevel.Warning);
        }
        return _cached ?? SeedTemplates;
    }

    private async Task RefreshInBackgroundAsync()
    {
        try
        {
            await Task.Delay(3000);
            var remote = await TryFetchFromRemoteAsync();
            if (remote is not null)
            {
                _cached = remote;
                await SaveToCacheAsync(remote);
                _output.Write($"Template catalog refreshed ({remote.Count} templates)");
            }
        }
        catch (Exception ex)
        {
            _output.Write($"Background template refresh failed: {ex.Message}", SvcLogLevel.Warning);
        }
    }

    private async Task<List<TemplateCatalogEntry>?> TryFetchFromRemoteAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var yaml = await client.GetStringAsync(RemoteUrl);
            return ParseYaml(yaml);
        }
        catch (Exception ex)
        {
            _output.Write($"Template fetch failed: {ex.Message}", SvcLogLevel.Warning);
            return null;
        }
    }

    private async Task<List<TemplateCatalogEntry>?> TryLoadFromCacheAsync()
    {
        try
        {
            var path = Path.Combine(CacheDirectory, CacheFileName);
            if (!File.Exists(path)) return null;

            var lastModified = File.GetLastWriteTimeUtc(path);
            if (DateTime.UtcNow - lastModified > CacheStaleness)
                return null;

            var yaml = await File.ReadAllTextAsync(path);
            return ParseYaml(yaml);
        }
        catch (Exception ex)
        {
            _output.Write($"Template cache read failed: {ex.Message}", SvcLogLevel.Warning);
            return null;
        }
    }

    private async Task SaveToCacheAsync(List<TemplateCatalogEntry> templates)
    {
        try
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();
            var yaml = serializer.Serialize(templates);
            Directory.CreateDirectory(CacheDirectory);
            await File.WriteAllTextAsync(Path.Combine(CacheDirectory, CacheFileName), yaml);
        }
        catch (Exception ex)
        {
            _output.Write($"Template cache write failed: {ex.Message}", SvcLogLevel.Warning);
        }
    }

    private static List<TemplateCatalogEntry>? ParseYaml(string yaml)
    {
        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return deserializer.Deserialize<List<TemplateCatalogEntry>>(yaml);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Template YAML parse failed: {ex.Message}");
            return null;
        }
    }

    public async Task<Dictionary<string, TemplateMetadataEntry>> GetMetadataAsync()
    {
        if (_metadataCached is not null)
            return _metadataCached;

        var loaded = await TryLoadMetadataFromCacheAsync();
        if (loaded is not null)
        {
            _metadataCached = loaded;
            _ = RefreshMetadataInBackgroundAsync();
            return _metadataCached;
        }

        loaded = await TryFetchMetadataFromRemoteAsync();
        if (loaded is not null)
        {
            _metadataCached = loaded;
            _ = SaveMetadataToCacheAsync(loaded);
            return _metadataCached;
        }

        _output.Write("Template metadata unavailable — enrichments won't be shown", SvcLogLevel.Warning);
        _metadataCached = [];
        return _metadataCached;
    }

    public async Task<Dictionary<string, TemplateMetadataEntry>> RefreshMetadataAsync()
    {
        _output.Write("Refreshing template metadata...");
        var loaded = await TryFetchMetadataFromRemoteAsync();
        if (loaded is not null)
        {
            _metadataCached = loaded;
            _ = SaveMetadataToCacheAsync(loaded);
            _output.Write($"Template metadata refreshed ({loaded.Count} entries)");
        }
        else
        {
            _output.Write("Metadata refresh failed — keeping current metadata", SvcLogLevel.Warning);
        }
        return _metadataCached ?? [];
    }

    private async Task RefreshMetadataInBackgroundAsync()
    {
        try
        {
            await Task.Delay(3000);
            var remote = await TryFetchMetadataFromRemoteAsync();
            if (remote is not null)
            {
                _metadataCached = remote;
                await SaveMetadataToCacheAsync(remote);
                _output.Write($"Template metadata refreshed ({remote.Count} entries)");
            }
        }
        catch (Exception ex)
        {
            _output.Write($"Background metadata refresh failed: {ex.Message}", SvcLogLevel.Warning);
        }
    }

    private async Task<Dictionary<string, TemplateMetadataEntry>?> TryFetchMetadataFromRemoteAsync()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var json = await client.GetStringAsync(RemoteMetadataUrl);
            return ParseMetadataJson(json);
        }
        catch (Exception ex)
        {
            _output.Write($"Metadata fetch failed: {ex.Message}", SvcLogLevel.Warning);
            return null;
        }
    }

    private async Task<Dictionary<string, TemplateMetadataEntry>?> TryLoadMetadataFromCacheAsync()
    {
        try
        {
            var path = Path.Combine(CacheDirectory, MetadataCacheFileName);
            if (!File.Exists(path)) return null;

            var lastModified = File.GetLastWriteTimeUtc(path);
            if (DateTime.UtcNow - lastModified > CacheStaleness)
                return null;

            var json = await File.ReadAllTextAsync(path);
            return ParseMetadataJson(json);
        }
        catch (Exception ex)
        {
            _output.Write($"Metadata cache read failed: {ex.Message}", SvcLogLevel.Warning);
            return null;
        }
    }

    private async Task SaveMetadataToCacheAsync(Dictionary<string, TemplateMetadataEntry> metadata)
    {
        try
        {
            var json = JsonSerializer.Serialize(metadata.Values.ToList(), new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(CacheDirectory);
            await File.WriteAllTextAsync(Path.Combine(CacheDirectory, MetadataCacheFileName), json);
        }
        catch (Exception ex)
        {
            _output.Write($"Metadata cache write failed: {ex.Message}", SvcLogLevel.Warning);
        }
    }

    private static Dictionary<string, TemplateMetadataEntry>? ParseMetadataJson(string json)
    {
        try
        {
            var entries = JsonSerializer.Deserialize<List<TemplateMetadataEntry>>(json);
            if (entries is null) return null;
            return entries.ToDictionary(e => e.Name, e => e);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Metadata JSON parse failed: {ex.Message}");
            return null;
        }
    }
}
