using Microsoft.Extensions.AI;
using WinContainers.AI;
using WinContainers.Runtime;

namespace WinContainers.Tests.Unit.Ai;

/// <summary>
/// A scripted <see cref="IChatClient"/> for agent tests. Each call to
/// <see cref="GetStreamingResponseAsync"/> returns the next queued update
/// list, in the order they were enqueued.
/// </summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly Queue<IReadOnlyList<ChatResponseUpdate>> _responses = new();
    private readonly Queue<Exception> _errors = new();

    /// <summary>Number of streaming calls made so far.</summary>
    public int CallCount { get; private set; }

    /// <summary>All options received per streaming call.</summary>
    public List<ChatOptions> ReceivedOptions { get; } = [];

    /// <summary>Queues an exception to be thrown by the next streaming call.</summary>
    public void EnqueueError(Exception ex)
    {
        _errors.Enqueue(ex);
    }

    /// <summary>Queues one streaming response to be returned on the next call.</summary>
    public void Enqueue(params ChatResponseUpdate[] updates)
    {
        _responses.Enqueue(updates);
    }

    /// <summary>Queues a plain text response with no tool calls.</summary>
    public void EnqueueText(string text)
    {
        Enqueue(new ChatResponseUpdate(ChatRole.Assistant, text));
    }

    /// <summary>Queues a response containing a single function call.</summary>
    public void EnqueueToolCall(string callId, string name, IDictionary<string, object?>? arguments = null)
    {
        Enqueue(new ChatResponseUpdate(
            ChatRole.Assistant,
            [new FunctionCallContent(callId, name, arguments ?? new Dictionary<string, object?>())]));
    }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> chats,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Tests use the streaming path only.");

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> chats,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        CallCount++;
        ReceivedOptions.Add(options ?? new ChatOptions());

        if (_errors.Count > 0)
        {
            throw _errors.Dequeue();
        }

        var updates = _responses.Count > 0
            ? _responses.Dequeue()
            : (IReadOnlyList<ChatResponseUpdate>)[new ChatResponseUpdate(ChatRole.Assistant, "")];

        foreach (var update in updates)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return update;
        }

        await Task.CompletedTask;
    }

    public object? GetService(Type serviceType, object? key = null) => null;

    public void Dispose()
    {
    }
}

/// <summary>
/// A recorder <see cref="IWslcDriver"/> for agent tests. It captures calls so
/// tests can verify which tools were dispatched and with what arguments.
/// </summary>
public class FakeDriver : IWslcDriver
{
    public List<string> StartedContainers { get; } = [];
    public List<string> StoppedContainers { get; } = [];
    public List<string> RemovedContainers { get; } = [];
    public List<string> RemovedImages { get; } = [];
    public List<string> RemovedVolumes { get; } = [];
    public List<string> RemovedNetworks { get; } = [];
    public List<string> CreatedVolumes { get; } = [];
    public List<string> CreatedNetworks { get; } = [];
    public List<string> PulledImages { get; } = [];
    public List<(string Image, string? Name, string? Ports, string? Volumes, string? Env)> RanContainers { get; } = [];
    public List<(string Id, string Command)> ExecCommands { get; } = [];
    public string? LastLoadImageTarPath { get; private set; }
    public string? LastLoadImageTarData { get; private set; }

    public string ContainersJson { get; set; } = "[]";
    public string ImagesJson { get; set; } = "[]";
    public string VolumesJson { get; set; } = "[]";
    public string NetworksJson { get; set; } = "[]";
    public bool Available { get; set; } = true;

    public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(Available);
    public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("1.0.0");
    public virtual Task<string> GetContainersAsync(CancellationToken ct) => Task.FromResult(ContainersJson);

    public Task<string> StartContainerAsync(string id, CancellationToken ct)
    {
        StartedContainers.Add(id);
        return Task.FromResult($"started {id}");
    }

    public Task<string> StopContainerAsync(string id, CancellationToken ct)
    {
        StoppedContainers.Add(id);
        return Task.FromResult($"stopped {id}");
    }

    public Task<string> RestartContainerAsync(string id, CancellationToken ct) => Task.FromResult($"restarted {id}");

    public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) => Task.FromResult($"renamed {id}");

    public Task<string> RemoveContainerAsync(string id, CancellationToken ct)
    {
        RemovedContainers.Add(id);
        return Task.FromResult($"removed {id}");
    }

    public Task<string> InspectContainerAsync(string id, CancellationToken ct) => Task.FromResult("{}");

    public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) => Task.FromResult("log line 1");

    public Task<string> GetImagesAsync(CancellationToken ct) => Task.FromResult(ImagesJson);

    public Task<string> PullImageAsync(string image, CancellationToken ct)
    {
        PulledImages.Add(image);
        return Task.FromResult($"pulled {image}");
    }

    public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct)
    {
        LastLoadImageTarPath = tarPath;
        LastLoadImageTarData = tarData;
        return Task.FromResult(string.Empty);
    }

    public Task<string> RemoveImageAsync(string id, CancellationToken ct)
    {
        RemovedImages.Add(id);
        return Task.FromResult($"removed {id}");
    }

    public Task<string> InspectImageAsync(string id, CancellationToken ct) => Task.FromResult("{}");

    public Task<string> GetVolumesAsync(CancellationToken ct) => Task.FromResult(VolumesJson);

    public Task<string> CreateVolumeAsync(string name, CancellationToken ct)
    {
        CreatedVolumes.Add(name);
        return Task.FromResult($"created volume {name}");
    }

    public Task<string> RemoveVolumeAsync(string name, CancellationToken ct)
    {
        RemovedVolumes.Add(name);
        return Task.FromResult($"removed volume {name}");
    }

    public Task<string> InspectVolumeAsync(string name, CancellationToken ct) => Task.FromResult("{}");

    public Task<string> GetNetworksAsync(CancellationToken ct) => Task.FromResult(NetworksJson);

    public Task<string> CreateNetworkAsync(string name, CancellationToken ct)
    {
        CreatedNetworks.Add(name);
        return Task.FromResult($"created network {name}");
    }

    public Task<string> RemoveNetworkAsync(string name, CancellationToken ct)
    {
        RemovedNetworks.Add(name);
        return Task.FromResult($"removed network {name}");
    }

    public Task<string> RunContainerAsync(
        string image,
        string? name = null,
        IEnumerable<string>? ports = null,
        IEnumerable<string>? volumes = null,
        IEnumerable<string>? env = null,
        CancellationToken ct = default)
    {
        RanContainers.Add((image, name, ports is null ? null : string.Join(",", ports),
            volumes is null ? null : string.Join(",", volumes),
            env is null ? null : string.Join(",", env)));
        return Task.FromResult($"ran {image}");
    }

    public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default)
    {
        ExecCommands.Add((id, command));
        return Task.FromResult("command output");
    }

    public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default)
    {
        ExecCommands.Add((id, shellCommand));
        return Task.FromResult("shell output");
    }
}

/// <summary>
/// A recording <see cref="IAgentObserver"/> that can be scripted to allow or
/// decline destructive-action confirmations.
/// </summary>
public sealed class FakeObserver : IAgentObserver
{
    private readonly Func<AgentStep, bool> _confirm;

    /// <summary>Text deltas received, in order.</summary>
    public List<string> TextDeltas { get; } = [];

    /// <summary>Steps that started, in order.</summary>
    public List<AgentStep> StartedSteps { get; } = [];

    /// <summary>Steps that finished, in order.</summary>
    public List<AgentStep> FinishedSteps { get; } = [];

    /// <summary>Steps that asked for confirmation, in order.</summary>
    public List<AgentStep> ConfirmationRequests { get; } = [];

    /// <summary>Retry waits reported, in order (seconds, next attempt, max attempts).</summary>
    public List<(int Seconds, int Attempt, int MaxAttempts)> RetryWaits { get; } = [];

    public FakeObserver(Func<AgentStep, bool>? confirm = null)
    {
        _confirm = confirm ?? (_ => true);
    }

    public Task OnTextDeltaAsync(string delta, CancellationToken ct)
    {
        TextDeltas.Add(delta);
        return Task.CompletedTask;
    }

    public Task OnStepStartingAsync(AgentStep step, CancellationToken ct)
    {
        StartedSteps.Add(step);
        return Task.CompletedTask;
    }

    public Task OnStepFinishedAsync(AgentStep step, CancellationToken ct)
    {
        FinishedSteps.Add(step);
        return Task.CompletedTask;
    }

    public Task<bool> OnConfirmDestructiveAsync(AgentStep step, CancellationToken ct)
    {
        ConfirmationRequests.Add(step);
        return Task.FromResult(_confirm(step));
    }

    public Task OnRetryWaitAsync(int seconds, int attempt, int maxAttempts, CancellationToken ct)
    {
        RetryWaits.Add((seconds, attempt, maxAttempts));
        return Task.CompletedTask;
    }
}
