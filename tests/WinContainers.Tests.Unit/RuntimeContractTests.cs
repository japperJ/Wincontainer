using System.Text.Json;
using FluentAssertions;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;

namespace WinContainers.Tests.Unit;

public class RuntimeContractTests
{
    [Fact]
    public void ViewModelBase_ShouldHandleDispatcherLifecycleSafely()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/ViewModels/ViewModelBase.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("DispatcherQueue.GetForCurrentThread()");
        source.Should().Contain("dispatcherQueue is null");
        source.Should().Contain("if (!dispatcherQueue.TryEnqueue");
        source.Should().NotContain("App.DispatcherQueue.HasThreadAccess");
    }

    [Fact]
    public void ContainerDetailPage_ShouldUnsubscribeInspectPropertyChangedHandler()
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs"));
        var source = File.ReadAllText(path);

        source.Should().Contain("PropertyChangedEventHandler? _inspectPropertyChangedHandler");
        source.Should().Contain("_viewModel.PropertyChanged -= _inspectPropertyChangedHandler");
        source.Should().Contain("_viewModel.PropertyChanged += _inspectPropertyChangedHandler");
        source.Should().Contain("_inspectPropertyChangedHandler = null");
        source.Should().NotContain("_viewModel.PropertyChanged += async (s, e) =>");
    }

    [Fact]
    public void ServiceInfo_ShouldRoundTripPortTokenAndScripts()
    {
        var info = new ServiceInfo("12345", "secret-token")
        {
            Scripts = ["Get-Container", "Pull-Image"]
        };

        info.Port.Should().Be("12345");
        info.Token.Should().Be("secret-token");
        info.Scripts.Should().Contain("Get-Container");
        info.Scripts.Should().Contain("Pull-Image");
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldDefaultToLoopbackListenAndLoopbackClient()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", null);

        try
        {
            ServiceEndpointResolver.Resolve().Should().Be("http://127.0.0.1:5123");
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("127.0.0.1");
            ServiceEndpointResolver.ResolveServicePort().Should().Be("5123");
            ServiceEndpointResolver.ResolveToken().Should().BeEmpty();
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldListenOnLanWhenTokenConfigured()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", null);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("0.0.0.0");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldHonorHostEnvironmentVariableOverToken()
    {
        ServiceEndpointResolver.ClearOverrides();

        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", "192.168.1.5");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveServiceHost().Should().Be("192.168.1.5");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public void WslcDriver_ShouldExist()
    {
        typeof(WslcDriver).Should().NotBeNull();
    }

    [Fact]
    public void WslcDriver_ShouldExposeExpectedMethods()
    {
        var methods = typeof(WslcDriver).GetMethods()
            .Where(m => m.DeclaringType == typeof(WslcDriver))
            .Select(m => m.Name)
            .Distinct()
            .ToHashSet();

        methods.Should().Contain(nameof(WslcDriver.GetVersionAsync));
        methods.Should().Contain(nameof(WslcDriver.GetContainersAsync));
        methods.Should().Contain(nameof(WslcDriver.StartContainerAsync));
        methods.Should().Contain(nameof(WslcDriver.StopContainerAsync));
        methods.Should().Contain(nameof(WslcDriver.RemoveContainerAsync));
        methods.Should().Contain(nameof(WslcDriver.GetImagesAsync));
        methods.Should().Contain(nameof(WslcDriver.PullImageAsync));
        methods.Should().Contain(nameof(WslcDriver.RemoveImageAsync));
        methods.Should().Contain(nameof(WslcDriver.GetVolumesAsync));
        methods.Should().Contain(nameof(WslcDriver.CreateVolumeAsync));
        methods.Should().Contain(nameof(WslcDriver.RemoveVolumeAsync));
        methods.Should().Contain(nameof(WslcDriver.GetNetworksAsync));
        methods.Should().Contain(nameof(WslcDriver.CreateNetworkAsync));
        methods.Should().Contain(nameof(WslcDriver.RemoveNetworkAsync));
    }

    [Fact]
    public void WslcContainerParser_ShouldParseContainerJson()
    {
        var json = """
[
  {
    "ID": "abc123",
    "Names": "my-container",
    "Image": "nginx:alpine",
    "Status": "Running 2 hours",
    "Ports": "0.0.0.0:8080->80/tcp",
    "CreatedAt": "2025-01-01"
  },
  {
    "ID": "def456",
    "Names": "stopped-app",
    "Image": "alpine:latest",
    "Status": "Exited (0)",
    "Ports": "",
    "CreatedAt": "2025-01-02"
  }
]
""";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().HaveCount(2);
        containers[0].Id.Should().Be("abc123");
        containers[0].Name.Should().Be("my-container");
        containers[0].Image.Should().Be("nginx:alpine");
        containers[0].Status.Should().Be("Running 2 hours");
        containers[0].Ports.Should().Be("0.0.0.0:8080->80/tcp");
        containers[0].PortLinks.Should().ContainSingle(l => l.Url == "localhost:8080");

        containers[1].Id.Should().Be("def456");
        containers[1].Name.Should().Be("stopped-app");
        containers[1].Ports.Should().Be("No ports");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseCaseInsensitiveContainerFields()
    {
        var json = "[{\"id\":\"abc123\",\"name\":\"clean-host\",\"image\":\"nginx:latest\",\"status\":\"Up\",\"ports\":\"\"}]";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].Id.Should().Be("abc123");
        containers[0].Name.Should().Be("clean-host");
        containers[0].Image.Should().Be("nginx:latest");
        containers[0].Status.Should().Be("Up");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseStructuredPortsAndNumericState()
    {
        var json = "[{\"Id\":\"abc123\",\"Image\":\"nodered/node-red:latest\",\"Name\":\"nodered1\",\"Ports\":[{\"BindingAddress\":\"127.0.0.1\",\"ContainerPort\":1880,\"HostPort\":1880,\"Protocol\":6}],\"State\":2}]";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].Name.Should().Be("nodered1");
        containers[0].Status.Should().Be("Up");
        containers[0].Ports.Should().Be("127.0.0.1:1880->1880/tcp");
        containers[0].PortLinks.Should().ContainSingle(link => link.Url == "localhost:1880");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseContainerMounts()
    {
        var json = """
[
  {
    "ID": "mnt123",
    "Names": "data-app",
    "Image": "busybox:latest",
    "Status": "Up",
    "Ports": "",
    "Mounts": [
      { "Source": "app-data", "Destination": "/data" },
      { "SourcePath": "/host/config", "Destination": "/config" }
    ]
  }
]
""";

        var containers = WslcContainerParser.ParseContainers(json);

        containers.Should().ContainSingle();
        containers[0].MountInfos.Should().Contain(m => m.Source == "app-data" && m.Target == "/data");
        containers[0].MountInfos.Should().Contain(m => m.Source == "/host/config" && m.Target == "/config");
    }

    [Fact]
    public void WslcContainerParser_ShouldParseImageJson()
    {
        var json = """
[
  {
    "ID": "sha256:abc123",
    "Repository": "nginx",
    "Tag": "alpine",
    "Size": "42MB",
    "CreatedSince": "2 weeks ago"
  }
]
""";

        var images = WslcContainerParser.ParseImages(json);

        images.Should().ContainSingle();
        images[0].Repository.Should().Be("nginx");
        images[0].Tag.Should().Be("alpine");
        images[0].ID.Should().Be("sha256:abc123");
        images[0].Size.Should().Be("42MB");
    }

    [Fact]
    public void WslcContainerParser_ShouldHandleEmptyJson()
    {
        WslcContainerParser.ParseContainers("[]").Should().BeEmpty();
        WslcContainerParser.ParseContainers("").Should().BeEmpty();
        WslcContainerParser.ParseContainers(null!).Should().BeEmpty();
        WslcContainerParser.ParseImages("[]").Should().BeEmpty();
        WslcContainerParser.ParseImages("").Should().BeEmpty();
    }

    [Fact]
    public void ImageListFormatter_ShouldRenderReadableImageSummary()
    {
        const string rawOutput = """
{"ID":"8b1e78743a03","Repository":"nginx","Tag":"alpine","Name":"docker.io/library/nginx:alpine"}
{"ID":"5b10f432ef3d","Repository":"alpine","Tag":"latest","Name":"docker.io/library/alpine:latest"}
""";

        var formatted = ImageListFormatter.Format(rawOutput);

        formatted.Should().Contain("Images: 2");
        formatted.Should().Contain("nginx:alpine");
        formatted.Should().Contain("alpine:latest");
        formatted.Should().NotContain("\"ID\":\"8b1e78743a03\"");
    }

    [Fact]
    public void WslcCommands_ShouldGenerateExpectedCommandStrings()
    {
        WslcCommands.Version().Should().Be("--version");
        WslcCommands.ContainerPs().Should().Be("container ps --all --format json");
        WslcCommands.ContainerStart("abc").Should().Be("container start abc");
        WslcCommands.ContainerStop("abc").Should().Be("container stop abc");
        WslcCommands.ImageLs().Should().Be("image ls --format json");
        WslcCommands.ImagePull("nginx").Should().Be("image pull nginx");
        WslcCommands.VolumeLs().Should().Be("volume ls --format json");
        WslcCommands.NetworkLs().Should().Be("network ls --format json");
    }

    [Fact]
    public void WslcVersionFormatter_ShouldExtractWslcVersion()
    {
        WslcVersionFormatter.Format("wslc 2.9.4.0").Should().Be("2.9.4.0");
    }

    [Fact]
    public void WslcRuntimeProbe_ShouldUseAContainerCommandInsteadOfVersionOnly()
    {
        WslcCommands.ContainerPs().Should().NotBe(WslcCommands.Version());
        WslcCommands.ContainerPs().Should().Contain("container ps");
    }

    [Fact]
    public void WslcCommands_ShouldQuoteSpacesInArgs()
    {
        WslcCommands.ContainerStart("my container").Should().Contain("\"my container\"");
        WslcCommands.ImagePull("my image:v2").Should().Be("image pull \"my image:v2\"");
    }

    [Fact]
    public void WslcCommands_Run_ShouldNotEmitUnsupportedRestartOption()
    {
        var command = WslcCommands.Run("linuxserver/heimdall:latest", "heimdall97", restart: "unless-stopped");

        command.Should().Be("run --detach --name heimdall97 linuxserver/heimdall:latest");
        command.Should().NotContain("--restart");
    }

    [Fact]
    public void WslcResourceParser_ShouldParseVolumeList()
    {
        const string output = "DRIVER VOLUME NAME\nlocal app-data\nlocal cache";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Select(v => v.Name).Should().Equal("app-data", "cache");
    }

    [Fact]
    public void WslcResourceParser_ShouldParseJsonVolumeList()
    {
        const string output = "{\"Driver\":\"local\",\"Name\":\"app-data\",\"Mountpoint\":\"/var/lib/volumes/app-data/_data\",\"Scope\":\"local\"}\n" +
            "{\"Driver\":\"local\",\"Name\":\"cache\",\"Mountpoint\":\"/var/lib/volumes/cache/_data\",\"Scope\":\"local\"}";

        var volumes = WslcResourceParser.ParseVolumes(output);

        volumes.Select(v => v.Name).Should().Equal("app-data", "cache");
    }

    [Fact]
    public void WslcResourceParser_ShouldParseNetworkList()
    {
        const string output = "NETWORK ID NAME DRIVER SCOPE\nabc123 bridge bridge local\ndef456 app-net bridge local";

        var networks = WslcResourceParser.ParseNetworks(output);

        networks.Select(n => n.Name).Should().Equal("bridge", "app-net");
        networks[0].Details.Should().Contain("abc123");
    }

    [Fact]
    public void WslcResourceParser_ShouldProtectBuiltInNetworks()
    {
        const string output = "{\"ID\":\"\",\"Name\":\"bridge\",\"Labels\":\"\"}\n" +
            "{\"ID\":\"custom\",\"Name\":\"app-net\",\"Labels\":\"\"}";

        var networks = WslcResourceParser.ParseNetworks(output);

        networks.Single(n => n.Name == "bridge").CanDelete.Should().BeFalse();
        networks.Single(n => n.Name == "app-net").CanDelete.Should().BeTrue();
    }

    [Fact]
    public void WslcCommands_ShouldGenerateContainerExecCommands()
    {
        WslcCommands.ContainerExecCommand("abc", "ls -lap /")
            .Should().Be("container exec abc ls -lap /");
        WslcCommands.ContainerExecShell("abc", "printf 'hello'")
            .Should().Be("container exec abc sh -c \"printf 'hello'\"");
        WslcCommands.ContainerExecShell("abc", "echo ok", "bash")
            .Should().Be("container exec abc bash -c \"echo ok\"");
    }

    [Fact]
    public void RuntimeTools_ShouldCheckExecutableOnPath()
    {
        RuntimeTools.IsExecutableAvailable("wslc");
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldHonorEnvironmentPortOverride()
    {
        ServiceEndpointResolver.ClearOverrides();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "5155");

        try
        {
            var endpoint = ServiceEndpointResolver.Resolve();

            endpoint.Should().Be("http://127.0.0.1:5155");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", null);
        }
    }

    [Fact]
    public void ServiceEndpointResolver_ShouldResolveBearerToken()
    {
        ServiceEndpointResolver.ClearOverrides();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        try
        {
            ServiceEndpointResolver.ResolveToken().Should().Be("test-token");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", null);
        }
    }

    [Fact]
    public void BearerTokenValidator_ShouldAuthorizeBearerTokenRequests()
    {
        BearerTokenValidator.IsAuthorized(string.Concat("Bearer", " ", "abc123"), "abc123").Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRejectInvalidBearerTokens()
    {
        BearerTokenValidator.IsAuthorized(string.Concat("Bearer", " ", "wrong-token"), "abc123").Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRejectRequestsWhenExpectedTokenIsEmpty()
    {
        BearerTokenValidator.IsAuthorized("Bearer abc123", string.Empty).Should().BeFalse();
        BearerTokenValidator.IsAuthorized(string.Empty, string.Empty).Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthorizationForAnyIpListenHostWithoutToken()
    {
        BearerTokenValidator.RequiresAuthorization("0.0.0.0", string.Empty).Should().BeTrue();
        BearerTokenValidator.RequiresAuthorization("::", string.Empty).Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldSkipAuthForLoopbackBindingWithoutConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("127.0.0.1", string.Empty).Should().BeFalse();
        BearerTokenValidator.RequiresAuthorization("localhost", string.Empty).Should().BeFalse();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthForLoopbackBindingWithConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("127.0.0.1", "secret-token").Should().BeTrue();
    }

    [Fact]
    public void BearerTokenValidator_ShouldRequireAuthForNonLoopbackBindingWithoutConfiguredToken()
    {
        BearerTokenValidator.RequiresAuthorization("0.0.0.0", string.Empty).Should().BeTrue();
        BearerTokenValidator.RequiresAuthorization("192.168.1.10", string.Empty).Should().BeTrue();
    }

    [Theory]
    [InlineData("O'Brien")]
    [InlineData("\\\"; alert('x')")]
    [InlineData("</script><script>alert('x')</script>")]
    public void WebViewScriptEncoder_ShouldKeepJsonPayloadInsideOneJavaScriptArgument(string json)
    {
        var script = WebViewScriptEncoder.BuildSetJsonScript(json);

        script.Should().StartWith("setJson(");
        script.Should().EndWith(")");
        script.Should().Be($"setJson({JsonSerializer.Serialize(json)})");
    }

    [Fact]
    public void HttpClientTimeouts_ShouldCreateFiniteClientsForServiceAndUpdates()
    {
        using var serviceClient = HttpClientTimeouts.Create(HttpClientTimeouts.ServiceTimeout);
        using var updateClient = HttpClientTimeouts.Create(HttpClientTimeouts.UpdateTimeout);

        HttpClientTimeouts.ServiceTimeout.Should().BePositive();
        HttpClientTimeouts.UpdateTimeout.Should().BePositive();
        serviceClient.Timeout.Should().Be(HttpClientTimeouts.ServiceTimeout);
        updateClient.Timeout.Should().Be(HttpClientTimeouts.UpdateTimeout);
        serviceClient.Timeout.Should().NotBe(Timeout.InfiniteTimeSpan);
        updateClient.Timeout.Should().NotBe(Timeout.InfiniteTimeSpan);
    }
}
