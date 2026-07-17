using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers_App.Services;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public sealed record ImageResult(string Name, string Description, string StarCount, string PullCount, bool IsOfficial)
{
    public string DisplayName => string.IsNullOrWhiteSpace(Description)
        ? $"{Name}  (★{StarCount})"
        : $"{Name}  (★{StarCount})  {Description}";

    public string FullDisplayName => $"{Name}  (★{StarCount}){(IsOfficial ? "  ✓ Official" : "")}{(PullCount != "0" ? $"  ↓{PullCount}M" : "")}";
}

public sealed record TemplateCatalogItem(string Name, string Description, string Category, string Image, string Compose, string ContainerName, string Website)
{
    public string Summary => $"{Name} • {Category}";
    public string FullSummary => $"{Name}  ({Description})";
}

public partial class PortEntry : ObservableObject
{
    private string _host = "";
    public string Host
    {
        get => _host;
        set => SetProperty(ref _host, value);
    }

    private string _container = "";
    public string Container
    {
        get => _container;
        set => SetProperty(ref _container, value);
    }
}

public partial class VolumeEntry : ObservableObject
{
    private string _source = "";
    public string Source
    {
        get => _source;
        set => SetProperty(ref _source, value);
    }

    private string _target = "";
    public string Target
    {
        get => _target;
        set => SetProperty(ref _target, value);
    }
}

public partial class EnvVarEntry : ObservableObject
{
    private string _name = "";
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    private string _value = "";
    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}

public sealed class ParsedServiceConfig
{
    public string ServiceName { get; set; } = "";
    public string Image { get; set; } = "";
    public string ContainerName { get; set; } = "";
    public List<(string Host, string Container)> Ports { get; set; } = [];
    public List<(string Source, string Target)> Volumes { get; set; } = [];
    public List<(string Name, string Value)> EnvVars { get; set; } = [];
    public string RestartPolicy { get; set; } = "no";
    public string Summary => $"{ContainerName}  ({Image}){(Ports.Count > 0 ? $"  {Ports.Count} port(s)" : "")}{(Volumes.Count > 0 ? $"  {Volumes.Count} volume(s)" : "")}{(EnvVars.Count > 0 ? $"  {EnvVars.Count} env(s)" : "")}";
}

public partial class QuickActionsViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private static readonly HttpClient DockerHubClient = new();
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly TemplateCatalogService _catalogService;
    private CancellationTokenSource? _searchCts;
    private List<TemplateCatalogItem> _allTemplates = [];

    public string[] RestartPolicies { get; } = ["no", "on-failure", "always", "unless-stopped"];

    public QuickActionsViewModel(IOutputService output, TemplateCatalogService catalogService)
    {
        _output = output;
        _catalogService = catalogService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        if (_dispatcherQueue == null)
        {
            _output.Write("WARNING: DispatcherQueue not available on current thread", ServiceLogLevel.Warning);
        }
        Ports = [];
        Volumes = [];
        EnvVars = [];
        TemplateCatalog = new ObservableCollection<TemplateCatalogItem>();
        Categories = new ObservableCollection<string>();
        _ = LoadTemplatesAsync();
    }

    public async Task LoadTemplatesAsync()
    {
        try
        {
            var entries = await _catalogService.GetTemplatesAsync();
            _allTemplates = [.. entries.Select(e => new TemplateCatalogItem(
                e.Name, e.Description, e.Category, e.Image, e.Compose, e.ContainerName, e.Website))];

            var cats = _allTemplates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
            Categories.Clear();
            Categories.Add("All");
            foreach (var c in cats)
                Categories.Add(c);

            if (SelectedCategory is null || !Categories.Contains(SelectedCategory))
                SelectedCategory = "All";
            else
                ApplyCategoryFilter();
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to load templates: {ex.Message}", ServiceLogLevel.Warning);
            _allTemplates = CreateDefaultTemplates();
            ApplyCategoryFilter();
        }
    }

    public async Task RefreshCatalogAsync()
    {
        IsRefreshingCatalog = true;
        try
        {
            var entries = await _catalogService.RefreshAsync();
            _allTemplates = [.. entries.Select(e => new TemplateCatalogItem(
                e.Name, e.Description, e.Category, e.Image, e.Compose, e.ContainerName, e.Website))];

            var cats = _allTemplates.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
            Categories.Clear();
            Categories.Add("All");
            foreach (var c in cats)
                Categories.Add(c);

            if (SelectedCategory is null || !Categories.Contains(SelectedCategory))
                SelectedCategory = "All";
            else
                ApplyCategoryFilter();
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to refresh templates: {ex.Message}", ServiceLogLevel.Warning);
        }
        finally
        {
            IsRefreshingCatalog = false;
        }
    }

    private void ApplyCategoryFilter()
    {
        TemplateCatalog.Clear();
        var filtered = _allTemplates.AsEnumerable();

        if (SelectedCategory != "All" && !string.IsNullOrWhiteSpace(SelectedCategory))
            filtered = filtered.Where(t => t.Category == SelectedCategory);

        var search = TemplateCatalogSearchText?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLowerInvariant();
            filtered = filtered.Where(t =>
                (t.Name?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Description?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Category?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Image?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (t.Website?.Contains(lower, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        foreach (var t in filtered)
            TemplateCatalog.Add(t);
    }

    private static List<TemplateCatalogItem> CreateDefaultTemplates() =>
    [
        new("Nginx", "Static web server", "Web", "nginx:latest", "services:\n  web:\n    image: nginx:latest\n    ports:\n      - \"8080:80\"", "nginx-web", "https://nginx.org"),
        new("Redis", "Cache and message broker", "Databases", "redis:latest", "services:\n  redis:\n    image: redis:latest\n    ports:\n      - \"6379:6379\"", "redis", "https://redis.io"),
        new("Postgres", "Relational database", "Databases", "postgres:16", "services:\n  db:\n    image: postgres:16\n    environment:\n      POSTGRES_PASSWORD: postgres\n    ports:\n      - \"5432:5432\"", "postgres", "https://postgresql.org"),
        new("Portainer", "Container management UI", "Management", "portainer/portainer-ce:latest", "services:\n  portainer:\n    image: portainer/portainer-ce:latest\n    ports:\n      - \"9000:9000\"\n    volumes:\n      - /var/run/docker.sock:/var/run/docker.sock", "portainer", "https://portainer.io"),
        new("Home Assistant", "Home automation", "Home", "ghcr.io/home-assistant/home-assistant:stable", "services:\n  homeassistant:\n    image: ghcr.io/home-assistant/home-assistant:stable\n    ports:\n      - \"8123:8123\"\n    volumes:\n      - ./config:/config", "homeassistant", "https://home-assistant.io"),
        new("n8n", "Workflow automation", "Automation", "n8n/n8n:latest", "services:\n  n8n:\n    image: n8n/n8n:latest\n    ports:\n      - \"5678:5678\"\n    environment:\n      N8N_HOST: localhost\n      N8N_PORT: 5678\n    volumes:\n      - n8n_data:/home/node/.n8n", "n8n", "https://n8n.io"),
    ];

    #region Observable Properties

    private string? _imageSearchText;
    public string? ImageSearchText
    {
        get => _imageSearchText;
        set => SetProperty(ref _imageSearchText, value);
    }

    private string? _containerNameText;
    public string? ContainerNameText
    {
        get => _containerNameText;
        set => SetProperty(ref _containerNameText, value);
    }

    private string? _projectNameText;
    public string? ProjectNameText
    {
        get => _projectNameText;
        set => SetProperty(ref _projectNameText, value);
    }

    private bool _isSearchEnabled;
    public bool IsSearchEnabled
    {
        get => _isSearchEnabled;
        set => SetProperty(ref _isSearchEnabled, value);
    }

    private string? _searchButtonContent;
    public string? SearchButtonContent
    {
        get => _searchButtonContent;
        set => SetProperty(ref _searchButtonContent, value);
    }

    private string? _selectedContainerId;
    public string? SelectedContainerId
    {
        get => _selectedContainerId;
        set => SetProperty(ref _selectedContainerId, value);
    }

    private string? _newNameText;
    public string? NewNameText
    {
        get => _newNameText;
        set => SetProperty(ref _newNameText, value);
    }

    private ObservableCollection<ImageResult>? _imageResults;
    public ObservableCollection<ImageResult>? ImageResults
    {
        get => _imageResults;
        set => SetProperty(ref _imageResults, value);
    }

    private bool _showImageResults;
    public bool ShowImageResults
    {
        get => _showImageResults;
        set => SetProperty(ref _showImageResults, value);
    }

    private ImageResult? _selectedImageResult;
    public ImageResult? SelectedImageResult
    {
        get => _selectedImageResult;
        set => SetProperty(ref _selectedImageResult, value);
    }

    private string _restartPolicy = "no";
    public string RestartPolicy
    {
        get => _restartPolicy;
        set => SetProperty(ref _restartPolicy, value);
    }

    private bool _showMultiServiceSummary;
    public bool ShowMultiServiceSummary
    {
        get => _showMultiServiceSummary;
        set => SetProperty(ref _showMultiServiceSummary, value);
    }

    private List<ParsedServiceConfig> _parsedServices = [];
    public List<ParsedServiceConfig> ParsedServices
    {
        get => _parsedServices;
        set => SetProperty(ref _parsedServices, value);
    }

    private string? _composeYamlText;
    public string? ComposeYamlText
    {
        get => _composeYamlText;
        set => SetProperty(ref _composeYamlText, value);
    }

    private ObservableCollection<PortEntry> _ports;
    public ObservableCollection<PortEntry> Ports
    {
        get => _ports;
        set => SetProperty(ref _ports, value);
    }

    private ObservableCollection<VolumeEntry> _volumes;
    public ObservableCollection<VolumeEntry> Volumes
    {
        get => _volumes;
        set => SetProperty(ref _volumes, value);
    }

    private ObservableCollection<EnvVarEntry> _envVars;
    public ObservableCollection<EnvVarEntry> EnvVars
    {
        get => _envVars;
        set => SetProperty(ref _envVars, value);
    }

    private ObservableCollection<TemplateCatalogItem> _templateCatalog = [];
    public ObservableCollection<TemplateCatalogItem> TemplateCatalog
    {
        get => _templateCatalog;
        set => SetProperty(ref _templateCatalog, value);
    }

    private TemplateCatalogItem? _selectedTemplate;
    public TemplateCatalogItem? SelectedTemplate
    {
        get => _selectedTemplate;
        set => SetProperty(ref _selectedTemplate, value);
    }

    private bool _isRefreshingCatalog;
    public bool IsRefreshingCatalog
    {
        get => _isRefreshingCatalog;
        set => SetProperty(ref _isRefreshingCatalog, value);
    }

    private ObservableCollection<string> _categories = [];
    public ObservableCollection<string> Categories
    {
        get => _categories;
        set => SetProperty(ref _categories, value);
    }

    private string _selectedCategory = "All";
    public string SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
                ApplyCategoryFilter();
        }
    }

    private string _templateCatalogSearchText = "";
    public string TemplateCatalogSearchText
    {
        get => _templateCatalogSearchText;
        set
        {
            if (SetProperty(ref _templateCatalogSearchText, value))
                ApplyCategoryFilter();
        }
    }

    #endregion

    #region CRUD Operations

    public void AddPort()
    {
        Ports.Add(new PortEntry());
    }

    public void RemovePort(PortEntry entry)
    {
        Ports.Remove(entry);
    }

    public void AddVolume()
    {
        Volumes.Add(new VolumeEntry());
    }

    public void RemoveVolume(VolumeEntry entry)
    {
        Volumes.Remove(entry);
    }

    public void AddEnvVar()
    {
        EnvVars.Add(new EnvVarEntry());
    }

    public void RemoveEnvVar(EnvVarEntry entry)
    {
        EnvVars.Remove(entry);
    }

    #endregion

    #region Compose YAML Parsing

    public void ParseComposeYaml()
    {
        if (string.IsNullOrWhiteSpace(ComposeYamlText))
        {
            _output.Write("Paste a docker-compose YAML or docker run command first.", ServiceLogLevel.Warning);
            return;
        }

        var text = ComposeYamlText.Trim();

        if (text.StartsWith("docker run", StringComparison.OrdinalIgnoreCase))
        {
            ParseDockerRun(text);
            return;
        }

        try
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            var yaml = deserializer.Deserialize<ComposeRoot>(text);

            if (yaml?.Services == null || yaml.Services.Count == 0)
            {
                _output.Write("No services found in compose YAML.", ServiceLogLevel.Warning);
                return;
            }

            var services = new List<ParsedServiceConfig>();
            foreach (var (name, svc) in yaml.Services)
            {
                var cfg = new ParsedServiceConfig
                {
                    ServiceName = name,
                    Image = svc.Image ?? "",
                    ContainerName = !string.IsNullOrWhiteSpace(svc.ContainerName) ? svc.ContainerName : name,
                };

                if (svc.Ports != null)
                {
                    foreach (var portStr in svc.Ports)
                    {
                        var parts = portStr.Split(':');
                        if (parts.Length == 2)
                            cfg.Ports.Add((parts[0], parts[1]));
                    }
                }

                if (svc.Volumes != null)
                {
                    foreach (var volStr in svc.Volumes)
                    {
                        var parts = volStr.Split(':');
                        if (parts.Length >= 2)
                            cfg.Volumes.Add((parts[0], parts[1]));
                    }
                }

                if (svc.Environment is not null)
                {
                    if (svc.Environment is System.Collections.IDictionary envMap)
                    {
                        foreach (var key in envMap.Keys)
                            cfg.EnvVars.Add((key?.ToString() ?? "", envMap[key]?.ToString() ?? ""));
                    }
                    else if (svc.Environment is System.Collections.IEnumerable envList and not string)
                    {
                        foreach (var item in envList)
                        {
                            var envStr = item?.ToString() ?? "";
                            var eqIdx = envStr.IndexOf('=');
                            if (eqIdx > 0)
                                cfg.EnvVars.Add((envStr[..eqIdx], envStr[(eqIdx + 1)..]));
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(svc.Restart))
                {
                    cfg.RestartPolicy = svc.Restart.ToLowerInvariant() switch
                    {
                        "no" => "no",
                        "on-failure" => "on-failure",
                        "always" => "always",
                        "unless-stopped" => "unless-stopped",
                        _ => "no"
                    };
                }

                services.Add(cfg);
            }

            var first = services[0];
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ImageSearchText = first.Image;
                ContainerNameText = first.ContainerName;
                RestartPolicy = first.RestartPolicy;
            });
            PopulateFormFromService(first);

            _dispatcherQueue?.TryEnqueue(() =>
            {
                ParsedServices = services;
                ShowMultiServiceSummary = services.Count > 1;
                if (ProjectNameText == null)
                    ProjectNameText = first.ContainerName;
            });

            var names = string.Join(", ", services.Select(s => s.ContainerName));
            _output.Write($"Compose imported: {services.Count} service(s) — {names}");
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to parse compose YAML: {ex.Message}", ServiceLogLevel.Warning);
        }
    }

    private void PopulateFormFromService(ParsedServiceConfig svc)
    {
        Ports.Clear();
        foreach (var (host, container) in svc.Ports)
            Ports.Add(new PortEntry { Host = host, Container = container });

        Volumes.Clear();
        foreach (var (source, target) in svc.Volumes)
            Volumes.Add(new VolumeEntry { Source = source, Target = target });

        EnvVars.Clear();
        foreach (var (name, value) in svc.EnvVars)
            EnvVars.Add(new EnvVarEntry { Name = name, Value = value });
    }

    public async Task CreateAllFromComposeAsync()
    {
        var services = ParsedServices;
        if (services.Count == 0)
        {
            _output.Write("No parsed services. Parse a compose file first.", ServiceLogLevel.Warning);
            return;
        }

        _output.Write($"Creating {services.Count} service(s) from compose...");

        foreach (var svc in services)
        {
            _output.Write($"Pulling image '{svc.Image}'...");
            var pullOutput = await App.ServiceClient.PullImageAsync(svc.Image);
            _output.Write($"Pull '{svc.Image}': {pullOutput}");

            var ports = svc.Ports.Select(p => $"{p.Host}:{p.Container}").ToList();
            var volumes = svc.Volumes.Select(v => $"{v.Source}:{v.Target}").ToList();
            var env = svc.EnvVars.Select(e => string.IsNullOrWhiteSpace(e.Value) ? e.Name : $"{e.Name}={e.Value}").ToList();

            _output.Write($"Running container '{svc.ContainerName}' from '{svc.Image}' (ports={ports.Count}, volumes={volumes.Count}, env={env.Count})...");
            var runOutput = await App.ServiceClient.RunContainerAsync(svc.Image, svc.ContainerName, ports, volumes, env, svc.RestartPolicy);
            _output.Write($"Run '{svc.ContainerName}': {runOutput}");
        }

        _output.Write($"All {services.Count} service(s) processed.");
    }

    #endregion

    #region Docker Run Parsing

    private void ParseDockerRun(string text)
    {
        _dispatcherQueue?.TryEnqueue(() =>
        {
            ImageSearchText = "";
            ContainerNameText = "";
            RestartPolicy = "no";
        });
        Ports.Clear();
        Volumes.Clear();
        EnvVars.Clear();

        var args = TokenizeDockerRun(text);
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i].ToLowerInvariant())
            {
                case "--name":
                    if (i + 1 < args.Length)
                    {
                        var name = args[++i];
                        _dispatcherQueue?.TryEnqueue(() => ContainerNameText = name);
                    }
                    break;
                case "-p":
                case "--publish":
                    if (i + 1 < args.Length)
                    {
                        var portStr = args[++i];
                        var parts = portStr.Split(':');
                        if (parts.Length == 2)
                        {
                            Ports.Add(new PortEntry { Host = parts[0], Container = parts[1] });
                        }
                    }
                    break;
                case "-v":
                case "--volume":
                    if (i + 1 < args.Length)
                    {
                        var volStr = args[++i];
                        var parts = volStr.Split(':');
                        if (parts.Length >= 2)
                        {
                            Volumes.Add(new VolumeEntry { Source = parts[0], Target = parts[1] });
                        }
                    }
                    break;
                case "-e":
                case "--env":
                    if (i + 1 < args.Length)
                    {
                        var envStr = args[++i];
                        var eqIdx = envStr.IndexOf('=');
                        if (eqIdx > 0)
                        {
                            EnvVars.Add(new EnvVarEntry { Name = envStr[..eqIdx], Value = envStr[(eqIdx + 1)..] });
                        }
                    }
                    break;
                case "--restart":
                    if (i + 1 < args.Length)
                    {
                        var rp = args[++i].ToLowerInvariant();
                        var mapped = rp switch
                        {
                            "no" => "no",
                            "on-failure" => "on-failure",
                            "always" => "always",
                            "unless-stopped" => "unless-stopped",
                            _ => "no"
                        };
                        _dispatcherQueue?.TryEnqueue(() => RestartPolicy = mapped);
                    }
                    break;
            }
        }

        var lastNonFlag = "";
        var skipNext = false;
        for (var i = 1; i < args.Length; i++)
        {
            if (skipNext) { skipNext = false; continue; }
            if (args[i].StartsWith('-') && i + 1 < args.Length)
            {
                skipNext = true;
                continue;
            }
            if (!args[i].StartsWith('-'))
            {
                lastNonFlag = args[i];
            }
        }

        var image = "";
        if (!string.IsNullOrWhiteSpace(lastNonFlag) && !lastNonFlag.StartsWith('-'))
        {
            image = lastNonFlag;
        }

        if (!string.IsNullOrWhiteSpace(image))
        {
            _dispatcherQueue?.TryEnqueue(() => ImageSearchText = image);
        }
    }

    private static string[] TokenizeDockerRun(string text)
    {
        var tokens = new List<string>();
        var current = new System.Text.StringBuilder();
        var inQuote = false;
        var quoteChar = ' ';

        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (inQuote)
            {
                if (c == quoteChar)
                {
                    inQuote = false;
                }
                else
                {
                    current.Append(c);
                }
            }
            else if (c == '\'' || c == '"')
            {
                inQuote = true;
                quoteChar = c;
            }
            else if (char.IsWhiteSpace(c))
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return [.. tokens];
    }

    #endregion

    #region Docker Hub Search

    public void DebounceSearch(string query)
    {
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(400, token);
                if (!string.IsNullOrWhiteSpace(query))
                    await SearchDockerHubAsync(query);
            }
            catch (OperationCanceledException) { }
        });
    }

    public async Task SearchDockerHubAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            _dispatcherQueue?.TryEnqueue(() => ShowImageResults = false);
            return;
        }

        _dispatcherQueue?.TryEnqueue(() =>
        {
            IsSearchEnabled = false;
            SearchButtonContent = "Searching...";
        });
        _output.Write($"Searching Docker Hub for '{query}'...");

        try
        {
            var results = new List<ImageResult>();

            await SearchRepositoryAsync($"https://hub.docker.com/v2/search/repositories?query={Uri.EscapeDataString(query)}&page_size=20", results);

            _dispatcherQueue?.TryEnqueue(() =>
            {
                ImageResults = new ObservableCollection<ImageResult>(results);
                ShowImageResults = results.Count > 0;
            });
            _output.Write(results.Count > 0
                ? $"Found {results.Count} images for '{query}'"
                : $"No results found for '{query}'");
        }
        catch (Exception ex)
        {
            _output.Write($"Search failed: {ex.Message}", ServiceLogLevel.Warning);
        }
        finally
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                IsSearchEnabled = true;
                SearchButtonContent = "Search";
            });
        }
    }

    private async Task SearchRepositoryAsync(string url, List<ImageResult> results)
    {
        try
        {
            using var response = await DockerHubClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.TryGetProperty("results", out var resultsArray))
            {
                foreach (var item in resultsArray.EnumerateArray())
                {
                    var name = item.TryGetProperty("repo_name", out var n) ? n.GetString() ?? "" : "";
                    var desc = item.TryGetProperty("short_description", out var d) ? d.GetString() ?? "" : "";
                    var stars = item.TryGetProperty("star_count", out var s) ? s.GetInt32() : 0;
                    var pulls = item.TryGetProperty("pull_count", out var p) ? p.GetInt64() : 0;
                    var isOfficial = item.TryGetProperty("is_official", out var o) && o.GetBoolean();

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        results.Add(new ImageResult(name, desc, stars > 0 ? $"{stars:N0}" : "0", pulls > 0 ? $"{(pulls / 1_000_000):N1}" : "0", isOfficial));
                    }
                }
            }
        }
        catch (Exception ex) { _output.Write($"SearchRepositoryAsync failed: {ex.Message}", ServiceLogLevel.Warning); }
    }

    public void SelectImage(ImageResult? result)
    {
        if (result is not null)
        {
            _dispatcherQueue?.TryEnqueue(() =>
            {
                ImageSearchText = result.Name;
                ShowImageResults = false;
            });
        }
    }

    public void ApplyTemplate(TemplateCatalogItem? template)
    {
        if (template is null)
            return;

        ImageSearchText = template.Image;
        ContainerNameText = template.ContainerName;
        ComposeYamlText = template.Compose;
        ParseComposeYaml();
        _output.Write($"Applied template '{template.Name}'");
    }

    #endregion

    #region Container Management

    public async Task CreateAndStartContainerAsync()
    {
        var image = ImageSearchText?.Trim();
        if (string.IsNullOrWhiteSpace(image))
        {
            _output.Write("Select or type an image name first.", ServiceLogLevel.Warning);
            return;
        }

        _output.Write($"Pulling image '{image}'...");
        var pullOutput = await App.ServiceClient.PullImageAsync(image);
        _output.Write($"Pull: {pullOutput}");

        var name = ContainerNameText?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            name = $"wc-{image.Replace('/', '-').Replace(':', '-')}"
                .Trim('-')
                .ToLowerInvariant();
        }

        var ports = Ports
            .Where(p => !string.IsNullOrWhiteSpace(p.Host) && !string.IsNullOrWhiteSpace(p.Container))
            .Select(p => $"{p.Host}:{p.Container}")
            .ToList();

        var volumes = Volumes
            .Where(v => !string.IsNullOrWhiteSpace(v.Source) && !string.IsNullOrWhiteSpace(v.Target))
            .Select(v => $"{v.Source}:{v.Target}")
            .ToList();

        var env = EnvVars
            .Where(e => !string.IsNullOrWhiteSpace(e.Name))
            .Select(e => string.IsNullOrWhiteSpace(e.Value) ? e.Name : $"{e.Name}={e.Value}")
            .ToList();

        var restart = RestartPolicy;

        _output.Write($"Creating and starting container '{name}' from image '{image}'...");
        var runOutput = await App.ServiceClient.RunContainerAsync(image, name, ports, volumes, env, restart);
        _output.Write($"Run: {runOutput}");
    }

    public async Task RunQuickActionAsync(string scriptName)
    {
        var id = SelectedContainerId?.Trim();
        if (string.IsNullOrWhiteSpace(id))
        {
            _output.Write("Enter a container ID or name first.", ServiceLogLevel.Warning);
            return;
        }

        _output.Write($"Running {scriptName} for '{id}'...");

        var output = scriptName switch
        {
            "Start-Container" => await App.ServiceClient.StartContainerAsync(id),
            "Stop-Container" => await App.ServiceClient.StopContainerAsync(id),
            "Remove-Container" => await App.ServiceClient.RemoveContainerAsync(id),
            _ => null
        };
        _output.Write($"{scriptName}: {output ?? "(not implemented)"}");
    }

    public async Task PullImageAsync()
    {
        var image = ImageSearchText?.Trim();
        if (string.IsNullOrWhiteSpace(image))
        {
            _output.Write("Select or type an image name to pull.", ServiceLogLevel.Warning);
            return;
        }

        _output.Write($"Pulling image '{image}'...");
        var output = await App.ServiceClient.PullImageAsync(image);
        _output.Write($"Pull: {output}");
    }

    #endregion

    #region Helpers

    private static bool IsLikelyError(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        return text.Contains("FATA[", StringComparison.OrdinalIgnoreCase)
            || text.Contains("ERRO[", StringComparison.OrdinalIgnoreCase)
            || text.Contains("error", StringComparison.OrdinalIgnoreCase)
            || text.Contains("failed", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

internal class ComposeRoot
{
    public Dictionary<string, ComposeService>? Services { get; set; }
}

internal class ComposeService
{
    public string? Image { get; set; }
    public string? ContainerName { get; set; }
    public List<string>? Ports { get; set; }
    public List<string>? Volumes { get; set; }
    public object? Environment { get; set; }
    public string? Restart { get; set; }
}
