using FluentAssertions;
using WinContainers.AI;

namespace WinContainers.Tests.Unit.Ai;

public class ProviderConfigTests
{
    [Fact]
    public void Defaults_ShouldPointAtOpenAiCompatible()
    {
        var config = new AiProviderConfig();

        config.Kind.Should().Be(AiProviderKind.OpenAiCompatible);
        config.Endpoint.Should().Be("https://api.openai.com/v1");
        config.Model.Should().Be("gpt-4o-mini");
        config.ConfirmDestructiveActions.Should().BeTrue();
    }

    [Fact]
    public void Factory_ShouldRejectEmptyEndpoint()
    {
        var factory = new OpenAiCompatibleChatClientFactory();

        var act = () => factory.Create(new AiProviderConfig { Endpoint = " ", Model = "gpt-4o-mini" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Factory_ShouldRejectEmptyModel()
    {
        var factory = new OpenAiCompatibleChatClientFactory();

        var act = () => factory.Create(new AiProviderConfig { Endpoint = "http://localhost:11434/v1", Model = "" });

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Factory_ShouldCreateClient_ForLocalOllamaEndpoint()
    {
        var factory = new OpenAiCompatibleChatClientFactory();

        var client = factory.Create(new AiProviderConfig
        {
            Kind = AiProviderKind.Ollama,
            Endpoint = "http://localhost:11434/v1",
            Model = "qwen2.5:3b",
        });

        client.Should().NotBeNull();
    }
}

public class ComposeFileSaverTests
{
    [Fact]
    public void Save_ShouldWriteValidYaml_ToSafePath()
    {
        var dir = Path.Combine(Path.GetTempPath(), "compose-test" + Guid.NewGuid().ToString("N"));
        try
        {
            var saver = new ComposeFileSaver(dir);

            var path = saver.Save("my-stack", "services:\n  web:\n    image: nginx:latest\n");

            path.Should().Be(Path.Combine(dir, "my-stack.yaml"));
            File.ReadAllText(path).Should().Contain("nginx");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_ShouldSanitizeFilename_AndFallBackWhenEmpty()
    {
        var dir = Path.Combine(Path.GetTempPath(), "compose-test" + Guid.NewGuid().ToString("N"));
        try
        {
            var saver = new ComposeFileSaver(dir);

            var path = saver.Save("../evil:name", "services:\n  a:\n    image: x\n");
            path.Should().EndWith(".yaml");
            Path.GetFileName(path).Should().NotContain("..");
            Path.GetFileName(path).Should().NotContain(":");
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Save_ShouldRejectEmptyYaml()
    {
        var saver = new ComposeFileSaver(Path.Combine(Path.GetTempPath(), "compose-test" + Guid.NewGuid().ToString("N")));

        var act = () => saver.Save("x", "   ");

        act.Should().Throw<InvalidOperationException>().WithMessage("*empty*");
    }

    [Fact]
    public void Save_ShouldRejectMalformedYaml()
    {
        var saver = new ComposeFileSaver(Path.Combine(Path.GetTempPath(), "compose-test" + Guid.NewGuid().ToString("N")));

        var act = () => saver.Save("x", "services:\n  - \t\t bad indentation :::: :\n    :::");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not valid*");
    }
}

public class ContainerSnapshotBuilderTests
{
    [Fact]
    public async Task BuildAsync_ShouldSummarizeContainersAndImages()
    {
        var driver = new FakeDriver
        {
            ContainersJson = """
                [
                  { "ID": "c1", "Names": "web", "Status": "Up", "Image": "nginx:latest", "Ports": "80->80" },
                  { "ID": "c2", "Names": "db", "Status": "Stopped", "Image": "postgres:16" }
                ]
                """,
            ImagesJson = """
                [
                  { "Repository": "nginx", "Tag": "latest", "ID": "i1" },
                  { "Repository": "postgres", "Tag": "16", "ID": "i2" }
                ]
                """,
        };

        var builder = new ContainerSnapshotBuilder(driver);
        var snapshot = await builder.BuildAsync(CancellationToken.None);

        snapshot.Should().Contain("Containers (2)");
        snapshot.Should().Contain("web");
        snapshot.Should().Contain("nginx:latest");
        snapshot.Should().Contain("Images (2)");
        snapshot.Should().Contain("postgres:16");
    }

    [Fact]
    public async Task BuildAsync_ShouldReportNone_WhenEmpty()
    {
        var builder = new ContainerSnapshotBuilder(new FakeDriver());
        var snapshot = await builder.BuildAsync(CancellationToken.None);

        snapshot.Should().Contain("Containers: none");
        snapshot.Should().Contain("Images: none");
    }

    [Fact]
    public async Task BuildAsync_ShouldHandleDriverFailure()
    {
        var driver = new ThrowingDriver();
        var builder = new ContainerSnapshotBuilder(driver);

        var snapshot = await builder.BuildAsync(CancellationToken.None);

        snapshot.Should().Contain("unavailable");
    }

    private sealed class ThrowingDriver : FakeDriver
    {
        public override Task<string> GetContainersAsync(CancellationToken ct)
            => throw new InvalidOperationException("wslc exploded");
    }
}
