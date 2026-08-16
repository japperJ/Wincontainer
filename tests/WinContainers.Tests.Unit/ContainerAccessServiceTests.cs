using FluentAssertions;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit;

public sealed class ContainerAccessServiceTests
{
    [Fact]
    public async Task SetAccessAsync_ShouldRecreateAndPersistConvertedConfiguration()
    {
        var calls = new List<string>();
        ContainerRunConfig? saved = null;
        var driver = new FakeDriver(calls);
        var service = new ContainerAccessService(
            driver,
            _ => new ContainerRunConfig
            {
                Image = "nginx",
                Ports = ["8080:80/tcp"],
                Volumes = ["data:/data"],
                Env = ["MODE=dev"],
                Network = "frontend"
            },
            (_, config) => saved = config);

        var result = await service.SetAccessAsync("container-id", true, "web");

        result.Success.Should().BeTrue();
        result.AllowLocalNetworkAccess.Should().BeTrue();
        result.Ports.Should().Equal("0.0.0.0:8080:80/tcp");
        calls.Should().Equal("stop:container-id", "remove:container-id", "run:web");
        driver.RunArguments.Should().Be(("nginx", "web", "0.0.0.0:8080:80/tcp", "data:/data", "MODE=dev", "frontend"));
        saved.Should().NotBeNull();
        saved!.AllowLocalNetworkAccess.Should().BeTrue();
        saved.Ports.Should().Equal("0.0.0.0:8080:80/tcp");
    }

    [Fact]
    public async Task SetAccessAsync_ShouldStopBeforeRemoveAndRun()
    {
        var calls = new List<string>();
        var service = new ContainerAccessService(
            new FakeDriver(calls),
            _ => new ContainerRunConfig { Image = "nginx", Ports = ["8080:80"] },
            (_, _) => { });

        await service.SetAccessAsync("id", false);

        calls.Should().Equal("stop:id", "remove:id", "run:id");
    }

    [Fact]
    public async Task SetAccessAsync_ShouldRejectMissingConfigWithoutDriverCalls()
    {
        var calls = new List<string>();
        var service = new ContainerAccessService(new FakeDriver(calls), _ => null, (_, _) => { });

        var result = await service.SetAccessAsync("missing", true);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("unavailable");
        calls.Should().BeEmpty();
    }

    [Theory]
    [InlineData("stop")]
    [InlineData("remove")]
    [InlineData("run")]
    public async Task SetAccessAsync_ShouldPropagateDriverFailure(string failingOperation)
    {
        var calls = new List<string>();
        var service = new ContainerAccessService(
            new FakeDriver(calls) { FailingOperation = failingOperation },
            _ => new ContainerRunConfig { Image = "nginx", Ports = ["8080:80"] },
            (_, _) => { });

        var result = await service.SetAccessAsync("id", true);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("failed");
        if (failingOperation != "run")
            calls.Should().NotContain("run:id");
    }

    private sealed class FakeDriver(List<string> calls) : IWslcDriver
    {
        public string? FailingOperation { get; init; }
        public (string Image, string Name, string Port, string Volume, string Env, string Network)? RunArguments { get; private set; }

        public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(true);
        public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("ok");
        public Task<string> GetContainersAsync(CancellationToken ct) => Task.FromResult("[]");
        public Task<string> StartContainerAsync(string id, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> StopContainerAsync(string id, CancellationToken ct)
        {
            calls.Add($"stop:{id}");
            return Task.FromResult(FailingOperation == "stop" ? "wslc error (1): stop failed" : "ok");
        }
        public Task<string> RestartContainerAsync(string id, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> RemoveContainerAsync(string id, CancellationToken ct)
        {
            calls.Add($"remove:{id}");
            return Task.FromResult(FailingOperation == "remove" ? "wslc error (1): remove failed" : "ok");
        }
        public Task<string> InspectContainerAsync(string id, CancellationToken ct) => Task.FromResult("{}");
        public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) => Task.FromResult("");
        public Task<string> GetImagesAsync(CancellationToken ct) => Task.FromResult("[]");
        public Task<string> PullImageAsync(string image, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> RemoveImageAsync(string id, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> InspectImageAsync(string id, CancellationToken ct) => Task.FromResult("{}");
        public Task<string> GetVolumesAsync(CancellationToken ct) => Task.FromResult("[]");
        public Task<string> CreateVolumeAsync(string name, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> RemoveVolumeAsync(string name, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> InspectVolumeAsync(string name, CancellationToken ct) => Task.FromResult("{}");
        public Task<string> GetNetworksAsync(CancellationToken ct) => Task.FromResult("[]");
        public Task<string> CreateNetworkAsync(string name, CancellationToken ct) => Task.FromResult("ok");
        public Task<string> RemoveNetworkAsync(string name, CancellationToken ct) => Task.FromResult("ok");

        public Task<string> RunContainerAsync(
            string image,
            string? name = null,
            IEnumerable<string>? ports = null,
            IEnumerable<string>? volumes = null,
            IEnumerable<string>? env = null,
            CancellationToken ct = default,
            string? network = null)
        {
            calls.Add($"run:{name}");
            RunArguments = (
                image,
                name ?? "",
                ports?.Single() ?? "",
                volumes?.SingleOrDefault() ?? "",
                env?.SingleOrDefault() ?? "",
                network ?? "");
            return Task.FromResult(FailingOperation == "run" ? "wslc error (1): run failed" : "ok");
        }

        public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default) => Task.FromResult("");
        public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default) => Task.FromResult("");
    }
}
