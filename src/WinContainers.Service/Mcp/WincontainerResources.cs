using System.Text.Json;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using WinContainers.Runtime;

namespace WinContainers.Service.Mcp;

/// <summary>
/// MCP resources, resource templates, and the read handler that expose the live
/// Wincontainer runtime (wslc) to MCP clients. Registered in ServiceHost via the
/// WithListResourcesHandler / WithReadResourceHandler / WithListResourceTemplatesHandler
/// extension methods so the data is populated directly from IWslcDriver.
/// </summary>
public static class WincontainerResources
{
    private const string MimeType = "application/json";

    /// <summary>
    /// Lists one resource per live container, image, volume, and network so that
    /// MCP clients (e.g. Server Explorer) show populated Resources.
    /// </summary>
    public static async ValueTask<ListResourcesResult> ListResourcesAsync(
        RequestContext<ListResourcesRequestParams> context,
        CancellationToken cancellationToken)
    {
        var driver = ResolveDriver(context);
        var resources = new List<Resource>();

        var containersJson = await driver.GetContainersAsync(cancellationToken);
        foreach (var container in WslcContainerParser.ParseContainers(containersJson))
        {
            resources.Add(new Resource
            {
                Uri = $"container://{container.Id}",
                Name = string.IsNullOrWhiteSpace(container.Name) ? container.Id : container.Name,
                Description = $"Container status: {container.Status}",
                MimeType = MimeType
            });
        }

        var imagesJson = await driver.GetImagesAsync(cancellationToken);
        foreach (var image in WslcContainerParser.ParseImages(imagesJson))
        {
            var identifier = image.ID;
            if (!string.Equals(image.Repository, "(none)", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(image.Tag, "(none)", StringComparison.OrdinalIgnoreCase))
            {
                identifier = $"{image.Repository}:{image.Tag}";
            }

            resources.Add(new Resource
            {
                Uri = $"image://{identifier}",
                Name = identifier,
                Description = $"Image id: {image.ID}",
                MimeType = MimeType
            });
        }

        var volumesJson = await driver.GetVolumesAsync(cancellationToken);
        foreach (var volume in WslcResourceParser.ParseVolumes(volumesJson))
        {
            resources.Add(new Resource
            {
                Uri = $"volume://{volume.Name}",
                Name = volume.Name,
                Description = string.IsNullOrWhiteSpace(volume.Details) ? "Volume" : $"Volume: {volume.Details}",
                MimeType = MimeType
            });
        }

        var networksJson = await driver.GetNetworksAsync(cancellationToken);
        foreach (var network in WslcResourceParser.ParseNetworks(networksJson))
        {
            resources.Add(new Resource
            {
                Uri = $"network://{network.Name}",
                Name = network.Name,
                Description = string.IsNullOrWhiteSpace(network.Details) ? "Network" : $"Network: {network.Details}",
                MimeType = MimeType
            });
        }

        return new ListResourcesResult { Resources = resources };
    }

    /// <summary>
    /// Reads the contents of a single resource by URI. Dispatches by scheme to the
    /// matching IWslcDriver inspect method and returns the raw JSON.
    /// </summary>
    public static async ValueTask<ReadResourceResult> ReadResourceAsync(
        RequestContext<ReadResourceRequestParams> context,
        CancellationToken cancellationToken)
    {
        var driver = ResolveDriver(context);
        var uri = context.Params?.Uri ?? string.Empty;
        var (scheme, id) = SplitUri(uri);

        string json = scheme switch
        {
            "container" => await driver.InspectContainerAsync(id, cancellationToken),
            "image" => await driver.InspectImageAsync(id, cancellationToken),
            "volume" => await driver.InspectVolumeAsync(id, cancellationToken),
            "network" => await driver.GetNetworksAsync(cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported resource URI scheme: {uri}")
        };

        var contents = new List<ResourceContents>
        {
            new TextResourceContents
            {
                Uri = uri,
                MimeType = MimeType,
                Text = json
            }
        };

        return new ReadResourceResult { Contents = contents };
    }

    /// <summary>
    /// Advertises the fixed resource templates so MCP clients can show templates even
    /// before any runtime data exists.
    /// </summary>
    public static ValueTask<ListResourceTemplatesResult> ListResourceTemplatesAsync(
        RequestContext<ListResourceTemplatesRequestParams> context,
        CancellationToken cancellationToken)
    {
        var templates = new List<ResourceTemplate>
        {
            new ResourceTemplate
            {
                UriTemplate = "container://{id}",
                Name = "Container inspect",
                Description = "Inspect metadata for a container by its id or name.",
                MimeType = MimeType
            },
            new ResourceTemplate
            {
                UriTemplate = "image://{name}",
                Name = "Image inspect",
                Description = "Inspect metadata for an image by its id or tag.",
                MimeType = MimeType
            },
            new ResourceTemplate
            {
                UriTemplate = "volume://{name}",
                Name = "Volume inspect",
                Description = "Inspect metadata for a named volume.",
                MimeType = MimeType
            },
            new ResourceTemplate
            {
                UriTemplate = "network://{name}",
                Name = "Network inspect",
                Description = "Inspect metadata for a named network.",
                MimeType = MimeType
            }
        };

        return ValueTask.FromResult(new ListResourceTemplatesResult { ResourceTemplates = templates });
    }

    private static IWslcDriver ResolveDriver<TParams>(RequestContext<TParams> context)
        where TParams : class
    {
        var services = context.Server?.Services
            ?? throw new InvalidOperationException("No service provider available on the MCP request context.");
        return services.GetRequiredService<IWslcDriver>();
    }

    private static (string Scheme, string Id) SplitUri(string uri)
    {
        var separator = uri.IndexOf("://", StringComparison.Ordinal);
        if (separator < 0)
            return (string.Empty, uri);

        var scheme = uri[..separator];
        var id = uri[(separator + 3)..];
        return (scheme, id);
    }
}
