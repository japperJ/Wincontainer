using System.Collections.ObjectModel;
using System.Text.Json;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class ImagesViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly IWslcServiceClient _serviceClient;

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
                if (inspectMounts is null || inspectMounts.Count == 0)
                {
                    var keys = WslcContainerParser.GetTopLevelJsonKeys(inspectRaw ?? "");
                    _output.Write($"Inspect mounts: 0 — top-level keys: [{keys}]");
                }
                else
                {
                    _output.Write($"Inspect mounts: {inspectMounts.Count}, env vars: {inspectEnv?.Count ?? 0}");
                }

                if (WslcContainerParser.IsRunningStatus(c.Status))
                    await _serviceClient.StopContainerAsync(c.Id);

                await _serviceClient.RemoveContainerAsync(c.Id);

                var ports = c.Ports is not null && c.Ports != "No ports"
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

                await _serviceClient.RunContainerAsync(image.FullTag, c.Name, ports, volumes, env);
                _output.Write($"Recreated container '{c.Name}' with updated image {image.FullTag}");
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
}
