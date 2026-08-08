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
    private const string NoUsableReplyMessage =
        "The model did not return a usable reply. Try again or check the provider configuration.";
    private const string ContinuationPrompt =
        "Your previous reply was cut off before you finished. Continue now: " +
        "make the tool call you intended, or give your final answer.";
    private const string NarrationContinuationPrompt =
        "Your previous reply described an action but made no tool call. " +
        "Do not describe what you are about to do. Make the tool call you intended now, " +
        "or give your final answer if you already have the information you need.";

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

            var reply = await GetAssistantTurnAsync(messages, options, ct);

            if (reply.Calls.Count == 0)
            {
                var cleaned = AgentTextCleaner.StripSpecialTokens(reply.Text);
                if (cleaned.Length > 0 && !reply.Interrupted)
                {
                    return new AgentTurnResult { Text = cleaned };
                }

                if (cleaned.Length > 0)
                {
                    // The reply was cut off mid-thought or only narrated an
                    // action without taking it: the stream was truncated, the
                    // model left an unclosed/unparseable tool-call marker, or
                    // it announced "Let me test ..." and then stopped. Keep the
                    // partial reply as context and nudge the model to continue,
                    // so the intended tool call or final answer completes
                    // instead of stopping the turn.
                    var partial = new ChatMessage(ChatRole.Assistant, new List<AIContent> { new TextContent(cleaned) });
                    messages.Add(partial);
                    history.Add(partial);
                    messages.Add(new ChatMessage(ChatRole.User, reply.NarrationOnly ? NarrationContinuationPrompt : ContinuationPrompt));
                    continue;
                }

                // The reply was empty or contained only unsupported markers
                // (for example DSML that could not be parsed into a tool call).
                // Give the model one final chance without tools so it can
                // answer in plain text instead of stopping the turn.
                var (noToolText, _, _, _) = await GetAssistantTurnAsync(messages, new ChatOptions(), ct);
                var finalCleaned = AgentTextCleaner.StripSpecialTokens(noToolText);
                return new AgentTurnResult
                {
                    Text = string.IsNullOrWhiteSpace(finalCleaned) ? NoUsableReplyMessage : finalCleaned,
                };
            }

            var assistantMessage = new ChatMessage(ChatRole.Assistant, new List<AIContent>());
            if (reply.Text.Length > 0)
                assistantMessage.Contents.Add(new TextContent(reply.Text));
            foreach (var call in reply.Calls)
                assistantMessage.Contents.Add(call);

            messages.Add(assistantMessage);
            history.Add(assistantMessage);

            foreach (var call in reply.Calls)
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
        var (finalText, _, _, _) = await GetAssistantTurnAsync(messages, new ChatOptions(), ct);
        return new AgentTurnResult
        {
            Text = string.IsNullOrWhiteSpace(finalText) ? MaxStepsMessage : finalText,
        };
    }

    private async Task<AssistantReply> GetAssistantTurnAsync(
        IList<ChatMessage> messages,
        ChatOptions options,
        CancellationToken ct)
    {
        var text = new StringBuilder();
        var calls = new List<FunctionCallContent>();
        ChatFinishReason? finishReason = null;

        await foreach (var update in _client.GetStreamingResponseAsync(messages, options, ct))
        {
            if (update.Text is { Length: > 0 } delta)
            {
                text.Append(delta);
                await _observer.OnTextDeltaAsync(delta, ct);
            }

            if (update.FinishReason is not null)
            {
                finishReason = update.FinishReason;
            }

            if (update.Contents is null)
                continue;

            foreach (var content in update.Contents)
            {
                if (content is FunctionCallContent functionCall)
                    calls.Add(functionCall);
            }
        }

        // Some models emit tool calls as DSML markers inside the text instead
        // of standard function-calling content. Recover them so the turn can
        // continue instead of stopping on raw markup.
        var raw = text.ToString();
        var dsmlCalls = AgentTextCleaner.ExtractToolCalls(raw, out var cleanedText, out var droppedBlocks);
        if (dsmlCalls.Count > 0)
        {
            calls.AddRange(dsmlCalls);
        }

        // A reply is "interrupted" when the model was clearly cut off, left a
        // tool call it could not finish, or only narrated an action it never
        // took: the stream reported a truncation finish reason, a tool-call
        // marker is left unclosed, every DSML block failed to parse, or the
        // reply ends with narration such as "Let me test ...". Such a reply is
        // not a final answer; the agent must nudge the model to continue.
        var truncated = finishReason is { } r
            && (r == ChatFinishReason.Length || r == ChatFinishReason.ContentFilter);
        var dsmlInterrupted = calls.Count == 0
            && (droppedBlocks > 0 || AgentTextCleaner.HasUnclosedToolCallMarker(raw));
        var narrationOnly = calls.Count == 0 && AgentTextCleaner.IsNarrationOnlyIncomplete(cleanedText);

        var interrupted = truncated || dsmlInterrupted || narrationOnly;

        return new AssistantReply(cleanedText, calls, interrupted, narrationOnly);
    }

    private sealed record AssistantReply(string Text, List<FunctionCallContent> Calls, bool Interrupted, bool NarrationOnly);

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
            - Never end a reply with a plan, a promise, or a colon. When you say you will do something, do it in the same reply by making the tool call.
            - A reply that only announces an action (for example "Let me test ...", "I'll check ...") is a failure. End every reply either with the tool call you announced or with the final answer.
            - Finish each reply with a complete sentence and a final answer. Do not leave a sentence unfinished.
            - After an action, briefly tell the user what you did and why.
            - If a tool returns an error, explain it in plain words and suggest a fix.
            - When the user wants a multi-service setup, write a docker-compose file with the save_compose_file tool and tell them the file path.
            - Call tools only through standard function calling. Never output DSML or other special markup tokens such as <｜DSML｜...｜>; the app removes them.
            - Be concise. Do not use markdown headings. Use short paragraphs or bullet lists.
            """;
    }
}
