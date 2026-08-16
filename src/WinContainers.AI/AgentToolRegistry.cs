using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.AI;
using WinContainers.Runtime;

namespace WinContainers.AI;

/// <summary>
/// Metadata for one agent tool, including its safety classification.
/// </summary>
public sealed record AgentTool(AIFunction Function, bool Destructive);

/// <summary>
/// Builds the agent tool set over <see cref="IWslcDriver"/> and provides
/// preview text and safety classification for each tool.
/// </summary>
public sealed class AgentToolRegistry
{
    /// <summary>Tools whose effects cannot be easily undone. They require user confirmation.</summary>
    public static readonly IReadOnlySet<string> DestructiveToolNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "remove_container",
        "remove_image",
        "remove_volume",
        "remove_network",
    };

    private readonly List<AgentTool> _tools;

    public AgentToolRegistry(IWslcDriver driver, ComposeFileSaver compose)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(compose);

        var implementations = new ToolImplementations(driver, compose);
        var methods = typeof(ToolImplementations)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

        _tools = [];
        foreach (var method in methods)
        {
            var function = AIFunctionFactory.Create(method, implementations, new AIFunctionFactoryOptions
            {
                Name = method.Name,
            });
            _tools.Add(new AgentTool(function, DestructiveToolNames.Contains(method.Name)));
        }
    }

    /// <summary>All tools, as required by <see cref="ChatOptions.Tools"/>.</summary>
    public IReadOnlyList<AITool> Tools => _tools.Select(t => t.Function).ToList();

    public AgentTool? Find(string name) => _tools.FirstOrDefault(t => t.Function.Name == name);

    /// <summary>Builds a short human-readable description of an action.</summary>
    public static string BuildPreview(string name, IReadOnlyDictionary<string, object?> args)
    {
        string? Get(string key) => args.TryGetValue(key, out var value) ? value?.ToString() : null;

        return name switch
        {
            "list_containers" => "List all containers",
            "list_images" => "List all images",
            "list_volumes" => "List all volumes",
            "list_networks" => "List all networks",
            "inspect_container" => $"Inspect container '{Get("id")}'",
            "inspect_image" => $"Inspect image '{Get("id")}'",
            "inspect_volume" => $"Inspect volume '{Get("name")}'",
            "get_container_logs" => $"Get logs for container '{Get("id")}'",
            "start_container" => $"Start container '{Get("id")}'",
            "stop_container" => $"Stop container '{Get("id")}'",
            "restart_container" => $"Restart container '{Get("id")}'",
            "rename_container" => $"Rename container '{Get("id")}' to '{Get("name")}'",
            "run_container" => $"Run container from image '{Get("image")}'",
            "exec_command" => $"Run command in container '{Get("id")}': {Get("command")}",
            "pull_image" => $"Pull image '{Get("image")}'",
            "remove_image" => $"Remove image '{Get("id")}'",
            "create_volume" => $"Create volume '{Get("name")}'",
            "remove_volume" => $"Remove volume '{Get("name")}'",
            "create_network" => $"Create network '{Get("name")}'",
            "remove_network" => $"Remove network '{Get("name")}'",
            "remove_container" => $"Remove container '{Get("id")}'",
            "save_compose_file" => $"Save compose file '{Get("filename")}'",
            _ => name,
        };
    }
}

/// <summary>
/// The concrete tool implementations. Each public method becomes an agent tool;
/// descriptions come from <see cref="DescriptionAttribute"/> on the method and
/// its parameters. The method name is the tool name.
/// </summary>
public sealed class ToolImplementations
{
    private readonly IWslcDriver _driver;
    private readonly ComposeFileSaver _compose;

    public ToolImplementations(IWslcDriver driver, ComposeFileSaver compose)
    {
        _driver = driver;
        _compose = compose;
    }

    [Description("List all containers managed by the runtime. Returns JSON output from wslc.")]
    public async Task<string> list_containers(CancellationToken ct) => await _driver.GetContainersAsync(ct);

    [Description("Inspect a container and return detailed configuration and status information.")]
    public async Task<string> inspect_container([Description("Container ID or name")] string id, CancellationToken ct)
        => await _driver.InspectContainerAsync(id, ct);

    [Description("Get the recent logs of a container.")]
    public async Task<string> get_container_logs(
        [Description("Container ID or name")] string id,
        [Description("Number of log lines to return")] int tail = 200,
        CancellationToken ct = default)
        => await _driver.GetContainerLogsAsync(id, tail, ct);

    [Description("Start a stopped container by ID or name.")]
    public async Task<string> start_container([Description("Container ID or name")] string id, CancellationToken ct)
        => await _driver.StartContainerAsync(id, ct);

    [Description("Stop a running container by ID or name.")]
    public async Task<string> stop_container([Description("Container ID or name")] string id, CancellationToken ct)
        => await _driver.StopContainerAsync(id, ct);

    [Description("Restart a container by ID or name.")]
    public async Task<string> restart_container([Description("Container ID or name")] string id, CancellationToken ct)
        => await _driver.RestartContainerAsync(id, ct);

    [Description("Rename an existing container.")]
    public async Task<string> rename_container(
        [Description("Container ID or name")] string id,
        [Description("New container name")] string name,
        CancellationToken ct)
        => await _driver.RenameContainerAsync(id, name, ct);

    [Description("Run (create and start) a new container from an image, optionally attached to a named network.")]
    public async Task<string> run_container(
        [Description("Image name, e.g. 'nginx:latest' or 'myapp:1.0'")] string image,
        [Description("Optional container name")] string? name = null,
        [Description("Comma-separated port mappings, e.g. '80:80,8080:80'")] string? ports = null,
        [Description("Comma-separated volume mounts, e.g. '/host:/container,/data:/data'")] string? volumes = null,
        [Description("Comma-separated environment variables, e.g. 'KEY1=value1,KEY2=value2'")] string? env = null,
        [Description("Optional network name to attach the container to, e.g. 'famnet'")] string? network = null,
        CancellationToken ct = default)
    {
        var portList = ports?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        var volumeList = volumes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        var envList = env?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [];
        var result = await _driver.RunContainerAsync(image, name, portList, volumeList, envList, ct, network);
        if (!string.IsNullOrWhiteSpace(name)
            && !result.TrimStart().StartsWith("wslc error (", StringComparison.OrdinalIgnoreCase)
            && !result.TrimStart().StartsWith("Error:", StringComparison.OrdinalIgnoreCase))
        {
            ContainerConfigStore.SaveConfig(name!, new ContainerRunConfig
            {
                Image = image,
                Ports = portList,
                Volumes = volumeList,
                Env = envList,
                Network = network,
                AllowLocalNetworkAccess = false
            });
        }

        return result;
    }

    [Description("Execute a command inside a running container and return its output.")]
    public async Task<string> exec_command(
        [Description("Container ID or name")] string id,
        [Description("Command to run, e.g. 'ls -la'")] string command,
        CancellationToken ct)
        => await _driver.ExecCommandAsync(id, command, ct);

    [Description("Pull an image from a registry.")]
    public async Task<string> pull_image([Description("Image name, e.g. 'nginx:latest'")] string image, CancellationToken ct)
        => await _driver.PullImageAsync(image, ct);

    [Description("List all downloaded images.")]
    public async Task<string> list_images(CancellationToken ct) => await _driver.GetImagesAsync(ct);

    [Description("Inspect an image and return detailed metadata.")]
    public async Task<string> inspect_image([Description("Image ID or name")] string id, CancellationToken ct)
        => await _driver.InspectImageAsync(id, ct);

    [Description("Delete an image by ID or name. This is destructive and cannot be undone.")]
    public async Task<string> remove_image([Description("Image ID or name")] string id, CancellationToken ct)
        => await _driver.RemoveImageAsync(id, ct);

    [Description("List all storage volumes.")]
    public async Task<string> list_volumes(CancellationToken ct) => await _driver.GetVolumesAsync(ct);

    [Description("Create a new named volume.")]
    public async Task<string> create_volume([Description("Volume name")] string name, CancellationToken ct)
        => await _driver.CreateVolumeAsync(name, ct);

    [Description("Delete a volume by name. This is destructive and cannot be undone.")]
    public async Task<string> remove_volume([Description("Volume name")] string name, CancellationToken ct)
        => await _driver.RemoveVolumeAsync(name, ct);

    [Description("Inspect a volume and return detailed information.")]
    public async Task<string> inspect_volume([Description("Volume name")] string name, CancellationToken ct)
        => await _driver.InspectVolumeAsync(name, ct);

    [Description("List all container networks.")]
    public async Task<string> list_networks(CancellationToken ct) => await _driver.GetNetworksAsync(ct);

    [Description("Create a new container network.")]
    public async Task<string> create_network([Description("Network name")] string name, CancellationToken ct)
        => await _driver.CreateNetworkAsync(name, ct);

    [Description("Delete a network by name. This is destructive and cannot be undone.")]
    public async Task<string> remove_network([Description("Network name")] string name, CancellationToken ct)
        => await _driver.RemoveNetworkAsync(name, ct);

    [Description("Delete a container by ID or name. This is destructive and cannot be undone.")]
    public async Task<string> remove_container([Description("Container ID or name")] string id, CancellationToken ct)
        => await _driver.RemoveContainerAsync(id, ct);

    [Description("Save a docker-compose YAML file to disk and return the file path. Use this when the user asks for a multi-service setup or wants to keep a compose file.")]
    public async Task<string> save_compose_file(
        [Description("File name without extension, e.g. 'my-stack'")] string filename,
        [Description("The full docker-compose YAML content")] string yaml,
        CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        await Task.Yield();
        return _compose.Save(filename, yaml);
    }
}
