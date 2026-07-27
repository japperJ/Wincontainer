using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers_App.Services;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public partial class OverviewViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly IWslcServiceClient _serviceClient;

    private string? _statusText;
    public string? StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string? _containerCountText;
    public string? ContainerCountText
    {
        get => _containerCountText;
        set => SetProperty(ref _containerCountText, value);
    }

    private string? _runningCountText;
    public string? RunningCountText
    {
        get => _runningCountText;
        set => SetProperty(ref _runningCountText, value);
    }

    private string? _imageCountText;
    public string? ImageCountText
    {
        get => _imageCountText;
        set => SetProperty(ref _imageCountText, value);
    }

    private string? _wsclVersionText;
    public string? WslcVersionText
    {
        get => _wsclVersionText;
        set => SetProperty(ref _wsclVersionText, value);
    }

    private bool _isRuntimeAvailable;
    public bool IsRuntimeAvailable
    {
        get => _isRuntimeAvailable;
        set => SetProperty(ref _isRuntimeAvailable, value);
    }

    private bool _showSetupHint;
    public bool ShowSetupHint
    {
        get => _showSetupHint;
        set => SetProperty(ref _showSetupHint, value);
    }

    private string? _setupHintText;
    public string? SetupHintText
    {
        get => _setupHintText;
        set => SetProperty(ref _setupHintText, value);
    }

    public OverviewViewModel(IOutputService output, IWslcServiceClient serviceClient)
    {
        _output = output;
        _serviceClient = serviceClient;
    }

    public async Task RefreshAsync()
    {
        try
        {
            var healthy = await _serviceClient.IsHealthyAsync();
            IsRuntimeAvailable = healthy;

            var version = healthy ? await _serviceClient.GetVersionAsync() : "(unknown)";
            WslcVersionText = healthy ? $"WSLC: {version}" : "WSLC: unavailable";

            var containerOutput = await _serviceClient.GetContainersAsync();
            var containers = WslcContainerParser.ParseContainers(containerOutput ?? "");

            var totalCount = containers.Count;
            var runningCount = containers.Count(c => WslcContainerParser.IsRunningStatus(c.Status));

            ContainerCountText = $"Containers: {totalCount}";
            RunningCountText = $"Running: {runningCount}";

            var imageOutput = await _serviceClient.GetImagesAsync();
            var images = WslcContainerParser.ParseImages(imageOutput ?? "");
            ImageCountText = $"Images: {images.Count}";

            ShowSetupHint = !IsRuntimeAvailable;
            SetupHintText = IsRuntimeAvailable
                ? ""
                : "WSLC runtime is not available.";
        }
        catch
        {
            StatusText = "Unable to reach service.";
            IsRuntimeAvailable = false;
        }
    }
}
