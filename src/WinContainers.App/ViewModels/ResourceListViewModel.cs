using System.Collections.ObjectModel;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public sealed class ResourceListViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private string _resourceType = "Volumes";
    private string _statusText = "";
    private bool _isLoading;

    public ObservableCollection<ResourceEntryData> Resources { get; } = [];

    public string ResourceType
    {
        get => _resourceType;
        set
        {
            if (SetProperty(ref _resourceType, value))
                OnPropertyChanged(nameof(Title));
        }
    }

    public string Title => ResourceType;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ResourceListViewModel(IOutputService output)
    {
        _output = output;
    }

    public async Task LoadAsync()
    {
        IsLoading = true;
        StatusText = $"Loading {ResourceType.ToLowerInvariant()}...";

        try
        {
            var output = string.Equals(ResourceType, "Networks", StringComparison.OrdinalIgnoreCase)
                ? await App.ServiceClient.GetNetworksAsync()
                : await App.ServiceClient.GetVolumesAsync();

            var resources = string.Equals(ResourceType, "Networks", StringComparison.OrdinalIgnoreCase)
                ? WslcResourceParser.ParseNetworks(output)
                : WslcResourceParser.ParseVolumes(output);

            Resources.Clear();
            foreach (var resource in resources)
                Resources.Add(resource);

            StatusText = $"{Resources.Count} {ResourceType.ToLowerInvariant()}";
        }
        catch (Exception ex)
        {
            Resources.Clear();
            StatusText = $"Failed to load {ResourceType.ToLowerInvariant()}: {ex.Message}";
            _output.Write(StatusText, Services.LogLevel.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task DeleteAsync(ResourceEntryData resource)
    {
        try
        {
            var output = string.Equals(ResourceType, "Networks", StringComparison.OrdinalIgnoreCase)
                ? await App.ServiceClient.RemoveNetworkAsync(resource.Name)
                : await App.ServiceClient.RemoveVolumeAsync(resource.Name);

            if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(output);

            _output.Write($"Removed {ResourceType[..^1].ToLowerInvariant()} '{resource.Name}'");
            await LoadAsync();
        }
        catch (Exception ex)
        {
            _output.Write($"Remove {ResourceType[..^1].ToLowerInvariant()} failed: {ex.Message}", Services.LogLevel.Error);
            throw;
        }
    }
}
