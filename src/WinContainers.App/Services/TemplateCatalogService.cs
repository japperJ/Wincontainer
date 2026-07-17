using System.Text;
using Windows.Storage;
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
    private static readonly TimeSpan CacheStaleness = TimeSpan.FromHours(24);

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
            var file = await ApplicationData.Current.LocalFolder.TryGetItemAsync(CacheFileName);
            if (file is null) return null;

            var lastModified = (file as StorageFile)?.DateCreated ?? DateTimeOffset.MinValue;
            if (DateTimeOffset.UtcNow - lastModified > CacheStaleness)
                return null;

            var yaml = await FileIO.ReadTextAsync((StorageFile)file);
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
            var file = await ApplicationData.Current.LocalFolder.CreateFileAsync(
                CacheFileName, CreationCollisionOption.ReplaceExisting);
            await FileIO.WriteTextAsync(file, yaml);
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
}
