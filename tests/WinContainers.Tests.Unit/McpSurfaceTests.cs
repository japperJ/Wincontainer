using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using WinContainers.Runtime;
using WinContainers.Tests.Unit.Ai;

namespace WinContainers.Tests.Unit;

/// <summary>
/// Exercises the MCP resource and prompt surfaces with a fully scripted driver so
/// we can verify they expose live Wincontainer data without a running wslc process.
/// </summary>
public class McpSurfaceTests
{
    private sealed class ScriptedDriver : IWslcDriver
    {
        private readonly FakeDriver _inner = new();
        public string ContainerInspect { get; set; } = "{}";
        public string ImageInspect { get; set; } = "{}";

        public string ContainersJson { get => _inner.ContainersJson; set => _inner.ContainersJson = value; }
        public string ImagesJson { get => _inner.ImagesJson; set => _inner.ImagesJson = value; }
        public string VolumesJson { get => _inner.VolumesJson; set => _inner.VolumesJson = value; }
        public string NetworksJson { get => _inner.NetworksJson; set => _inner.NetworksJson = value; }

        public Task<bool> IsAvailableAsync(CancellationToken ct) => _inner.IsAvailableAsync(ct);
        public Task<string> GetVersionAsync(CancellationToken ct) => _inner.GetVersionAsync(ct);
        public Task<string> GetContainersAsync(CancellationToken ct) => _inner.GetContainersAsync(ct);
        public Task<string> StartContainerAsync(string id, CancellationToken ct) => _inner.StartContainerAsync(id, ct);
        public Task<string> StopContainerAsync(string id, CancellationToken ct) => _inner.StopContainerAsync(id, ct);
        public Task<string> RestartContainerAsync(string id, CancellationToken ct) => _inner.RestartContainerAsync(id, ct);
        public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) => _inner.RenameContainerAsync(id, name, ct);
        public Task<string> RemoveContainerAsync(string id, CancellationToken ct) => _inner.RemoveContainerAsync(id, ct);
        public Task<string> InspectContainerAsync(string id, CancellationToken ct) => Task.FromResult(ContainerInspect);
        public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) => _inner.GetContainerLogsAsync(id, tail, ct);
        public Task<string> GetImagesAsync(CancellationToken ct) => _inner.GetImagesAsync(ct);
        public Task<string> PullImageAsync(string image, CancellationToken ct) => _inner.PullImageAsync(image, ct);
        public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct) => _inner.LoadImageAsync(tarPath, tarData, ct);
        public Task<string> RemoveImageAsync(string id, CancellationToken ct) => _inner.RemoveImageAsync(id, ct);
        public Task<string> InspectImageAsync(string id, CancellationToken ct) => Task.FromResult(ImageInspect);
        public Task<string> GetVolumesAsync(CancellationToken ct) => _inner.GetVolumesAsync(ct);
        public Task<string> CreateVolumeAsync(string name, CancellationToken ct) => _inner.CreateVolumeAsync(name, ct);
        public Task<string> RemoveVolumeAsync(string name, CancellationToken ct) => _inner.RemoveVolumeAsync(name, ct);
        public Task<string> InspectVolumeAsync(string name, CancellationToken ct) => _inner.InspectVolumeAsync(name, ct);
        public Task<string> GetNetworksAsync(CancellationToken ct) => _inner.GetNetworksAsync(ct);
        public Task<string> CreateNetworkAsync(string name, CancellationToken ct) => _inner.CreateNetworkAsync(name, ct);
        public Task<string> RemoveNetworkAsync(string name, CancellationToken ct) => _inner.RemoveNetworkAsync(name, ct);
        public Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, CancellationToken ct = default, string? network = null)
            => _inner.RunContainerAsync(image, name, ports, volumes, env, ct, network);
        public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default) => _inner.ExecCommandAsync(id, command, ct);
        public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default) => _inner.ExecShellAsync(id, shellCommand, shell, ct);
    }

    private static FakeMcpServer CreateServer(IWslcDriver driver)
    {
        var services = new ServiceCollection();
        services.AddSingleton<IWslcDriver>(driver);
        return new FakeMcpServer(services.BuildServiceProvider());
    }

    private static RequestContext<TParams> CreateContext<TParams>(McpServer server, TParams parameters)
        where TParams : class
    {
        var jsonRpcRequest = new JsonRpcRequest { Method = "test" };
        var ctors = typeof(RequestContext<TParams>).GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var ctor = ctors.FirstOrDefault(c => c.GetParameters().Length == 3)
            ?? ctors.First(c => c.GetParameters().Length == 2);
        var args = ctor.GetParameters().Length == 3
            ? new object[] { server, jsonRpcRequest, parameters }
            : new object[] { server, jsonRpcRequest };
        var context = (RequestContext<TParams>)ctor.Invoke(args);
        typeof(RequestContext<TParams>).GetProperty("Server")!.SetValue(context, server);
        typeof(RequestContext<TParams>).GetProperty("Params")!.SetValue(context, parameters);
        return context;
    }

    [Fact]
    public async Task ListResources_ReturnsOneEntryPerLiveObject()
    {
        var driver = new ScriptedDriver
        {
            ContainersJson = "[{\"Id\":\"abc123\",\"Names\":\"web\",\"Status\":\"Running\"}]",
            ImagesJson = "[{\"Repository\":\"nginx\",\"Tag\":\"latest\",\"ID\":\"img1\"}]",
            VolumesJson = "[{\"Name\":\"data\",\"Details\":\"local\"}]",
            NetworksJson = "[{\"Name\":\"bridge\",\"Details\":\"nat\"}]"
        };
        var server = CreateServer(driver);

        var result = await WinContainers.Service.Mcp.WincontainerResources.ListResourcesAsync(
            CreateContext(server, new ListResourcesRequestParams()), CancellationToken.None);

        result.Resources.Should().HaveCount(4);
        result.Resources.Should().Contain(r => r.Uri == "container://abc123");
        result.Resources.Should().Contain(r => r.Uri == "image://nginx:latest");
        result.Resources.Should().Contain(r => r.Uri == "volume://data");
        result.Resources.Should().Contain(r => r.Uri == "network://bridge");
    }

    [Fact]
    public async Task ReadResource_ContainerScheme_DispatchesToInspect()
    {
        var driver = new ScriptedDriver { ContainerInspect = "{\"Id\":\"abc123\"}" };
        var server = CreateServer(driver);

        var result = await WinContainers.Service.Mcp.WincontainerResources.ReadResourceAsync(
            CreateContext(server, new ReadResourceRequestParams { Uri = "container://abc123" }), CancellationToken.None);

        var text = Assert.Single(result.Contents) as TextResourceContents;
        text.Should().NotBeNull();
        text!.Uri.Should().Be("container://abc123");
        text.Text.Should().Be("{\"Id\":\"abc123\"}");
    }

    [Fact]
    public async Task ReadResource_NetworkScheme_ReturnsNetworksJson()
    {
        var driver = new FakeDriver { NetworksJson = "[{\"Name\":\"bridge\"}]" };
        var server = CreateServer(driver);

        var result = await WinContainers.Service.Mcp.WincontainerResources.ReadResourceAsync(
            CreateContext(server, new ReadResourceRequestParams { Uri = "network://bridge" }), CancellationToken.None);

        var text = Assert.Single(result.Contents) as TextResourceContents;
        text!.Text.Should().Contain("bridge");
    }

    [Fact]
    public async Task ReadResource_UnknownScheme_Throws()
    {
        var driver = new ScriptedDriver();
        var server = CreateServer(driver);

        var act = async () => await WinContainers.Service.Mcp.WincontainerResources.ReadResourceAsync(
            CreateContext(server, new ReadResourceRequestParams { Uri = "bogus://x" }), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ListResourceTemplates_ReturnsFourFixedTemplates()
    {
        var driver = new ScriptedDriver();
        var server = CreateServer(driver);

        var result = await WinContainers.Service.Mcp.WincontainerResources.ListResourceTemplatesAsync(
            CreateContext(server, new ListResourceTemplatesRequestParams()), CancellationToken.None);

        result.ResourceTemplates.Should().HaveCount(4);
        result.ResourceTemplates.Should().Contain(t => t.UriTemplate == "container://{id}");
        result.ResourceTemplates.Should().Contain(t => t.UriTemplate == "image://{name}");
        result.ResourceTemplates.Should().Contain(t => t.UriTemplate == "volume://{name}");
        result.ResourceTemplates.Should().Contain(t => t.UriTemplate == "network://{name}");
    }

    [Fact]
    public async Task Prompts_ReturnNonEmptyBodiesReferencingLiveData()
    {
        var driver = new ScriptedDriver
        {
            ContainerInspect = "{\"Id\":\"abc123\"}",
            ImageInspect = "{\"Repo\":\"nginx\"}",
            ContainersJson = "[]",
            ImagesJson = "[]",
            VolumesJson = "[]",
            NetworksJson = "[]"
        };

        var summarize = await WinContainers.Service.Mcp.WincontainerPrompts.SummarizeContainer("abc123", driver, CancellationToken.None);
        summarize.Should().Contain("abc123");

        var diagnose = await WinContainers.Service.Mcp.WincontainerPrompts.DiagnoseContainer("abc123", driver, CancellationToken.None);
        diagnose.Should().Contain("abc123");

        var explain = await WinContainers.Service.Mcp.WincontainerPrompts.ExplainImage("nginx", driver, CancellationToken.None);
        explain.Should().Contain("nginx");

        var runtime = await WinContainers.Service.Mcp.WincontainerPrompts.SummarizeRuntime(driver, CancellationToken.None);
        runtime.Should().Contain("Wincontainer");
    }

    /// <summary>A minimal <see cref="McpServer"/> implementation that only needs to provide
    /// an <see cref="IServiceProvider"/> for the resource handlers.</summary>
    private sealed class FakeMcpServer : McpServer
    {
        public FakeMcpServer(IServiceProvider services)
        {
            Services = services;
        }

        public override ClientCapabilities? ClientCapabilities => null;
        public override Implementation ClientInfo => new() { Name = "fake", Version = "0" };
        public override McpServerOptions ServerOptions => new();
        public override IServiceProvider Services { get; }
        public override string? SessionId => null;
        public override string? NegotiatedProtocolVersion => "2025-06-18";
        [Obsolete]
        public override LoggingLevel? LoggingLevel => null;

        public override Task RunAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public override Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request, CancellationToken cancellationToken = default) => Task.FromResult<JsonRpcResponse>(null!);
        public override Task SendMessageAsync(JsonRpcMessage message, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public override IAsyncDisposable RegisterNotificationHandler(string method, Func<JsonRpcNotification, CancellationToken, ValueTask> handler) => null!;
        public override ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
