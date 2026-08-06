using FluentAssertions;
using Microsoft.Extensions.AI;
using WinContainers.AI;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit.Ai;

public class ContainerAgentTests
{
    private static (ContainerAgent Agent, FakeChatClient Client, FakeDriver Driver, FakeObserver Observer) Create(
        bool confirmDestructive = true,
        Func<AgentStep, bool>? confirm = null,
        string composeDir = "compose")
    {
        var driver = new FakeDriver();
        var client = new FakeChatClient();
        var observer = new FakeObserver(confirm);
        var compose = new ComposeFileSaver(Path.Combine(Path.GetTempPath(), composeDir + Guid.NewGuid().ToString("N")));

        var registry = new AgentToolRegistry(driver, compose);
        var agent = new ContainerAgent(
            client,
            registry,
            observer,
            _ => Task.FromResult("snapshot"),
            confirmDestructive);

        return (agent, client, driver, observer);
    }

    [Fact]
    public async Task RunTurnAsync_ShouldReturnFinalText_WhenModelAnswersWithoutTools()
    {
        var (agent, client, _, _) = Create();
        client.EnqueueText("No containers need changes.");

        var history = new List<ChatMessage>();
        var result = await agent.RunTurnAsync(history, "Is everything healthy?", CancellationToken.None);

        result.Text.Should().Be("No containers need changes.");
        history.Should().NotContain(m => m.Role == ChatRole.System);
        history.Should().ContainSingle(m => m.Role == ChatRole.User);
    }

    [Fact]
    public async Task RunTurnAsync_ShouldDispatchTool_AndReturnFollowingText()
    {
        var (agent, client, driver, observer) = Create();

        client.EnqueueToolCall("call-1", "list_containers");
        client.EnqueueText("You have one container.");

        var history = new List<ChatMessage>();
        var result = await agent.RunTurnAsync(history, "List my containers.", CancellationToken.None);

        result.Text.Should().Be("You have one container.");
        observer.StartedSteps.Should().ContainSingle(s => s.Name == "list_containers");
        observer.StartedSteps[0].Preview.Should().Be("List all containers");
        observer.FinishedSteps.Should().ContainSingle(s => s.Name == "list_containers" && s.Success);

        // The assistant tool call and the tool result are recorded in history.
        history.Should().Contain(m => m.Contents.OfType<FunctionCallContent>().Any(c => c.Name == "list_containers"));
        history.Should().Contain(m => m.Role == ChatRole.Tool);
    }

    [Fact]
    public async Task RunTurnAsync_ShouldConfirmAndExecuteDestructiveAction_WhenAllowed()
    {
        var (agent, client, driver, observer) = Create(confirmDestructive: true, confirm: _ => true);

        client.EnqueueToolCall("call-1", "remove_container", new Dictionary<string, object?> { ["id"] = "web" });
        client.EnqueueText("Removed web.");

        await agent.RunTurnAsync(new List<ChatMessage>(), "Remove container web.", CancellationToken.None);

        observer.ConfirmationRequests.Should().ContainSingle(s => s.Name == "remove_container");
        driver.RemovedContainers.Should().Equal("web");
    }

    [Fact]
    public async Task RunTurnAsync_ShouldNotExecuteDestructiveAction_WhenDeclined()
    {
        var (agent, client, driver, observer) = Create(confirmDestructive: true, confirm: _ => false);

        client.EnqueueToolCall("call-1", "remove_container", new Dictionary<string, object?> { ["id"] = "web" });
        client.EnqueueText("I will not remove web.");

        await agent.RunTurnAsync(new List<ChatMessage>(), "Remove container web.", CancellationToken.None);

        driver.RemovedContainers.Should().BeEmpty();
        observer.FinishedSteps.Should().ContainSingle(s => s.Name == "remove_container" && s.Declined);
    }

    [Fact]
    public async Task RunTurnAsync_ShouldSkipConfirmation_WhenDisabled()
    {
        var (agent, client, driver, observer) = Create(confirmDestructive: false);

        client.EnqueueToolCall("call-1", "remove_container", new Dictionary<string, object?> { ["id"] = "web" });
        client.EnqueueText("Removed web.");

        await agent.RunTurnAsync(new List<ChatMessage>(), "Remove container web.", CancellationToken.None);

        observer.ConfirmationRequests.Should().BeEmpty();
        driver.RemovedContainers.Should().Equal("web");
    }

    [Fact]
    public async Task RunTurnAsync_ShouldCreateVolume_WithoutConfirmation()
    {
        var (agent, client, driver, observer) = Create(confirmDestructive: true);

        client.EnqueueToolCall("call-1", "create_volume", new Dictionary<string, object?> { ["name"] = "data" });
        client.EnqueueText("Created volume data.");

        await agent.RunTurnAsync(new List<ChatMessage>(), "Create a volume called data.", CancellationToken.None);

        observer.ConfirmationRequests.Should().BeEmpty();
        driver.CreatedVolumes.Should().Equal("data");
    }

    [Fact]
    public async Task RunTurnAsync_ShouldGenerateComposeFile()
    {
        var (agent, client, _, observer) = Create(composeDir: "compose");

        client.EnqueueToolCall(
            "call-1",
            "save_compose_file",
            new Dictionary<string, object?>
            {
                ["filename"] = "my-stack",
                ["yaml"] = "services:\n  web:\n    image: nginx:latest",
            });
        client.EnqueueText("Saved the compose file.");

        await agent.RunTurnAsync(new List<ChatMessage>(), "Write a compose file for nginx.", CancellationToken.None);

        observer.FinishedSteps.Should().ContainSingle(s => s.Name == "save_compose_file" && s.Success);
        observer.FinishedSteps[0].Output.Should().Contain("my-stack.yaml");
    }

    [Fact]
    public async Task RunTurnAsync_ShouldPropagateOperationCanceled()
    {
        var (agent, client, _, _) = Create();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        client.EnqueueToolCall("call-1", "list_containers");

        var act = async () => await agent.RunTurnAsync(new List<ChatMessage>(), "List containers.", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task RunTurnAsync_ShouldStopAfterMaxIterations()
    {
        var (agent, client, _, _) = Create();

        // Every iteration ends in a tool call, forcing the iteration cap.
        for (var i = 0; i < 20; i++)
        {
            client.EnqueueToolCall($"call-{i}", "list_containers");
        }

        var result = await agent.RunTurnAsync(new List<ChatMessage>(), "Do it forever.", CancellationToken.None);

        result.Text.Should().Contain("maximum number of steps");
        client.CallCount.Should().BeLessThanOrEqualTo(12);
    }

    [Fact]
    public void DestructiveToolNames_ShouldContainRemoveOperationsOnly()
    {
        AgentToolRegistry.DestructiveToolNames.Should().BeEquivalentTo(
            "remove_container",
            "remove_image",
            "remove_volume",
            "remove_network");
    }

    [Fact]
    public void BuildPreview_ShouldDescribeActions()
    {
        AgentToolRegistry.BuildPreview("start_container", new Dictionary<string, object?> { ["id"] = "web" })
            .Should().Be("Start container 'web'");
        AgentToolRegistry.BuildPreview("run_container", new Dictionary<string, object?> { ["image"] = "nginx" })
            .Should().Be("Run container from image 'nginx'");
    }

    [Fact]
    public async Task RunTurnAsync_ShouldHandleToolFailure_Gracefully()
    {
        var driver = new ThrowingDriver();
        var client = new FakeChatClient();
        var observer = new FakeObserver();
        var compose = new ComposeFileSaver(Path.Combine(Path.GetTempPath(), "compose" + Guid.NewGuid().ToString("N")));

        var agent = new ContainerAgent(
            client,
            new AgentToolRegistry(driver, compose),
            observer,
            _ => Task.FromResult("snapshot"),
            true);

        client.EnqueueToolCall("call-1", "list_containers");
        client.EnqueueText("I could not list containers.");

        var result = await agent.RunTurnAsync(new List<ChatMessage>(), "List containers.", CancellationToken.None);

        result.Text.Should().Be("I could not list containers.");
        observer.FinishedSteps.Should().ContainSingle(s => s.Name == "list_containers" && !s.Success);
        observer.FinishedSteps[0].Output.Should().NotBeNullOrEmpty();
    }

    private sealed class ThrowingDriver : FakeDriver
    {
        public override Task<string> GetContainersAsync(CancellationToken ct)
            => throw new InvalidOperationException("wslc exploded");
    }
}
