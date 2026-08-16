namespace WinContainers.Runtime;

public sealed record ContainerAccessResult(
    bool Success,
    string Message,
    bool AllowLocalNetworkAccess,
    IReadOnlyList<string> Ports)
{
    public static ContainerAccessResult Failure(string message, bool allowLocalNetworkAccess = false) =>
        new(false, message, allowLocalNetworkAccess, Array.Empty<string>());
}

public sealed class ContainerAccessService
{
    private readonly IWslcDriver _driver;
    private readonly Func<string, ContainerRunConfig?> _loadConfig;
    private readonly Action<string, ContainerRunConfig> _saveConfig;

    public ContainerAccessService(
        IWslcDriver driver,
        Func<string, ContainerRunConfig?>? loadConfig = null,
        Action<string, ContainerRunConfig>? saveConfig = null)
    {
        _driver = driver;
        _loadConfig = loadConfig ?? ContainerConfigStore.LoadConfig;
        _saveConfig = saveConfig ?? ContainerConfigStore.SaveConfig;
    }

    public async Task<ContainerAccessResult> SetAccessAsync(
        string containerId,
        bool allowLocalNetworkAccess,
        string? containerName = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(containerId))
            return ContainerAccessResult.Failure("A container ID is required.");

        var configKey = string.IsNullOrWhiteSpace(containerName) ? containerId : containerName;
        var config = _loadConfig(configKey);
        if (config is null && !string.Equals(configKey, containerId, StringComparison.Ordinal))
            config = _loadConfig(containerId);

        if (config is null)
            return ContainerAccessResult.Failure("Saved container configuration is unavailable.");

        if (string.IsNullOrWhiteSpace(config.Image))
            return ContainerAccessResult.Failure("Saved container configuration has no image.");

        if (config.Ports.Count == 0)
            return ContainerAccessResult.Failure("No published ports are saved for this container.");

        var conversion = PortBindingConverter.Convert(config.Ports, allowLocalNetworkAccess);
        if (!conversion.Success)
            return ContainerAccessResult.Failure(conversion.Error ?? "Published port validation failed.");

        var runName = string.IsNullOrWhiteSpace(containerName) ? configKey : containerName;
        var stopResult = await _driver.StopContainerAsync(containerId, ct);
        if (IsError(stopResult))
            return ContainerAccessResult.Failure($"Failed to stop container: {stopResult}", config.AllowLocalNetworkAccess);

        var removeResult = await _driver.RemoveContainerAsync(containerId, ct);
        if (IsError(removeResult))
        {
            return ContainerAccessResult.Failure(
                $"Failed to remove container: {removeResult}",
                config.AllowLocalNetworkAccess);
        }

        var runResult = await _driver.RunContainerAsync(
            config.Image,
            runName,
            conversion.Bindings,
            config.Volumes,
            config.Env,
            ct,
            config.Network);
        if (IsError(runResult))
        {
            return ContainerAccessResult.Failure(
                $"Container recreation failed after removal; recovery may be required: {runResult}",
                config.AllowLocalNetworkAccess);
        }

        var updatedConfig = config with
        {
            Ports = conversion.Bindings.ToList(),
            AllowLocalNetworkAccess = allowLocalNetworkAccess
        };
        _saveConfig(runName, updatedConfig);

        return new ContainerAccessResult(
            true,
            allowLocalNetworkAccess
                ? "Container recreated with local-network access enabled."
                : "Container recreated with local-only access.",
            allowLocalNetworkAccess,
            conversion.Bindings);
    }

    private static bool IsError(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return false;

        var value = output.Trim();
        return value.StartsWith("wslc error (", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("Validation error:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("Error response", StringComparison.OrdinalIgnoreCase);
    }
}
