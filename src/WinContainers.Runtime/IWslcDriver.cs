namespace WinContainers.Runtime;

public interface IWslcDriver
{
    Task<bool> IsAvailableAsync(CancellationToken ct);
    Task<string> GetVersionAsync(CancellationToken ct);
    Task<string> GetContainersAsync(CancellationToken ct);
    Task<string> StartContainerAsync(string id, CancellationToken ct);
    Task<string> StopContainerAsync(string id, CancellationToken ct);
    Task<string> RestartContainerAsync(string id, CancellationToken ct);
    Task<string> RenameContainerAsync(string id, string name, CancellationToken ct);
    Task<string> RemoveContainerAsync(string id, CancellationToken ct);
    Task<string> InspectContainerAsync(string id, CancellationToken ct);
    Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct);
    Task<string> GetImagesAsync(CancellationToken ct);
    Task<string> PullImageAsync(string image, CancellationToken ct);
    Task<string> RemoveImageAsync(string id, CancellationToken ct);
    Task<string> InspectImageAsync(string id, CancellationToken ct);
    Task<string> GetVolumesAsync(CancellationToken ct);
    Task<string> CreateVolumeAsync(string name, CancellationToken ct);
    Task<string> RemoveVolumeAsync(string name, CancellationToken ct);
    Task<string> InspectVolumeAsync(string name, CancellationToken ct);
    Task<string> GetNetworksAsync(CancellationToken ct);
    Task<string> CreateNetworkAsync(string name, CancellationToken ct);
    Task<string> RemoveNetworkAsync(string name, CancellationToken ct);
    Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, CancellationToken ct = default);
    Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default);
    Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default);
}
