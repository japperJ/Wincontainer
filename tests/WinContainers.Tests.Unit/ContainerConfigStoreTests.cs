using System.Text.Json;
using FluentAssertions;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit;

public sealed class ContainerConfigStoreTests
{
    [Fact]
    public void ContainerRunConfig_ShouldUseSafeDefaultsForLegacyJson()
    {
        var config = JsonSerializer.Deserialize<ContainerRunConfig>(
            """{"Image":"nginx","Ports":["8080:80/tcp"],"Volumes":[],"Env":[]}""");

        config.Should().NotBeNull();
        config!.Network.Should().BeNull();
        config.AllowLocalNetworkAccess.Should().BeFalse();
    }

    [Fact]
    public void ContainerRunConfig_ShouldRoundTripNetworkAndAccessState()
    {
        var original = new ContainerRunConfig
        {
            Image = "nginx",
            Ports = ["127.0.0.1:8080:80/tcp"],
            Volumes = ["data:/data"],
            Env = ["MODE=dev"],
            Network = "frontend",
            AllowLocalNetworkAccess = true
        };

        var restored = JsonSerializer.Deserialize<ContainerRunConfig>(
            JsonSerializer.Serialize(original));

        restored.Should().BeEquivalentTo(original);
    }
}
