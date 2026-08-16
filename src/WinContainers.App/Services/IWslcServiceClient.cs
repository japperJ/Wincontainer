using WinContainers.Runtime;

namespace WinContainers_App.Services;

public interface IWslcServiceClient
{
    Task<bool> IsHealthyAsync();
    Task<string> GetVersionAsync();
    Task<string> GetContainersAsync();
    Task<string> StartContainerAsync(string id);
    Task<string> StopContainerAsync(string id);
    Task<string> RestartContainerAsync(string id);
    Task<string> RenameContainerAsync(string id, string name);
    Task<string> InspectContainerAsync(string id);
    Task<string> RemoveContainerAsync(string id);
    Task<string> GetContainerLogsAsync(string id, int tail = 500);
    Task<string> GetImagesAsync();
    Task<string> PullImageAsync(string image);
    Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, string? network = null);
    Task<ContainerAccessResult> SetContainerAccessAsync(string containerId, bool allowLocalNetworkAccess, string? containerName = null);
    Task<string> RemoveImageAsync(string id);
    Task<string> GetVolumesAsync();
    Task<string> CreateVolumeAsync(string name);
    Task<string> RemoveVolumeAsync(string name);
    Task<string> GetNetworksAsync();
    Task<string> CreateNetworkAsync(string name);
    Task<string> RemoveNetworkAsync(string name);
    Task<string> ExecContainerAsync(string id, string command, bool useShell = false, string? shell = null);
}
