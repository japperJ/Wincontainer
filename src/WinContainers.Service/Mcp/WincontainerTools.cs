using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
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
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private sealed record SessionContextPayload(
        string SessionId,
        string SessionName,
        bool VisibleInUi,
        bool AdminSession,
        string? Warning = null);

    private sealed record ToolEnvelope(
        SessionContextPayload Session,
        string Tool,
        bool Success,
        string? Result = null,
        string? Guidance = null,
        object? Validation = null,
        object? Failure = null);

    private static SessionContextPayload BuildSessionContext()
    {
        var sessionId = Environment.GetEnvironmentVariable("COPILOT_SESSION_ID") ?? "unknown";
        var sessionName = Environment.GetEnvironmentVariable("COPILOT_SESSION_NAME") ?? "unknown";
        var isAdmin = string.Equals(Environment.GetEnvironmentVariable("WINCONTAINER_ADMIN_SESSION"), "true", StringComparison.OrdinalIgnoreCase);
        var isVisible = !string.Equals(Environment.GetEnvironmentVariable("WINCONTAINER_HIDDEN_SESSION"), "true", StringComparison.OrdinalIgnoreCase);
        var warning = !isVisible && !isAdmin
            ? "Warning: target session is hidden and non-admin. Deploy actions can be easy to miss in this session."
            : null;
        return new SessionContextPayload(sessionId, sessionName, isVisible, isAdmin, warning);
    }

    private static string Wrap(
        string tool,
        bool success,
        string? result = null,
        string? guidance = null,
        object? validation = null,
        object? failure = null)
    {
        var payload = new ToolEnvelope(
            BuildSessionContext(),
            tool,
            success,
            result,
            guidance,
            validation,
            failure);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static bool IsWslcError(string response) =>
        response.StartsWith("wslc error (", StringComparison.OrdinalIgnoreCase) ||
        response.StartsWith("Validation error:", StringComparison.OrdinalIgnoreCase);

    private static string SafeDisplayValue(string value)
    {
        var sanitized = new string(value.Where(character => !char.IsControl(character)).ToArray()).Trim();
        if (sanitized.Length == 0)
        {
            return "(not specified)";
        }

        return sanitized.Length <= 128 ? sanitized : sanitized[..128] + "…";
    }

    private static int CountDelimitedValues(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? 0
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Length;

    private static string BuildRedeployDisplaySummary(
        string webContainerId,
        string image,
        string? name,
        string? ports,
        string? volumes,
        string? env,
        string? network)
    {
        var environmentState = CountDelimitedValues(env) > 0 ? "supplied" : "not supplied";
        var nameState = string.IsNullOrWhiteSpace(name) ? "not supplied" : "supplied";
        var networkState = string.IsNullOrWhiteSpace(network) ? "not supplied" : "supplied";

        return
            $"Redeploy web container '{SafeDisplayValue(webContainerId)}' with replacement image '{SafeDisplayValue(image)}' " +
            $"(ports: {CountDelimitedValues(ports)}; volumes: {CountDelimitedValues(volumes)}; " +
            $"environment {environmentState}; name {nameState}; network {networkState}).";
    }

    private static string WithSessionWarningPrefixIfNeeded(string tool, string response)
    {
        var session = BuildSessionContext();
        if (session.Warning is null)
            return Wrap(tool, !IsWslcError(response), response);

        return Wrap(tool, !IsWslcError(response), response, guidance: session.Warning);
    }

    private static async Task<(bool Allowed, string? Response)> RequestHumanApprovalAsync(
        string toolName,
        string displaySummary,
        McpServer server,
        CancellationToken ct)
    {
        if (!McpDestructiveConfirmationPolicy.Enabled)
        {
            return (true, null);
        }

        if (server.ClientCapabilities?.Elicitation?.Form is null)
        {
            return (false, Wrap(
                toolName,
                false,
                "Destructive action is blocked because the MCP client does not support elicitation.",
                guidance: "Run this action from an MCP client that supports in-request human elicitation.",
                failure: new { reason = "elicitation_unsupported" }));
        }

        try
        {
            var session = BuildSessionContext();
            var message =
                $"{displaySummary}\n\n" +
                $"Session: {session.SessionName} ({(session.VisibleInUi ? "visible" : "hidden")}, " +
                $"{(session.AdminSession ? "administrator" : "non-administrator")})\n" +
                $"Session ID: {session.SessionId}";

            if (!string.IsNullOrWhiteSpace(session.Warning))
            {
                message += $"\n\n{session.Warning}";
            }

            var result = await server.ElicitAsync(
                new ElicitRequestParams
                {
                    Message = message,
                    RequestedSchema = new ElicitRequestParams.RequestSchema
                    {
                        Properties =
                        {
                            ["Allow"] = new ElicitRequestParams.UntitledSingleSelectEnumSchema
                            {
                                Enum = ["allow", "deny"]
                            }
                        },
                        Required = ["Allow"]
                    }
                },
                ct);
            ct.ThrowIfCancellationRequested();

            if (string.Equals(result.Action, "accept", StringComparison.Ordinal) &&
                result.Content is not null &&
                result.Content.TryGetValue("Allow", out var allowValue) &&
                allowValue.ValueKind == JsonValueKind.String &&
                string.Equals(allowValue.GetString(), "allow", StringComparison.Ordinal))
            {
                return (true, null);
            }

            return (false, Wrap(
                toolName,
                false,
                "Destructive action was not approved.",
                failure: new { reason = "human_approval_denied" }));
        }
        catch (InvalidOperationException) when (!ct.IsCancellationRequested)
        {
            return (false, Wrap(
                toolName,
                false,
                "Destructive action is blocked because human elicitation failed.",
                guidance: "Run this action from an MCP client that supports in-request human elicitation.",
                failure: new { reason = "elicitation_unavailable" }));
        }
        catch (McpException) when (!ct.IsCancellationRequested)
        {
            return (false, Wrap(
                toolName,
                false,
                "Destructive action is blocked because human elicitation failed.",
                guidance: "Run this action from an MCP client that supports in-request human elicitation.",
                failure: new { reason = "elicitation_unavailable" }));
        }
    }
    // ── Container lifecycle ──────────────────────────────────────────

    [McpServerTool, Description("List all containers managed by the runtime. Returns JSON output from wslc.")]
    public static async Task<string> ListContainers(IWslcDriver driver, CancellationToken ct)
        => await driver.GetContainersAsync(ct);

    [McpServerTool, Description("Run (create + start) a new container from an image, optionally attached to a named network.")]
    public static async Task<string> RunContainer(
        [Description("Image name, e.g. 'ubuntu:latest' or 'myapp:1.0'")] string image,
        [Description("Optional container name")] string? name = null,
        [Description("Comma-separated port mappings, e.g. '80:80,8080:80/tcp'")] string? ports = null,
        [Description("Comma-separated volume mounts, e.g. '/host:/container,/data:/data'")] string? volumes = null,
        [Description("Comma-separated environment variables, e.g. 'KEY1=val1,KEY2=val2'")] string? env = null,
        [Description("Optional network name to attach the container to, e.g. 'famnet'")] string? network = null,
        IWslcDriver driver = null!,
        CancellationToken ct = default)
    {
        var volumeList = volumes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var runResult = await driver.RunContainerAsync(
            image, name,
            ports?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            volumeList,
            env?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ct,
            network);

        if (IsWslcError(runResult))
        {
            var errorText = runResult;
            if (runResult.Contains("mount", StringComparison.OrdinalIgnoreCase) && runResult.Contains("limit", StringComparison.OrdinalIgnoreCase))
                errorText += " Guidance: host mount limit reached. Remove unused bind mounts or switch to named volumes.";
            if (runResult.Contains("image", StringComparison.OrdinalIgnoreCase) && runResult.Contains("not found", StringComparison.OrdinalIgnoreCase))
                errorText += " Guidance: image can be stale or missing. Pull or load the image again, then redeploy.";
            return WithSessionWarningPrefixIfNeeded("run_container", errorText);
        }

        // Persist the recreation data immediately after WSLC creates the container.
        // The optional in-container health probe can fail when the image has no wget or curl.
        if (!string.IsNullOrWhiteSpace(name))
        {
            ContainerConfigStore.SaveConfig(name!, new ContainerRunConfig
            {
                Image = image,
                Ports = ports?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [],
                Volumes = volumeList?.ToList() ?? [],
                Env = env?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList() ?? [],
                Network = network,
                AllowLocalNetworkAccess = false
            });
        }

        var target = string.IsNullOrWhiteSpace(name) ? image : name!;
        var inspectResult = await driver.InspectContainerAsync(target, ct);
        var logsResult = await driver.GetContainerLogsAsync(target, 120, ct);
        var reachable = "unknown";
        if (!string.IsNullOrWhiteSpace(name))
        {
            var health = await driver.ExecCommandAsync(name!, "wget -qO- http://127.0.0.1/ || curl -fsS http://127.0.0.1/", ct);
            reachable = IsWslcError(health) ? "unavailable" : "reachable";
        }

        var validation = new
        {
            containerState = inspectResult,
            portMapping = inspectResult,
            httpHealth = reachable
        };

        if (inspectResult.Contains("\"Running\":false", StringComparison.OrdinalIgnoreCase))
        {
            var failure = new
            {
                reason = "Container failed startup validation.",
                containerToImageMapping = inspectResult,
                finalLogs = logsResult
            };
            return Wrap("run_container", false, runResult, validation: validation, failure: failure);
        }

        return Wrap("run_container", true, runResult, validation: validation);
    }

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

    [McpServerTool, Description("Remove (delete) a container by ID or name. DESTRUCTIVE: requires an in-request human Allow/Deny elicitation before execution.")]
    public static async Task<string> RemoveContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        McpServer server,
        CancellationToken ct,
        [Description("Set true to confirm destructive action for DB-related resources.")] bool confirmDestructive = false)
    {
        if (id.Contains("db", StringComparison.OrdinalIgnoreCase) && !confirmDestructive)
        {
            return Wrap(
                "remove_container",
                false,
                "Blocked destructive action on DB-related resource.",
                guidance: "Re-run with confirmDestructive=true after you confirm this is intended.");
        }

        var confirmation = await RequestHumanApprovalAsync(
            "remove_container",
            $"Remove container '{SafeDisplayValue(id)}'.",
            server,
            ct);
        if (!confirmation.Allowed)
        {
            return confirmation.Response!;
        }

        ct.ThrowIfCancellationRequested();
        var result = await driver.RemoveContainerAsync(id, ct);
        return WithSessionWarningPrefixIfNeeded("remove_container", result);
    }

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

    [McpServerTool, Description("Remove (delete) a downloaded image by ID or tag. DESTRUCTIVE: requires an in-request human Allow/Deny elicitation before execution.")]
    public static async Task<string> RemoveImage(
        [Description("Image ID or tag")] string id,
        IWslcDriver driver,
        McpServer server,
        CancellationToken ct)
    {
        var confirmation = await RequestHumanApprovalAsync(
            "remove_image",
            $"Remove image '{SafeDisplayValue(id)}'.",
            server,
            ct);
        if (!confirmation.Allowed)
        {
            return confirmation.Response!;
        }

        ct.ThrowIfCancellationRequested();
        var result = await driver.RemoveImageAsync(id, ct);
        return WithSessionWarningPrefixIfNeeded("remove_image", result);
    }

    [McpServerTool, Description("Inspect an image and return detailed metadata.")]
    public static async Task<string> InspectImage(
        [Description("Image ID or tag")] string id,
        IWslcDriver driver,
        CancellationToken ct)
        => await driver.InspectImageAsync(id, ct);

    [McpServerTool, Description("Start a chunked image tar upload. Returns JSON metadata containing the uploadId property.")]
    public static string StartImageUpload(ImageUploadStore store)
    {
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        return JsonSerializer.Serialize(store.Start(), options);
    }

    [McpServerTool, Description("Append the next base64 chunk to an image tar upload. Chunks must be ordered and no larger than 3 KB decoded.")]
    public static Task<string> UploadImageChunk(
        [Description("The uploadId property from the JSON returned by start_image_upload")] string uploadId,
        [Description("Zero-based chunk sequence number")] int sequence,
        [Description("Base64 data for one tar chunk, maximum 3 KB decoded")] string base64Chunk,
        ImageUploadStore store,
        CancellationToken ct) =>
        store.AppendChunkAsync(uploadId, sequence, base64Chunk, ct);

    [McpServerTool, Description("Finish a chunked image tar upload and load it into WSLC.")]
    public static Task<string> FinishImageUpload(
        [Description("The uploadId property from the JSON returned by start_image_upload")] string uploadId,
        ImageUploadStore store,
        IWslcDriver driver,
        CancellationToken ct) =>
        store.CompleteAsync(uploadId, (path, token) => driver.LoadImageAsync(path, null, token), ct);

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

        var guidance = "Use tar save/load for portable deploy flow. Avoid bind mounts in admin session; prefer named volumes.";
        var result = await driver.LoadImageAsync(tarPath, tarData, ct);
        if (IsWslcError(result) && result.Contains("image", StringComparison.OrdinalIgnoreCase))
        {
            result += " Guidance: image metadata looks stale. Re-export the tar from source and load again.";
        }

        return Wrap("load_image", !IsWslcError(result), result, guidance: guidance);
    }

    [McpServerTool, Description("Redeploy only the web container. Keeps DB container, network, and app data unchanged. DESTRUCTIVE: requires an in-request human Allow/Deny elicitation before execution.")]
    public static async Task<string> RedeployWebOnly(
        [Description("Web container id or name")] string webContainerId,
        [Description("Replacement image for web container")] string image,
        [Description("Optional container name")] string? name,
        [Description("Comma-separated port mappings, e.g. '80:80,8080:80/tcp'")] string? ports,
        [Description("Comma-separated volume mounts, e.g. '/host:/container,/data:/data'")] string? volumes,
        [Description("Comma-separated environment variables, e.g. 'KEY1=val1,KEY2=val2'")] string? env,
        [Description("Optional network name to attach the container to, e.g. 'famnet'")] string? network,
        IWslcDriver driver,
        McpServer server,
        CancellationToken ct)
    {
        var confirmation = await RequestHumanApprovalAsync(
            "redeploy_web_only",
            BuildRedeployDisplaySummary(webContainerId, image, name, ports, volumes, env, network),
            server,
            ct);
        if (!confirmation.Allowed)
        {
            return confirmation.Response!;
        }

        ct.ThrowIfCancellationRequested();
        var stop = await driver.StopContainerAsync(webContainerId, ct);
        if (IsWslcError(stop))
            return Wrap("redeploy_web_only", false, stop);

        ct.ThrowIfCancellationRequested();
        var remove = await driver.RemoveContainerAsync(webContainerId, ct);
        if (IsWslcError(remove))
            return Wrap("redeploy_web_only", false, remove);

        ct.ThrowIfCancellationRequested();
        var run = await driver.RunContainerAsync(
            image,
            name,
            ports?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            volumes?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            env?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            ct,
            network);

        return WithSessionWarningPrefixIfNeeded("redeploy_web_only", run);
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

    [McpServerTool, Description("Remove (delete) a storage volume by name. DESTRUCTIVE: requires an in-request human Allow/Deny elicitation before execution.")]
    public static async Task<string> RemoveVolume(
        [Description("Volume name")] string name,
        IWslcDriver driver,
        McpServer server,
        CancellationToken ct,
        [Description("Set true to confirm destructive action for DB-related resources.")] bool confirmDestructive = false)
    {
        if (name.Contains("db", StringComparison.OrdinalIgnoreCase) && !confirmDestructive)
        {
            return Wrap(
                "remove_volume",
                false,
                "Blocked destructive action on DB-related resource.",
                guidance: "Re-run with confirmDestructive=true after you confirm this is intended.");
        }

        var confirmation = await RequestHumanApprovalAsync(
            "remove_volume",
            $"Remove volume '{SafeDisplayValue(name)}'.",
            server,
            ct);
        if (!confirmation.Allowed)
        {
            return confirmation.Response!;
        }

        ct.ThrowIfCancellationRequested();
        var result = await driver.RemoveVolumeAsync(name, ct);
        return WithSessionWarningPrefixIfNeeded("remove_volume", result);
    }

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

    [McpServerTool, Description("Remove (delete) a container network by name. DESTRUCTIVE: requires an in-request human Allow/Deny elicitation before execution.")]
    public static async Task<string> RemoveNetwork(
        [Description("Network name")] string name,
        IWslcDriver driver,
        McpServer server,
        CancellationToken ct)
    {
        var confirmation = await RequestHumanApprovalAsync(
            "remove_network",
            $"Remove network '{SafeDisplayValue(name)}'.",
            server,
            ct);
        if (!confirmation.Allowed)
        {
            return confirmation.Response!;
        }

        ct.ThrowIfCancellationRequested();
        var result = await driver.RemoveNetworkAsync(name, ct);
        return WithSessionWarningPrefixIfNeeded("remove_network", result);
    }

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
