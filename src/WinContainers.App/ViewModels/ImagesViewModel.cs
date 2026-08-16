using System.Collections.ObjectModel;
using System.Text.Json;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class ImagesViewModel : ViewModelBase
{
    private const int BackgroundPollIntervalMs = 10000;
    private readonly IOutputService _output;
    private readonly IWslcServiceClient _serviceClient;

    private CancellationTokenSource? _pollCts;

    private string? _statusText;
    public string? StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private bool _showDetail;
    public bool ShowDetail
    {
        get => _showDetail;
        set => SetProperty(ref _showDetail, value);
    }

    private ImageEntryData? _selectedImage;
    public ImageEntryData? SelectedImage
    {
        get => _selectedImage;
        set => SetProperty(ref _selectedImage, value);
    }

    private string? _inspectJson;
    public string? InspectJson
    {
        get => _inspectJson;
        set => SetProperty(ref _inspectJson, value);
    }

    public ObservableCollection<ImageEntryData> Images { get; } = [];
    public ObservableCollection<ImageLayerData> Layers { get; } = [];

    private string _layersCountText = "";
    public string LayersCountText
    {
        get => _layersCountText;
        set => SetProperty(ref _layersCountText, value);
    }

    public ImagesViewModel(IOutputService output, IWslcServiceClient serviceClient)
    {
        _output = output;
        _serviceClient = serviceClient;
    }

    public async Task LoadImagesAsync()
    {
        IsLoading = true;
        StatusText = "Loading images...";

        var imageOutput = await _serviceClient.GetImagesAsync();
        var images = WslcContainerParser.ParseImages(imageOutput ?? "");

        var containerOutput = await _serviceClient.GetContainersAsync();
        var containers = WslcContainerParser.ParseContainers(containerOutput ?? "");
        var inUseNames = WslcContainerParser.GetInUseImageNames(containers);

        foreach (var img in images)
        {
            var nameTag = $"{img.Repository}:{img.Tag}";
            img.InUse = inUseNames.Contains(nameTag, StringComparer.OrdinalIgnoreCase);
        }

        Images.Clear();
        foreach (var img in images)
            Images.Add(img);

        StatusText = $"{Images.Count} image(s)";
        IsLoading = false;
    }

    public async Task LoadImageDetailAsync(ImageEntryData image)
    {
        SelectedImage = image;
        ShowDetail = true;
        StatusText = $"Loading details for {image.FullTag}...";

        InspectJson = "{}";
        Layers.Clear();

        StatusText = $"{image.FullTag} — {Layers.Count} layer(s)";
    }

    public async Task UpdateImageAsync(ImageEntryData image)
    {
        IsLoading = true;
        StatusText = $"Pulling latest version of {image.FullTag}...";

        try
        {
            var output = await _serviceClient.PullImageAsync(image.FullTag);
            if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("wslc error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(output);

            _output.Write($"Updated image {image.FullTag}");
            await LoadImagesAsync();
        }
        catch (Exception ex)
        {
            _output.Write($"Update image failed: {ex.Message}", Services.LogLevel.Error);
            throw;
        }
    }

    public async Task RecreateContainersForImageAsync(ImageEntryData image)
    {
        var containerOutput = await _serviceClient.GetContainersAsync();
        var containers = WslcContainerParser.ParseContainers(containerOutput ?? "");
        var matching = containers.Where(c =>
        {
            var ci = c.Image ?? "";
            if (!ci.Contains(':'))
                ci += ":latest";
            return ci.Equals(image.FullTag, StringComparison.OrdinalIgnoreCase);
        }).ToList();

        if (matching.Count == 0)
        {
            _output.Write($"No containers found using image {image.FullTag}");
            return;
        }

        IsLoading = true;
        StatusText = $"Recreating {matching.Count} container(s) using {image.FullTag}...";

        foreach (var c in matching)
        {
            try
            {
                _output.Write($"Recreating container '{c.Name}' ({c.Id})...");

                // Inspect the container first to capture mounts and env (not available in ps output)
                var inspectRaw = await _serviceClient.InspectContainerAsync(c.Id);
                _output.Write($"Raw inspect output (first 500): {(inspectRaw?.Length > 500 ? inspectRaw[..500] : inspectRaw) ?? "null"}");
                var inspectMounts = WslcContainerParser.ParseMountsFromInspect(inspectRaw ?? "");
                var inspectEnv = WslcContainerParser.ParseEnvFromInspect(inspectRaw ?? "");
                var jsonKeys = WslcContainerParser.GetTopLevelJsonKeys(inspectRaw ?? "");

                // Fall back to locally stored config when WSLC inspect doesn't return mounts/env
                var savedConfig = ContainerConfigStore.LoadConfig(c.Name);
                _output.Write($"Config store for '{c.Name}': found={(savedConfig != null)}, volumes={savedConfig?.Volumes.Count ?? 0}, env={savedConfig?.Env.Count ?? 0}");
                if ((inspectMounts == null || inspectMounts.Count == 0) && savedConfig?.Volumes.Count > 0)
                {
                    _output.Write($"Using saved config for volumes ({savedConfig.Volumes.Count} entries)");
                    inspectMounts = savedConfig.Volumes
                        .Select(v =>
                        {
                            var parts = v.Split(':', 2);
                            return new MountInfo(parts[0], parts.Length > 1 ? parts[1] : "");
                        })
                        .ToList();
                }
                if ((inspectEnv == null || inspectEnv.Count == 0) && savedConfig?.Env.Count > 0)
                {
                    _output.Write($"Using saved config for env vars ({savedConfig.Env.Count} entries)");
                    inspectEnv = savedConfig.Env;
                }

                _output.Write($"Inspect: {inspectMounts?.Count ?? 0} mounts, {inspectEnv?.Count ?? 0} env vars — top-level keys: [{jsonKeys}]");

                if (WslcContainerParser.IsRunningStatus(c.Status))
                    await _serviceClient.StopContainerAsync(c.Id);

                await _serviceClient.RemoveContainerAsync(c.Id);

                var ports = savedConfig?.Ports.Count > 0
                    ? savedConfig.Ports.ToList()
                    : c.Ports is not null && c.Ports != "No ports"
                        ? c.Ports.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Replace("->", ":"))
                            .ToList()
                        : null;

                // Prefer inspect mounts (full details) over ps-output mounts
                var volumes = inspectMounts?.Count > 0
                    ? inspectMounts.Select(m => $"{m.Source}:{m.Target}").ToList()
                    : c.MountInfos?.Count > 0
                        ? c.MountInfos.Select(m => $"{m.Source}:{m.Target}").ToList()
                        : null;

                // Prefer inspect env (not available in ps output at all) over whatever the parser found
                var env = inspectEnv?.Count > 0 ? inspectEnv : c.Env;

                _output.Write($"Forwarding: {ports?.Count ?? 0} ports, {volumes?.Count ?? 0} volumes, {env?.Count ?? 0} env vars");

                await _serviceClient.RunContainerAsync(image.FullTag, c.Name, ports, volumes, env, savedConfig?.Network);
                _output.Write($"Recreated container '{c.Name}' with updated image {image.FullTag}");

                // Save config for future updates — WSLC inspect is unreliable for mounts/env,
                // so persisting the actual config used ensures we can recover it next time.
                var recreateConfig = new ContainerRunConfig
                {
                    Image = image.FullTag,
                    Ports = ports ?? [],
                    Volumes = volumes ?? [],
                    Env = env ?? [],
                    Network = savedConfig?.Network,
                    AllowLocalNetworkAccess = savedConfig?.AllowLocalNetworkAccess ?? false
                };
                ContainerConfigStore.SaveConfig(c.Name, recreateConfig);
                _output.Write($"Saved recreated config for '{c.Name}' ({recreateConfig.Volumes.Count} volumes, {recreateConfig.Env.Count} env vars)");

            }
            catch (Exception ex)
            {
                _output.Write($"Failed to recreate container '{c.Name}': {ex.Message}", Services.LogLevel.Error);
            }
        }

        await LoadImagesAsync();
    }

    public async Task DeleteImageAsync(ImageEntryData image)
    {
        try
        {
            var output = await _serviceClient.RemoveImageAsync(image.ID);
            if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(output);

            _output.Write($"Removed image {image.FullTag}");
            await LoadImagesAsync();
        }
        catch (Exception ex)
        {
            _output.Write($"Remove image failed: {ex.Message}", Services.LogLevel.Error);
            throw;
        }
    }

    public async Task LoadInspectAsync(string imageId)
    {
        InspectJson = "{\"info\": \"Inspect not available via WSLC API\"}";
    }

    public void CloseDetail()
    {
        ShowDetail = false;
        SelectedImage = null;
        Layers.Clear();
        LayersCountText = "";
        InspectJson = null;
        StatusText = $"{Images.Count} image(s)";
    }

    public void StartPolling()
    {
        if (_pollCts is not null) return;
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    public void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await QuietRefreshAsync();
            await Task.Delay(BackgroundPollIntervalMs, ct);
        }
    }

    /// <summary>
    /// Refreshes the image list silently for background polling — no IsLoading/StatusText changes.
    /// </summary>
    private async Task QuietRefreshAsync()
    {
        try
        {
            var imageOutput = await _serviceClient.GetImagesAsync();
            var images = WslcContainerParser.ParseImages(imageOutput ?? "");

            var containerOutput = await _serviceClient.GetContainersAsync();
            var containers = WslcContainerParser.ParseContainers(containerOutput ?? "");
            var inUseNames = WslcContainerParser.GetInUseImageNames(containers);

            foreach (var img in images)
            {
                var nameTag = $"{img.Repository}:{img.Tag}";
                img.InUse = inUseNames.Contains(nameTag, StringComparer.OrdinalIgnoreCase);
            }

            App.DispatcherQueue.TryEnqueue(() =>
            {
                Images.Clear();
                foreach (var img in images)
                    Images.Add(img);
            });
        }
        catch (Exception ex)
        {
            _output.Write($"Image refresh failed: {ex.Message}", Services.LogLevel.Warning);
        }
    }
}
