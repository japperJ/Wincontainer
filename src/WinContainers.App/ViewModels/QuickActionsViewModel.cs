using System.ComponentModel;
using System.Net.Http;
using System.Text.Json;
using Microsoft.UI.Dispatching;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
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
    private int _conflictCheckVersion;
    private bool _suppressFormConflictRefresh;

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
        ConflictWarnings = new ObservableCollection<string>();
        AttachConflictListeners();
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
        set
        {
            if (SetProperty(ref _containerNameText, value))
                _ = RefreshConflictsAsync();
        }
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

    private bool _showComposePreview;
    public bool ShowComposePreview
    {
        get => _showComposePreview;
        set => SetProperty(ref _showComposePreview, value);
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

    private ObservableCollection<string> _conflictWarnings = [];
    public ObservableCollection<string> ConflictWarnings
    {
        get => _conflictWarnings;
        set => SetProperty(ref _conflictWarnings, value);
    }

    private bool _hasConflicts;
    public bool HasConflicts
    {
        get => _hasConflicts;
        set => SetProperty(ref _hasConflicts, value);
    }

    private string _conflictSummary = string.Empty;
    public string ConflictSummary
    {
        get => _conflictSummary;
        set => SetProperty(ref _conflictSummary, value);
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

    public async Task<bool> ParseComposeYamlAsync()
    {
        if (string.IsNullOrWhiteSpace(ComposeYamlText))
        {
            _output.Write("Paste a docker-compose YAML or docker run command first.", ServiceLogLevel.Warning);
            ClearComposePreview();
            return false;
        }

        var text = ComposeYamlText.Trim();

        if (text.StartsWith("docker run", StringComparison.OrdinalIgnoreCase))
        {
            return await ParseDockerRunAsync(text);
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
                ClearComposePreview();
                return false;
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
            ImageSearchText = first.Image;
            ContainerNameText = first.ContainerName;
            RestartPolicy = first.RestartPolicy;
            PopulateFormFromService(first);

            ParsedServices = services;
            ShowComposePreview = true;
            if (string.IsNullOrWhiteSpace(ProjectNameText))
                ProjectNameText = first.ContainerName;
            await RefreshComposeConflictsAsync();

            var names = string.Join(", ", services.Select(s => s.ContainerName));
            _output.Write($"Compose imported: {services.Count} service(s) — {names}");
            return true;
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to parse compose YAML: {ex.Message}", ServiceLogLevel.Warning);
            ClearComposePreview();
            return false;
        }
    }

    private void ClearComposePreview()
    {
        ParsedServices = [];
        ShowComposePreview = false;
    }

    private void PopulateFormFromService(ParsedServiceConfig svc)
    {
        _suppressFormConflictRefresh = true;
        try
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

            AttachConflictListeners();
        }
        finally
        {
            _suppressFormConflictRefresh = false;
        }
    }

    private void AttachConflictListeners()
    {
        Ports.CollectionChanged -= OnPortsCollectionChanged;
        Volumes.CollectionChanged -= OnVolumesCollectionChanged;
        Ports.CollectionChanged += OnPortsCollectionChanged;
        Volumes.CollectionChanged += OnVolumesCollectionChanged;

        foreach (var entry in Ports)
            entry.PropertyChanged -= OnPortPropertyChanged;
        foreach (var entry in Volumes)
            entry.PropertyChanged -= OnVolumePropertyChanged;
        foreach (var entry in Ports)
            entry.PropertyChanged += OnPortPropertyChanged;
        foreach (var entry in Volumes)
            entry.PropertyChanged += OnVolumePropertyChanged;
    }

    private void OnPortsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (PortEntry item in e.OldItems)
                item.PropertyChanged -= OnPortPropertyChanged;
        }
        if (e.NewItems is not null)
        {
            foreach (PortEntry item in e.NewItems)
                item.PropertyChanged += OnPortPropertyChanged;
        }
        if (_suppressFormConflictRefresh) return;
        _ = RefreshConflictsAsync();
    }

    private void OnVolumesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems is not null)
        {
            foreach (VolumeEntry item in e.OldItems)
                item.PropertyChanged -= OnVolumePropertyChanged;
        }
        if (e.NewItems is not null)
        {
            foreach (VolumeEntry item in e.NewItems)
                item.PropertyChanged += OnVolumePropertyChanged;
        }
        if (_suppressFormConflictRefresh) return;
        _ = RefreshConflictsAsync();
    }

    private void OnPortPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressFormConflictRefresh) return;
        if (e.PropertyName == nameof(PortEntry.Host))
            _ = RefreshConflictsAsync();
    }

    private void OnVolumePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressFormConflictRefresh) return;
        if (e.PropertyName == nameof(VolumeEntry.Source) || e.PropertyName == nameof(VolumeEntry.Target))
            _ = RefreshConflictsAsync();
    }

    public async Task CreateAllFromComposeAsync()
    {
        var services = ParsedServices;
        if (services.Count == 0)
        {
            _output.Write("No parsed services. Parse a compose file first.", ServiceLogLevel.Warning);
            return;
        }

        await RefreshComposeConflictsAsync();
        if (HasConflicts)
            _output.Write($"Compose has {ConflictWarnings.Count} warning(s). Review the preview before continuing if these are not intentional.", ServiceLogLevel.Warning);

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

    private async Task<bool> ParseDockerRunAsync(string text)
    {
        ImageSearchText = "";
        ContainerNameText = "";
        RestartPolicy = "no";
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
                        ContainerNameText = name;
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
                        RestartPolicy = mapped;
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
            ImageSearchText = image;
            var containerName = string.IsNullOrWhiteSpace(ContainerNameText)
                ? image.Split(['/', ':'], StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? image
                : ContainerNameText;
            ParsedServices =
            [
                new ParsedServiceConfig
                {
                    ServiceName = containerName,
                    Image = image,
                    ContainerName = containerName,
                    Ports = [.. Ports.Select(p => (p.Host, p.Container))],
                    Volumes = [.. Volumes.Select(v => (v.Source, v.Target))],
                    EnvVars = [.. EnvVars.Select(e => (e.Name, e.Value))],
                    RestartPolicy = RestartPolicy
                }
            ];
            ShowComposePreview = true;
            await RefreshComposeConflictsAsync();
            _output.Write($"Docker run imported: {containerName} ({image})");
            return true;
        }

        _output.Write("Could not find an image name in the docker run command.", ServiceLogLevel.Warning);
        ClearComposePreview();
        return false;
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

    public async Task ApplyTemplateAsync(TemplateCatalogItem? template)
    {
        if (template is null)
            return;

        ImageSearchText = template.Image;
        ContainerNameText = template.ContainerName;
        ComposeYamlText = template.Compose;
        await ParseComposeYamlAsync();
        _output.Write($"Applied template '{template.Name}'");
    }

    public async Task RefreshConflictsAsync()
        => await RefreshConflictsAsync([BuildCurrentServiceConfig()]);

    public async Task RefreshComposeConflictsAsync()
        => await RefreshConflictsAsync(ParsedServices);

    private ParsedServiceConfig BuildCurrentServiceConfig()
    {
        var image = ImageSearchText?.Trim() ?? string.Empty;
        var name = ContainerNameText?.Trim();
        if (string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(image))
        {
            name = $"wc-{image.Replace('/', '-').Replace(':', '-')}"
                .Trim('-')
                .ToLowerInvariant();
        }

        return new ParsedServiceConfig
        {
            ServiceName = name ?? string.Empty,
            Image = image,
            ContainerName = name ?? string.Empty,
            Ports = [.. Ports.Select(p => (p.Host, p.Container))],
            Volumes = [.. Volumes.Select(v => (v.Source, v.Target))],
            EnvVars = [.. EnvVars.Select(e => (e.Name, e.Value))],
            RestartPolicy = RestartPolicy
        };
    }

    private async Task RefreshConflictsAsync(IReadOnlyList<ParsedServiceConfig> services)
    {
        var checkVersion = Interlocked.Increment(ref _conflictCheckVersion);
        try
        {
            var output = await App.ServiceClient.GetContainersAsync();
            var running = WslcContainerParser.ParseContainers(output ?? "")
                .Where(c => ContainerService.IsRunningStatus(c.Status))
                .ToList();

            var warnings = new List<string>();

            foreach (var service in services)
            {
                var name = service.ContainerName?.Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    var match = running.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
                    if (match is not null)
                        warnings.Add($"Container name '{name}' is already used by running container '{match.Name}' ({match.Image}).");
                }
            }

            warnings.AddRange(FindDuplicateServiceNames(services));

            var usedHostPorts = running
                .SelectMany(c => c.PortLinks)
                .Select(l => l.Url?[("localhost:".Length)..])
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var (service, hostPort) in services.SelectMany(s => s.Ports
                         .Select(p => (Service: s, HostPort: ExtractPortNumber(p.Host)))
                         .Where(item => !string.IsNullOrWhiteSpace(item.HostPort))))
            {
                if (!string.IsNullOrWhiteSpace(hostPort) && usedHostPorts.Contains(hostPort))
                    warnings.Add($"Host port {hostPort} for '{service.ContainerName}' is already mapped by a running container.");
            }

            warnings.AddRange(FindDuplicateHostPorts(services));

            var usedMounts = new List<MountInfo>();
            foreach (var container in running)
            {
                try
                {
                    var inspectOutput = await App.ServiceClient.InspectContainerAsync(container.Id);
                    usedMounts.AddRange(WslcContainerParser.ParseMountsFromInspect(inspectOutput ?? ""));
                }
                catch (Exception ex)
                {
                    _output.Write($"Mount conflict check skipped for '{container.Name}': {ex.Message}", ServiceLogLevel.Warning);
                }
            }

            // Fallback to any mounts already present in the ps output
            usedMounts.AddRange(running.SelectMany(c => c.MountInfos));

            var normalizedMounts = usedMounts
                .Select(m => (Source: NormalizeMountPath(m.Source), Target: NormalizeMountPath(m.Target)))
                .ToHashSet();

            foreach (var (service, volume) in services.SelectMany(s => s.Volumes.Select(v => (Service: s, Volume: v))))
            {
                var hasSource = !string.IsNullOrWhiteSpace(volume.Source);
                var hasTarget = !string.IsNullOrWhiteSpace(volume.Target);
                if (!hasSource && !hasTarget) continue;

                var normalizedSource = NormalizeMountPath(volume.Source);
                var normalizedTarget = NormalizeMountPath(volume.Target);

                foreach (var mount in normalizedMounts)
                {
                    var sourceClash = hasSource &&
                                      !string.IsNullOrWhiteSpace(mount.Source) &&
                                      mount.Source.Equals(normalizedSource, StringComparison.OrdinalIgnoreCase);

                    var targetClash = hasTarget &&
                                      !string.IsNullOrWhiteSpace(mount.Target) &&
                                      mount.Target.Equals(normalizedTarget, StringComparison.OrdinalIgnoreCase);

                    if (sourceClash && targetClash)
                    {
                        warnings.Add($"Volume '{volume.Source}:{volume.Target}' for '{service.ContainerName}' is already mounted by a running container.");
                    }
                    else if (sourceClash)
                    {
                        warnings.Add($"Volume source '{volume.Source}' for '{service.ContainerName}' is already mounted to '{mount.Target}' by a running container.");
                    }
                    else if (targetClash)
                    {
                        warnings.Add($"Volume target '{volume.Target}' for '{service.ContainerName}' is already in use by a running container (source: '{mount.Source}').");
                    }
                }
            }

            warnings.AddRange(FindDuplicateVolumes(services));

            if (checkVersion != _conflictCheckVersion)
                return;

            ConflictWarnings = new ObservableCollection<string>(warnings);
            HasConflicts = warnings.Count > 0;
            ConflictSummary = warnings.Count > 0
                ? $"{warnings.Count} potential conflict(s) detected"
                : string.Empty;
        }
        catch (Exception ex)
        {
            _output.Write($"Conflict check failed: {ex.Message}", ServiceLogLevel.Warning);
        }
    }

    private static IEnumerable<string> FindDuplicateServiceNames(IReadOnlyList<ParsedServiceConfig> services)
        => services
            .Select(s => s.ContainerName?.Trim())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .GroupBy(name => name!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"Container name '{group.Key}' is used by multiple parsed services.");

    private static IEnumerable<string> FindDuplicateHostPorts(IReadOnlyList<ParsedServiceConfig> services)
        => services
            .SelectMany(s => s.Ports.Select(p => (Service: s.ContainerName, HostPort: ExtractPortNumber(p.Host))))
            .Where(item => !string.IsNullOrWhiteSpace(item.HostPort))
            .GroupBy(item => item.HostPort, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => $"Host port {group.Key} is used by multiple parsed services: {string.Join(", ", group.Select(item => item.Service).Distinct(StringComparer.OrdinalIgnoreCase))}.");

    private static IEnumerable<string> FindDuplicateVolumes(IReadOnlyList<ParsedServiceConfig> services)
    {
        var mounts = services
            .SelectMany(s => s.Volumes.Select(v => (
                Service: s.ContainerName,
                Source: NormalizeMountPath(v.Source),
                Target: NormalizeMountPath(v.Target))))
            .Where(item => !string.IsNullOrWhiteSpace(item.Source) || !string.IsNullOrWhiteSpace(item.Target))
            .ToList();

        foreach (var group in mounts.Where(m => !string.IsNullOrWhiteSpace(m.Source)).GroupBy(m => m.Source, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
                yield return $"Volume source '{group.Key}' is used by multiple parsed services: {string.Join(", ", group.Select(item => item.Service).Distinct(StringComparer.OrdinalIgnoreCase))}.";
        }

        foreach (var group in mounts.Where(m => !string.IsNullOrWhiteSpace(m.Target)).GroupBy(m => m.Target, StringComparer.OrdinalIgnoreCase))
        {
            if (group.Count() > 1)
                yield return $"Volume target '{group.Key}' is used by multiple parsed services: {string.Join(", ", group.Select(item => item.Service).Distinct(StringComparer.OrdinalIgnoreCase))}.";
        }
    }

    private static string ExtractPortNumber(string hostPart)
    {
        if (string.IsNullOrWhiteSpace(hostPart)) return string.Empty;
        var lastColon = hostPart.LastIndexOf(':');
        var candidate = lastColon >= 0 ? hostPart[(lastColon + 1)..] : hostPart;
        return candidate.Trim();
    }

    private static string NormalizeMountPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return string.Empty;
        return path.Replace('\\', '/').Trim().TrimEnd('/');
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
