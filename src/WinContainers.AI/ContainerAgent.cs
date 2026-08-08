using System.Text;
using Microsoft.Extensions.AI;

namespace WinContainers.AI;

/// <summary>
/// A tool-calling agent that controls containers through the registry's tools.
/// It streams assistant text, runs tool calls one at a time, shows each step
/// through <see cref="IAgentObserver"/>, and requires confirmation for
/// destructive actions.
/// </summary>
public sealed class ContainerAgent
{
    private const int MaxIterations = 10;
    private const int MaxStepOutputChars = 8000;
    private const string MaxStepsMessage =
        "I reached the maximum number of steps for this request. Try breaking the task into smaller parts.";

    /// <summary>Default seconds to wait between retries after a transient error.</summary>
    public const int RetryDelaySecondsDefault = 10;

    /// <summary>Default number of attempts (initial call plus retries) for a turn.</summary>
    public const int MaxAttemptsDefault = 3;

    private readonly IChatClient _client;
    private readonly AgentToolRegistry _registry;
    private readonly IAgentObserver _observer;
    private readonly Func<CancellationToken, Task<string>> _snapshotProvider;
    private readonly bool _confirmDestructiveActions;
    private readonly int _retryDelaySeconds;
    private readonly int _maxAttempts;

    public ContainerAgent(
        IChatClient client,
        AgentToolRegistry registry,
        IAgentObserver observer,
        Func<CancellationToken, Task<string>> snapshotProvider,
        bool confirmDestructiveActions,
        int retryDelaySeconds = RetryDelaySecondsDefault,
        int maxAttempts = MaxAttemptsDefault)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(registry);
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(snapshotProvider);

        _client = client;
        _registry = registry;
        _observer = observer;
        _snapshotProvider = snapshotProvider;
        _confirmDestructiveActions = confirmDestructiveActions;
        _retryDelaySeconds = Math.Max(0, retryDelaySeconds);
        _maxAttempts = Math.Max(1, maxAttempts);
    }

    /// <summary>
    /// Runs one turn. <paramref name="history"/> holds the persisted conversation
    /// (user and assistant messages). It is updated with the user message and all
    /// messages produced during the turn, so the caller can persist it afterwards.
    /// </summary>
    public async Task<AgentTurnResult> RunTurnAsync(IList<ChatMessage> history, string userMessage, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(history);
        ArgumentException.ThrowIfNullOrWhiteSpace(userMessage);

        var baseHistory = history.ToList();

        for (var attempt = 1; attempt <= _maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            ResetHistory(history, baseHistory);

            try
            {
                return await RunAttemptAsync(history, userMessage, ct);
            }
            catch (Exception ex) when (AgentErrorClassifier.IsRetryable(ex) && attempt < _maxAttempts)
            {
                await _observer.OnRetryWaitAsync(_retryDelaySeconds, attempt + 1, _maxAttempts, ct);
                await Task.Delay(TimeSpan.FromSeconds(_retryDelaySeconds), ct);
            }
        }

        throw new InvalidOperationException("RunTurnAsync always returns or throws within the attempt loop.");
    }

    private static void ResetHistory(IList<ChatMessage> history, IList<ChatMessage> baseHistory)
    {
        history.Clear();
        foreach (var message in baseHistory)
        {
            history.Add(message);
        }
    }

    private async Task<AgentTurnResult> RunAttemptAsync(IList<ChatMessage> history, string userMessage, CancellationToken ct)
    {
        string snapshot;
        try
        {
            snapshot = await _snapshotProvider(ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            snapshot = $"- Container state unavailable ({ex.Message})";
        }

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, BuildSystemPrompt(snapshot)),
        };
        messages.AddRange(history);
        messages.Add(new ChatMessage(ChatRole.User, userMessage));
        history.Add(new ChatMessage(ChatRole.User, userMessage));

        var options = new ChatOptions
        {
            Tools = _registry.Tools.ToList(),
            AllowMultipleToolCalls = false,
        };

        for (var iteration = 0; iteration < MaxIterations; iteration++)
        {
            ct.ThrowIfCancellationRequested();

            var (assistantText, functionCalls) = await GetAssistantTurnAsync(messages, options, ct);

            if (functionCalls.Count == 0)
            {
                return new AgentTurnResult { Text = assistantText };
            }

            var assistantMessage = new ChatMessage(ChatRole.Assistant, new List<AIContent>());
            if (assistantText.Length > 0)
                assistantMessage.Contents.Add(new TextContent(assistantText));
            foreach (var call in functionCalls)
                assistantMessage.Contents.Add(call);

            messages.Add(assistantMessage);
            history.Add(assistantMessage);

            foreach (var call in functionCalls)
            {
                var step = BuildStep(call);
                var allowed = await RunToolAsync(call, step, ct);
                var resultMessage = allowed
                    ? new ChatMessage(ChatRole.Tool, new List<AIContent> { new FunctionResultContent(call.CallId, step.Output ?? string.Empty) })
                    : new ChatMessage(ChatRole.Tool, new List<AIContent> { new FunctionResultContent(call.CallId, "The user declined this action. Explain this to the user.") });

                messages.Add(resultMessage);
                history.Add(resultMessage);
            }
        }

        // The iteration cap was reached. Give the model one final chance to
        // answer from the tool results it already has, without allowing any
        // more tool calls. If it still produces no text, fall back to a hint.
        var (finalText, _) = await GetAssistantTurnAsync(messages, new ChatOptions(), ct);
        return new AgentTurnResult
        {
            Text = string.IsNullOrWhiteSpace(finalText) ? MaxStepsMessage : finalText,
        };
    }

    private async Task<(string Text, List<FunctionCallContent> Calls)> GetAssistantTurnAsync(
        IList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken ct)
    {
        var text = new StringBuilder();
        var calls = new List<FunctionCallContent>();

        await foreach (var update in _client.GetStreamingResponseAsync(messages, options, ct))
        {
            if (update.Text is { Length: > 0 } delta)
            {
                text.Append(delta);
                await _observer.OnTextDeltaAsync(delta, ct);
            }

            if (update.Contents is null)
                continue;

            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent functionCall)
                    calls.Add(functionCall);
            }
        }

        return (text.ToString(), calls);
    }

    private AgentStep BuildStep(FunctionCallContent call)
    {
        var arguments = call.Arguments as IReadOnlyDictionary<string, object?> ?? call.Arguments?.ToDictionary(kv => kv.Key, kv => kv.Value);
        return new AgentStep
        {
            Name = call.Name,
            Preview = AgentToolRegistry.BuildPreview(call.Name, arguments ?? new Dictionary<string, object?>()),
        };
    }

    private async Task<bool> RunToolAsync(FunctionCallContent call, AgentStep step, CancellationToken ct)
    {
        var tool = _registry.Find(call.Name);
        if (tool is null)
        {
            step.Success = false;
            step.Output = $"Unknown tool '{call.Name}'.";
            await _observer.OnStepFinishedAsync(step, ct);
            return false;
        }

        var requiresConfirmation = _confirmDestructiveActions && tool.Destructive;
        if (requiresConfirmation)
        {
            var allowed = await _observer.OnConfirmDestructiveAsync(step, ct);
            if (!allowed)
            {
                step.Declined = true;
                step.Output = "Declined by user.";
                await _observer.OnStepFinishedAsync(step, ct);
                return false;
            }
        }

        await _observer.OnStepStartingAsync(step, ct);

        try
        {
            var args = new AIFunctionArguments(call.Arguments ?? new Dictionary<string, object?>());
            var result = await tool.Function.InvokeAsync(args, ct);
            step.Output = TrimOutput(result?.ToString());
            step.Success = true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            step.Output = TrimOutput(ex.Message) ?? "The tool failed.";
            step.Success = false;
        }
        finally
        {
            await _observer.OnStepFinishedAsync(step, ct);
        }

        return true;
    }

    private static string? TrimOutput(string? output)
    {
        if (string.IsNullOrEmpty(output))
            return output;

        return output.Length <= MaxStepOutputChars ? output : output[..MaxStepOutputChars] + "\n… (output truncated)";
    }

    private static string BuildSystemPrompt(string snapshot)
    {
        return $"""
            You are WinContainers AI, an assistant built into the WinContainers desktop app for Windows.
            You manage containers, images, volumes, and networks that run through the WSLC runtime.
            You act through the available tools. Never invent tool output; read it from the tool results.

            Current container and image state:
            {snapshot}

            Rules:
            - Use a tool when an action or a lookup is needed. Do not guess container state.
            - After an action, briefly tell the user what you did and why.
            - If a tool returns an error, explain it in plain words and suggest a fix.
            - When the user wants a multi-service setup, write a docker-compose file with the save_compose_file tool and tell them the file path.
            - Call tools only through standard function calling. Never output DSML or other special markup tokens such as <｜DSML｜...｜>; the app removes them.
            - Be concise. Do not use markdown headings. Use short paragraphs or bullet lists.
            """;
    }
}
