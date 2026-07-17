using System.Collections.ObjectModel;
using System.Text.Json;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class ImagesViewModel : ViewModelBase
{
    private readonly ContainerService _containerService;
    private readonly IOutputService _output;

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

    public ImagesViewModel(ContainerService containerService, IOutputService output)
    {
        _containerService = containerService;
        _output = output;
    }

    public async Task LoadImagesAsync()
    {
        IsLoading = true;
        StatusText = "Loading images...";

        var imageOutput = await App.ServiceClient.GetImagesAsync();
        var images = WslcContainerParser.ParseImages(imageOutput ?? "");

        var containerOutput = await App.ServiceClient.GetContainersAsync();
        var containers = WslcContainerParser.ParseContainers(containerOutput ?? "");
        var inUseNames = _containerService.GetInUseImageNames(containers);

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

    public async Task DeleteImageAsync(ImageEntryData image)
    {
        try
        {
            var output = await App.ServiceClient.RemoveImageAsync(image.ID);
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
