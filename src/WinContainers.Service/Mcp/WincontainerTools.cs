using System.ComponentModel;
using ModelContextProtocol.Server;
using WinContainers.Runtime;

namespace WinContainers.Service.Mcp;

/// <summary>
/// MCP tools that expose the Wincontainer runtime (wslc) to AI coders.
/// Each tool wraps an IWslcDriver method with typed parameters and descriptions
/// so that AI clients can discover and invoke them directly.
/// </summary>
[McpServerToolType]
public class WincontainerTools
{
    // ── Container lifecycle ──────────────────────────────────────────

    [McpServerTool, Description("List all containers managed by the runtime. Returns JSON output from wslc.")]
    public static async Task<string> ListContainers(IWslcDriver driver, CancellationToken ct)
        => await driver.GetContainersAsync(ct);

    [McpServerTool, Description("Run (create + start) a new container from an image.")]
    public static async Task<string> RunContainer(
        [Description("Image name, e.g. 'ubuntu:latest' or 'myapp:1.0'")] string image,
        [Description("Optional container name")] string? name = null,
        [Description("Comma-separated port mappings, e.g. '80:80,8080:80/tcp'")] string? ports = null,
        [Description("Comma-separated volume mounts, e.g. '/host:/container,/data:/data'")] string? volumes = null,
        [Description("Comma-separated environment variables, e.g. 'KEY1=val1,KEY2=val2'")] string? env = null,
        IWslcDriver driver = null!,
        CancellationToken ct = default)
        => await driver.RunContainerAsync(
            image, name,
            ports?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            volumes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            env?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ct);

    [McpServerTool, Description("Start a stopped container by ID or name.")]
    public static async Task<string> StartContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.StartContainerAsync(id, ct);

    [McpServerTool, Description("Stop a running container by ID or name.")]
    public static async Task<string> StopContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.StopContainerAsync(id, ct);

    [McpServerTool, Description("Restart a container by ID or name.")]
    public static async Task<string> RestartContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RestartContainerAsync(id, ct);

    [McpServerTool, Description("Rename an existing container.")]
    public static async Task<string> RenameContainer(
        [Description("Container ID or name")] string id,
        [Description("New container name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RenameContainerAsync(id, name, ct);

    [McpServerTool, Description("Remove (delete) a container by ID or name.")]
    public static async Task<string> RemoveContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RemoveContainerAsync(id, ct);

    [McpServerTool, Description("Inspect a container and return detailed configuration and status information.")]
    public static async Task<string> InspectContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.InspectContainerAsync(id, ct);

    [McpServerTool, Description("Execute a command inside a running container and return its output.")]
    public static async Task<string> ExecCommand(
        [Description("Container ID or name")] string id,
        [Description("Command to run inside the container, e.g. 'ls -la /app'")] string command,
        [Description("If true, run the command through a shell (e.g. bash -c)")] bool useShell = false,
        [Description("Shell to use when useShell is true, e.g. '/bin/bash'")] string? shell = null,
        IWslcDriver driver = null!,
        CancellationToken ct = default)
    {
        if (useShell)
            return await driver.ExecShellAsync(id, command, shell, ct);
        return await driver.ExecCommandAsync(id, command, ct);
    }

    [McpServerTool, Description("Get recent logs from a container.")]
    public static async Task<string> GetContainerLogs(
        [Description("Container ID or name")] string id,
        [Description("Number of recent log lines to return (default 500)")] int? tail = null,
        IWslcDriver driver = null!,
        CancellationToken ct = default)
        => await driver.GetContainerLogsAsync(id, tail ?? 500, ct);

    // ── Images ───────────────────────────────────────────────────────

    [McpServerTool, Description("List all downloaded container images.")]
    public static async Task<string> ListImages(IWslcDriver driver, CancellationToken ct)
        => await driver.GetImagesAsync(ct);

    [McpServerTool, Description("Pull (download) a container image from a registry.")]
    public static async Task<string> PullImage(
        [Description("Image name to pull, e.g. 'ubuntu:24.04' or 'nginx:alpine'")] string image,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.PullImageAsync(image, ct);

    [McpServerTool, Description("Remove (delete) a downloaded image by ID or tag.")]
    public static async Task<string> RemoveImage(
        [Description("Image ID or tag")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RemoveImageAsync(id, ct);

    [McpServerTool, Description("Inspect an image and return detailed metadata.")]
    public static async Task<string> InspectImage(
        [Description("Image ID or tag")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.InspectImageAsync(id, ct);

    [McpServerTool, Description("Load a local .tar container image archive into the WSLC image store.")]
    public static async Task<string> LoadImage(
        [Description("Existing local .tar path on the Wincontainer host; provide this or tarData, not both.")] string? tarPath = null,
        [Description("Base64-encoded .tar archive, maximum 512 MB decoded; provide this or tarPath, not both.")] string? tarData = null,
        IWslcDriver driver = null!,
        CancellationToken ct = default)
    {
        var hasPath = !string.IsNullOrWhiteSpace(tarPath);
        var hasData = !string.IsNullOrWhiteSpace(tarData);
        if (hasPath == hasData)
            return "Validation error: provide exactly one of tarPath or tarData.";

        return await driver.LoadImageAsync(tarPath, tarData, ct);
    }

    // ── Volumes ──────────────────────────────────────────────────────

    [McpServerTool, Description("List all storage volumes.")]
    public static async Task<string> ListVolumes(IWslcDriver driver, CancellationToken ct)
        => await driver.GetVolumesAsync(ct);

    [McpServerTool, Description("Create a new storage volume.")]
    public static async Task<string> CreateVolume(
        [Description("Volume name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.CreateVolumeAsync(name, ct);

    [McpServerTool, Description("Remove (delete) a storage volume by name.")]
    public static async Task<string> RemoveVolume(
        [Description("Volume name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RemoveVolumeAsync(name, ct);

    [McpServerTool, Description("Inspect a volume and return detailed information.")]
    public static async Task<string> InspectVolume(
        [Description("Volume name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.InspectVolumeAsync(name, ct);

    // ── Networks ─────────────────────────────────────────────────────

    [McpServerTool, Description("List all container networks.")]
    public static async Task<string> ListNetworks(IWslcDriver driver, CancellationToken ct)
        => await driver.GetNetworksAsync(ct);

    [McpServerTool, Description("Create a new container network.")]
    public static async Task<string> CreateNetwork(
        [Description("Network name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.CreateNetworkAsync(name, ct);

    [McpServerTool, Description("Remove (delete) a container network by name.")]
    public static async Task<string> RemoveNetwork(
        [Description("Network name")] string name,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.RemoveNetworkAsync(name, ct);

    // ── System ───────────────────────────────────────────────────────

    [McpServerTool, Description("Check whether the wslc runtime is available and healthy.")]
    public static async Task<string> HealthCheck(IWslcDriver driver, CancellationToken ct)
    {
        var available = await driver.IsAvailableAsync(ct);
        var version = await driver.GetVersionAsync(ct);
        return System.Text.Json.JsonSerializer.Serialize(new { ok = available, wslcVersion = version });
    }

    [McpServerTool, Description("Get the wslc runtime version string.")]
    public static async Task<string> GetVersion(IWslcDriver driver, CancellationToken ct)
        => await driver.GetVersionAsync(ct);
}
