using System.ComponentModel;
using ModelContextProtocol.Server;
using WinContainers.Runtime;

namespace WinContainers.Service.Mcp;

/// <summary>
/// MCP prompts that give AI clients ready-made instructions for working with the
/// Wincontainer runtime. Each prompt pulls live data from IWslcDriver and returns a
/// natural-language prompt body referencing that data.
/// </summary>
[McpServerPromptType]
public class WincontainerPrompts
{
    [McpServerPrompt, Description("Build a prompt that asks the model to summarize a single container from its inspect metadata.")]
    public static async Task<string> SummarizeContainer(
        [Description("Container id or name to summarize")] string id,
        IWslcDriver driver,
        CancellationToken ct)
    {
        var inspect = await driver.InspectContainerAsync(id, ct);
        return
            "Summarize the following Wincontainer container inspect output for the operator. " +
            "Cover what the container does, its current status, exposed ports, and any mounts. " +
            "Keep it concise and flag anything that looks misconfigured.\n\n" +
            $"Container id: {id}\n\n{inspect}";
    }

    [McpServerPrompt, Description("Build a prompt that asks the model to diagnose a failing or stopped container using inspect and logs.")]
    public static async Task<string> DiagnoseContainer(
        [Description("Container id or name to diagnose")] string id,
        IWslcDriver driver,
        CancellationToken ct)
    {
        var inspect = await driver.InspectContainerAsync(id, ct);
        var logs = await driver.GetContainerLogsAsync(id, 200, ct);
        return
            "Diagnose why the following Wincontainer container may be failing, crashing, or not " +
            "starting. Inspect the metadata and recent logs, identify the most likely root cause, " +
            "and suggest concrete remediation steps.\n\n" +
            $"Container id: {id}\n\nInspect:\n{inspect}\n\nRecent logs:\n{logs}";
    }

    [McpServerPrompt, Description("Build a prompt that asks the model to explain an image's origin and layers from its inspect metadata.")]
    public static async Task<string> ExplainImage(
        [Description("Image id or tag to explain")] string name,
        IWslcDriver driver,
        CancellationToken ct)
    {
        var inspect = await driver.InspectImageAsync(name, ct);
        return
            "Explain the following Wincontainer image inspect output. Describe what the image is, " +
            "its base/layers, size, and when it was created, and note anything a deployer should " +
            "watch for.\n\n" +
            $"Image: {name}\n\n{inspect}";
    }

    [McpServerPrompt, Description("Build a prompt that asks the model to summarize the whole runtime across containers, images, volumes, and networks.")]
    public static async Task<string> SummarizeRuntime(IWslcDriver driver, CancellationToken ct)
    {
        var containers = await driver.GetContainersAsync(ct);
        var images = await driver.GetImagesAsync(ct);
        var volumes = await driver.GetVolumesAsync(ct);
        var networks = await driver.GetNetworksAsync(ct);
        return
            "Summarize the current Wincontainer runtime state for the operator. Cover how many " +
            "containers are running vs stopped, which images are present, which volumes and " +
            "networks exist, and call out anything that needs attention.\n\n" +
            $"Containers:\n{containers}\n\nImages:\n{images}\n\nVolumes:\n{volumes}\n\nNetworks:\n{networks}";
    }
}
