using System.Text;
using Microsoft.Extensions.AI;
using WinContainers.Runtime;

namespace WinContainers.AI;

/// <summary>
/// Builds a compact snapshot of the current container and image state for
/// injection into the agent's system prompt.
/// </summary>
public sealed class ContainerSnapshotBuilder
{
    private readonly IWslcDriver _driver;

    public ContainerSnapshotBuilder(IWslcDriver driver)
    {
        ArgumentNullException.ThrowIfNull(driver);
        _driver = driver;
    }

    public async Task<string> BuildAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();

        try
        {
            var containers = WslcContainerParser.ParseContainers(await _driver.GetContainersAsync(ct));
            sb.AppendLine(containers.Count == 0
                ? "- Containers: none"
                : $"- Containers ({containers.Count}):");
            foreach (var container in containers.Take(40))
            {
                sb.AppendLine($"  - {container.Name} | {container.Status} | image {container.Image}" +
                    (string.IsNullOrWhiteSpace(container.Ports) ? string.Empty : $" | ports {container.Ports}"));
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"- Containers: unavailable ({ex.Message})");
        }

        try
        {
            var images = WslcContainerParser.ParseImages(await _driver.GetImagesAsync(ct));
            sb.AppendLine(images.Count == 0
                ? "- Images: none"
                : $"- Images ({images.Count}):");
            foreach (var image in images.Take(40))
            {
                sb.AppendLine($"  - {image.FullTag}");
            }
        }
        catch (Exception ex)
        {
            sb.AppendLine($"- Images: unavailable ({ex.Message})");
        }

        return sb.ToString();
    }
}
