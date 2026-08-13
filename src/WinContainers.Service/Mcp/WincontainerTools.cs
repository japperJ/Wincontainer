using System.ComponentModel;
using System.Text.Json;
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
        object? Failure = null,
        bool RequiresConfirmation = false,
        string? OperationId = null,
        DateTimeOffset? ExpiresAtUtc = null,
        string? Message = null);

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
        object? failure = null,
        bool requiresConfirmation = false,
        string? operationId = null,
        DateTimeOffset? expiresAtUtc = null,
        string? message = null)
    {
        var payload = new ToolEnvelope(
            BuildSessionContext(),
            tool,
            success,
            result,
            guidance,
            validation,
            failure,
            requiresConfirmation,
            operationId,
            expiresAtUtc,
            message);
        return JsonSerializer.Serialize(payload, JsonOptions);
    }

    private static bool IsWslcError(string response) =>
        response.StartsWith("wslc error (", StringComparison.OrdinalIgnoreCase) ||
        response.StartsWith("Validation error:", StringComparison.OrdinalIgnoreCase);

    private static string WithSessionWarningPrefixIfNeeded(string tool, string response)
    {
        var session = BuildSessionContext();
        if (session.Warning is null)
            return response;

        return Wrap(tool, !IsWslcError(response), response, session.Warning);
    }

    private static bool TryHandleDestructiveConfirmation(
        string toolName,
        string canonicalArguments,
        bool confirm,
        string? operationId,
        out string response)
    {
        response = string.Empty;

        if (!McpDestructiveConfirmationPolicy.Enabled)
        {
            return true;
        }

        if (!(confirm && !string.IsNullOrWhiteSpace(operationId)))
        {
            var operation = McpDestructiveConfirmationPolicy.IssueOperation(toolName, canonicalArguments);
            response = Wrap(
                toolName,
                false,
                "Destructive action requires confirmation.",
                guidance: "Re-run with confirm=true and the returned operationId.",
                failure: new { reason = "confirmation_required" },
                requiresConfirmation: true,
                operationId: operation.OperationId,
                expiresAtUtc: operation.ExpiresAtUtc,
                message: "Destructive action requires confirmation. Re-run with confirm=true and the returned operationId.");
            return false;
        }

        if (McpDestructiveConfirmationPolicy.TryConsume(toolName, operationId!, canonicalArguments, out var rejectReason))
        {
            return true;
        }

        response = Wrap(
            toolName,
            false,
            "Blocked destructive confirmation.",
            guidance: "Use a fresh confirmation flow for the same tool and arguments.",
            failure: new { reason = rejectReason },
            message: $"Destructive confirmation rejected: {rejectReason}.");
        return false;
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

        var target = string.IsNullOrWhiteSpace(name) ? image : name!;
        var inspectResult = await driver.InspectContainerAsync(target, ct);
        var logsResult = await driver.GetContainerLogsAsync(target, 120, ct);
        var reachable = "unknown";
        if (!string.IsNullOrWhiteSpace(name))
        {
            var health = await driver.ExecCommandAsync(name!, "wget -qO- http://127.0.0.1/ || curl -fsS http://127.0.0.1/", ct);
            reachable = IsWslcError(health) ? "unreachable" : "reachable";
        }

        var validation = new
        {
            containerState = inspectResult,
            portMapping = inspectResult,
            httpHealth = reachable
        };

        if (inspectResult.Contains("\"Running\":false", StringComparison.OrdinalIgnoreCase) || reachable == "unreachable")
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

    [McpServerTool, Description("Remove (delete) a container by ID or name.")]
    public static async Task<string> RemoveContainer(
        [Description("Container ID or name")] string id,
        IWslcDriver driver,
        CancellationToken ct,
        [Description("Set true to confirm destructive action for DB-related resources.")] bool confirmDestructive = false,
        [Description("Set true to confirm the destructive action after a prior round-trip request.")] bool confirm = false,
        [Description("Confirmation operationId returned by the prior destructive call.")] string? operationId = null)
    {
        if (id.Contains("db", StringComparison.OrdinalIgnoreCase) && !confirmDestructive)
        {
            return Wrap(
                "remove_container",
                false,
                "Blocked destructive action on DB-related resource.",
                guidance: "Re-run with confirmDestructive=true after you confirm this is intended.");
        }

        var canonicalArguments = McpDestructiveConfirmationPolicy.CanonicalizeArguments(id);
        if (!TryHandleDestructiveConfirmation("remove_container", canonicalArguments, confirm, operationId, out var confirmationResponse))
        {
            return confirmationResponse;
        }

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

    [McpServerTool, Description("Remove (delete) a downloaded image by ID or tag.")]
    public static async Task<string> RemoveImage(
        [Description("Image ID or tag")] string id,
        IWslcDriver driver,
        CancellationToken ct,
        [Description("Set true to confirm the destructive action after a prior round-trip request.")] bool confirm = false,
        [Description("Confirmation operationId returned by the prior destructive call.")] string? operationId = null)
    {
        var canonicalArguments = McpDestructiveConfirmationPolicy.CanonicalizeArguments(id);
        if (!TryHandleDestructiveConfirmation("remove_image", canonicalArguments, confirm, operationId, out var confirmationResponse))
        {
            return confirmationResponse;
        }

        return await driver.RemoveImageAsync(id, ct);
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

    [McpServerTool, Description("Redeploy only the web container. Keeps DB container, network, and app data unchanged.")]
    public static async Task<string> RedeployWebOnly(
        [Description("Web container id or name")] string webContainerId,
        [Description("Replacement image for web container")] string image,
        [Description("Optional container name")] string? name,
        [Description("Comma-separated port mappings, e.g. '80:80,8080:80/tcp'")] string? ports,
        [Description("Comma-separated volume mounts, e.g. '/host:/container,/data:/data'")] string? volumes,
        [Description("Comma-separated environment variables, e.g. 'KEY1=val1,KEY2=val2'")] string? env,
        [Description("Optional network name to attach the container to, e.g. 'famnet'")] string? network,
        IWslcDriver driver,
        CancellationToken ct,
        [Description("Set true to confirm the destructive action after a prior round-trip request.")] bool confirm = false,
        [Description("Confirmation operationId returned by the prior destructive call.")] string? operationId = null)
    {
        var canonicalArguments = McpDestructiveConfirmationPolicy.CanonicalizeArguments(webContainerId, image, name ?? string.Empty, ports ?? string.Empty, volumes ?? string.Empty, env ?? string.Empty, network ?? string.Empty);
        if (!TryHandleDestructiveConfirmation("redeploy_web_only", canonicalArguments, confirm, operationId, out var confirmationResponse))
        {
            return confirmationResponse;
        }

        var stop = await driver.StopContainerAsync(webContainerId, ct);
        if (IsWslcError(stop))
            return Wrap("redeploy_web_only", false, stop);

        var remove = await driver.RemoveContainerAsync(webContainerId, ct);
        if (IsWslcError(remove))
            return Wrap("redeploy_web_only", false, remove);

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

    [McpServerTool, Description("Remove (delete) a storage volume by name.")]
    public static async Task<string> RemoveVolume(
        [Description("Volume name")] string name,
        IWslcDriver driver,
        CancellationToken ct,
        [Description("Set true to confirm destructive action for DB-related resources.")] bool confirmDestructive = false,
        [Description("Set true to confirm the destructive action after a prior round-trip request.")] bool confirm = false,
        [Description("Confirmation operationId returned by the prior destructive call.")] string? operationId = null)
    {
        if (name.Contains("db", StringComparison.OrdinalIgnoreCase) && !confirmDestructive)
        {
            return Wrap(
                "remove_volume",
                false,
                "Blocked destructive action on DB-related resource.",
                guidance: "Re-run with confirmDestructive=true after you confirm this is intended.");
        }

        var canonicalArguments = McpDestructiveConfirmationPolicy.CanonicalizeArguments(name);
        if (!TryHandleDestructiveConfirmation("remove_volume", canonicalArguments, confirm, operationId, out var confirmationResponse))
        {
            return confirmationResponse;
        }

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

    [McpServerTool, Description("Remove (delete) a container network by name.")]
    public static async Task<string> RemoveNetwork(
        [Description("Network name")] string name,
        IWslcDriver driver,
        CancellationToken ct,
        [Description("Set true to confirm the destructive action after a prior round-trip request.")] bool confirm = false,
        [Description("Confirmation operationId returned by the prior destructive call.")] string? operationId = null)
    {
        var canonicalArguments = McpDestructiveConfirmationPolicy.CanonicalizeArguments(name);
        if (!TryHandleDestructiveConfirmation("remove_network", canonicalArguments, confirm, operationId, out var confirmationResponse))
        {
            return confirmationResponse;
        }

        return await driver.RemoveNetworkAsync(name, ct);
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
