using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.AI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel.DataTransfer;
using WinContainers.AI;
using WinContainers_App.Models;
using WinContainers_App.Services;
using LogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

/// <summary>
/// Drives the AI chat page: builds the agent, streams assistant text into the
/// message list, renders tool calls as step cards, confirms destructive
/// actions, and persists the conversation.
/// </summary>
public sealed class AiViewModel : ViewModelBase
{
    private readonly AiChatService _ai;
    private readonly IDialogService _dialogs;
    private readonly IOutputService _output;
    private readonly ChatHistoryStore _history;
    private readonly DispatcherQueue _dispatcher;

    public ObservableCollection<object> Messages { get; } = [];

    private CancellationTokenSource? _cts;
    private AssistantChatMessage? _assistantBubble;
    private readonly Dictionary<string, StepCardMessage> _stepCards = new();
    private ThinkingChatMessage? _thinkingItem;

    private bool _isBusy;
    private string? _input;
    private string _providerStatus = "Not configured";
    private bool _hasConfiguredProvider;

    public AiViewModel(AiChatService ai, IDialogService dialogs, IOutputService output, ChatHistoryStore history)
    {
        _ai = ai;
        _dialogs = dialogs;
        _output = output;
        _history = history;
        _dispatcher = DispatcherQueue.GetForCurrentThread() ?? App.DispatcherQueue;
    }

    public bool IsInitialized { get; private set; }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanSend));
                OnPropertyChanged(nameof(IsCancellable));
                OnPropertyChanged(nameof(CanClear));
                UpdateThinkingIndicator();
            }
        }
    }

    public string? Input
    {
        get => _input;
        set
        {
            if (SetProperty(ref _input, value))
            {
                OnPropertyChanged(nameof(CanSend));
            }
        }
    }

    public bool CanSend => !IsBusy && !string.IsNullOrWhiteSpace(Input);
    public bool IsCancellable => IsBusy;
    public bool CanClear => Messages.Count > 0 && !IsBusy;

    public string ProviderStatus
    {
        get => _providerStatus;
        private set => SetProperty(ref _providerStatus, value);
    }

    public bool HasConfiguredProvider
    {
        get => _hasConfiguredProvider;
        private set => SetProperty(ref _hasConfiguredProvider, value);
    }

    public void Initialize()
    {
        if (IsInitialized)
        {
            return;
        }

        IsInitialized = true;
        RefreshStatus();

        foreach (var record in _history.Load())
        {
            if (record.Role == "user")
            {
                Messages.Add(new UserChatMessage(record.Text));
            }
            else
            {
                Messages.Add(new AssistantChatMessage { Text = record.Text, IsComplete = true });
            }
        }

        OnPropertyChanged(nameof(CanClear));
    }

    public void RefreshStatus()
    {
        var config = _ai.LoadConfig();
        HasConfiguredProvider = AiChatService.IsConfigured(config);
        ProviderStatus = BuildStatusText(config);
    }

    public void ApplyConfig(AiProviderConfig config)
    {
        _ai.SaveConfig(config);
        RefreshStatus();
    }

    public async Task SendAsync()
    {
        if (!CanSend)
        {
            return;
        }

        var text = Input!.Trim();
        Input = null;

        var history = BuildHistory();
        Messages.Add(new UserChatMessage(text));

        IsBusy = true;
        _cts = new CancellationTokenSource();

        ContainerAgent agent;
        try
        {
            agent = _ai.CreateAgent(new AgentUiObserver(this, _dispatcher));
        }
        catch (Exception ex)
        {
            _output.Write($"AI setup failed: {ex.Message}", LogLevel.Error);
            AddSystemMessage($"The AI assistant could not start: {ex.Message}");
            _cts.Dispose();
            _cts = null;
            IsBusy = false;
            return;
        }

        try
        {
            var result = await Task.Run(() => agent.RunTurnAsync(history, text, _cts.Token));
            FinishTurn(result);
            SaveHistory(history);
        }
        catch (OperationCanceledException)
        {
            FinishTurn(new AgentTurnResult { Cancelled = true });
        }
        catch (Exception ex)
        {
            _output.Write($"AI turn failed: {ex.Message}", LogLevel.Error);
            AddSystemMessage($"The AI assistant ran into a problem: {ex.Message}");
            FinishTurn(new AgentTurnResult());
        }
        finally
        {
            _cts?.Dispose();
            _cts = null;
            IsBusy = false;
        }
    }

    public void CancelTurn() => _cts?.Cancel();

    public void ClearConversation()
    {
        if (IsBusy)
        {
            return;
        }

        Messages.Clear();
        _stepCards.Clear();
        _assistantBubble = null;
        _thinkingItem = null;
        _history.Save([]);
        OnPropertyChanged(nameof(CanClear));
    }

    public void CopyAsMarkdown()
    {
        var sb = new StringBuilder();
        foreach (var message in Messages)
        {
            switch (message)
            {
                case UserChatMessage user:
                    sb.AppendLine($"**You:** {user.Text}");
                    sb.AppendLine();
                    break;
                case AssistantChatMessage assistant:
                    sb.AppendLine($"**AI:** {assistant.Text}");
                    sb.AppendLine();
                    break;
                case StepCardMessage step:
                    sb.AppendLine($"- {step.Preview}");
                    break;
            }
        }

        var package = new DataPackage { RequestedOperation = DataPackageOperation.Copy };
        package.SetText(sb.ToString().TrimEnd());
        Clipboard.SetContent(package);
    }

    public Task<bool> DetectOllamaAsync() => _ai.IsOllamaRunningAsync();

    public Task<string> InstallOllamaAsync(CancellationToken ct) => _ai.InstallOllamaAsync(ct);

    internal void AppendStreamingDelta(string delta)
    {
        _assistantBubble ??= new AssistantChatMessage();
        if (_assistantBubble.Text.Length == 0)
        {
            Messages.Add(_assistantBubble);
            EnsureThinkingLast();
            OnPropertyChanged(nameof(CanClear));
        }

        _assistantBubble.Text += delta;
    }

    internal void StepStarting(AgentStep step)
    {
        FinalizeStreamingBubble();
        var card = new StepCardMessage(step) { IsRunning = true };
        _stepCards[step.Id] = card;
        Messages.Add(card);
        EnsureThinkingLast();
        OnPropertyChanged(nameof(CanClear));
    }

    internal void StepFinished(AgentStep step)
    {
        if (!_stepCards.TryGetValue(step.Id, out var card))
        {
            return;
        }

        card.IsRunning = false;
        card.IsSuccess = step.Success;
        card.IsDeclined = step.Declined;
        card.Output = step.Output;
    }

    /// <summary>
    /// Closes the current streaming bubble so any later text (for example the
    /// final answer after tool steps) starts a new bubble below the step cards.
    /// </summary>
    private void FinalizeStreamingBubble()
    {
        if (_assistantBubble is null)
        {
            return;
        }

        _assistantBubble.IsComplete = true;
        _assistantBubble = null;
    }

    /// <summary>
    /// Adds or removes the thinking indicator so it is present exactly while a
    /// turn runs. It is always the last item in the message list.
    /// </summary>
    private void UpdateThinkingIndicator()
    {
        if (IsBusy)
        {
            if (_thinkingItem is null)
            {
                _thinkingItem = new ThinkingChatMessage();
                Messages.Add(_thinkingItem);
            }

            return;
        }

        if (_thinkingItem is not null)
        {
            Messages.Remove(_thinkingItem);
            _thinkingItem = null;
        }
    }

    /// <summary>Moves the thinking indicator below any new streaming or step content.</summary>
    private void EnsureThinkingLast()
    {
        if (_thinkingItem is not null && !ReferenceEquals(Messages[^1], _thinkingItem))
        {
            Messages.Remove(_thinkingItem);
            Messages.Add(_thinkingItem);
        }
    }

    internal async Task<bool> ConfirmDestructiveAsync(AgentStep step)
    {
        var result = await _dialogs.ShowConfirmAsync(
            "Confirm destructive action",
            $"The AI assistant wants to run this action. It cannot be undone.\n\n{step.Preview}",
            primaryButtonText: "Allow",
            closeButtonText: "Deny");
        return result == ContentDialogResult.Primary;
    }

    private void FinishTurn(AgentTurnResult result)
    {
        var rawText = result.Text ?? _assistantBubble?.Text ?? string.Empty;
        var cleaned = AgentTextCleaner.StripSpecialTokens(rawText);

        if (!string.IsNullOrEmpty(cleaned))
        {
            if (_assistantBubble is not null)
            {
                _assistantBubble.Text = cleaned;
                _assistantBubble.IsComplete = true;
                _assistantBubble = null;
            }
            else
            {
                Messages.Add(new AssistantChatMessage { Text = cleaned, IsComplete = true });
            }

            OnPropertyChanged(nameof(CanClear));
        }
        else if (_assistantBubble is not null)
        {
            Messages.Remove(_assistantBubble);
            _assistantBubble = null;
            OnPropertyChanged(nameof(CanClear));
        }

        if (result.Cancelled)
        {
            AddSystemMessage("Turn cancelled.");
        }
        else if (string.IsNullOrEmpty(cleaned) && !string.IsNullOrEmpty(rawText))
        {
            AddSystemMessage("The model reply contained only unsupported special tokens. Check the model or provider configuration.");
        }
    }

    private void AddSystemMessage(string text) => Messages.Add(new SystemChatMessage(text));

    /// <summary>
    /// Removes the streaming bubble and step cards of a failed turn attempt so
    /// a retry starts with a clean message list.
    /// </summary>
    private void RemoveTransientTurnArtifacts()
    {
        if (_assistantBubble is not null)
        {
            Messages.Remove(_assistantBubble);
            _assistantBubble = null;
        }

        foreach (var card in _stepCards.Values.ToList())
        {
            Messages.Remove(card);
        }

        _stepCards.Clear();
    }

    /// <summary>
    /// Shows a live countdown in the chat while the agent waits before retrying
    /// a turn after a transient provider error. The status line stays in the
    /// chat as a record of the retry.
    /// </summary>
    internal async Task ShowRetryWaitAsync(int seconds, int attempt, int maxAttempts, CancellationToken ct)
    {
        RemoveTransientTurnArtifacts();

        var wait = new RetryWaitChatMessage();
        Messages.Add(wait);
        EnsureThinkingLast();
        OnPropertyChanged(nameof(CanClear));

        try
        {
            for (var i = seconds; i > 0; i--)
            {
                wait.Text = i == 1
                    ? $"The provider is busy. Waiting 1 second before retry (attempt {attempt} of {maxAttempts})..."
                    : $"The provider is busy. Waiting {i} seconds before retry (attempt {attempt} of {maxAttempts})...";
                await Task.Delay(TimeSpan.FromSeconds(1), ct);
            }

            wait.Text = $"Retrying now (attempt {attempt} of {maxAttempts})...";
        }
        catch
        {
            Messages.Remove(wait);
            throw;
        }
    }

    private List<ChatMessage> BuildHistory()
    {
        var history = new List<ChatMessage>();
        foreach (var message in Messages)
        {
            switch (message)
            {
                case UserChatMessage user:
                    history.Add(new ChatMessage(ChatRole.User, user.Text));
                    break;
                case AssistantChatMessage assistant when assistant.IsComplete:
                    history.Add(new ChatMessage(ChatRole.Assistant, assistant.Text));
                    break;
            }
        }

        return history;
    }

    private void SaveHistory(IList<ChatMessage> history)
    {
        var records = history
            .Where(m => m.Role == ChatRole.User || m.Role == ChatRole.Assistant)
            .Where(m => !string.IsNullOrEmpty(m.Text))
            .Select(m => new ChatRecord(m.Role == ChatRole.User ? "user" : "assistant", m.Text!))
            .ToList();
        _history.Save(records);
    }

    private static string BuildStatusText(AiProviderConfig config)
    {
        if (!AiChatService.IsConfigured(config))
        {
            return "Not configured";
        }

        return config.Kind == AiProviderKind.Ollama
            ? $"Ollama (local) · {config.Model}"
            : $"{config.Model} · {config.Endpoint}";
    }

    /// <summary>
    /// Marshals agent callbacks to the UI thread and shows the destructive
    /// confirmation dialog there before the agent continues.
    /// </summary>
    private sealed class AgentUiObserver : IAgentObserver
    {
        private readonly AiViewModel _vm;
        private readonly DispatcherQueue _dispatcher;

        public AgentUiObserver(AiViewModel vm, DispatcherQueue dispatcher)
        {
            _vm = vm;
            _dispatcher = dispatcher;
        }

        public Task OnTextDeltaAsync(string delta, CancellationToken ct)
            => RunOnUiAsync(() => _vm.AppendStreamingDelta(delta), ct);

        public Task OnStepStartingAsync(AgentStep step, CancellationToken ct)
            => RunOnUiAsync(() => _vm.StepStarting(step), ct);

        public Task OnStepFinishedAsync(AgentStep step, CancellationToken ct)
            => RunOnUiAsync(() => _vm.StepFinished(step), ct);

        public Task<bool> OnConfirmDestructiveAsync(AgentStep step, CancellationToken ct)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => tcs.TrySetResult(false));

            if (!_dispatcher.TryEnqueue(() => _ = ConfirmAsync(step, tcs)))
            {
                tcs.TrySetResult(false);
            }

            return tcs.Task;
        }

        public Task OnRetryWaitAsync(int seconds, int attempt, int maxAttempts, CancellationToken ct)
        {
            if (_dispatcher.HasThreadAccess)
            {
                return _vm.ShowRetryWaitAsync(seconds, attempt, maxAttempts, ct);
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

            if (!_dispatcher.TryEnqueue(() => _ = RunRetryWaitAsync(seconds, attempt, maxAttempts, ct, tcs)))
            {
                tcs.TrySetCanceled();
            }

            return tcs.Task;
        }

        private async Task RunRetryWaitAsync(int seconds, int attempt, int maxAttempts, CancellationToken ct, TaskCompletionSource tcs)
        {
            try
            {
                await _vm.ShowRetryWaitAsync(seconds, attempt, maxAttempts, ct);
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        }

        private async Task ConfirmAsync(AgentStep step, TaskCompletionSource<bool> tcs)
        {
            try
            {
                tcs.TrySetResult(await _vm.ConfirmDestructiveAsync(step));
            }
            catch
            {
                tcs.TrySetResult(false);
            }
        }

        private async Task RunOnUiAsync(Action action, CancellationToken ct)
        {
            if (_dispatcher.HasThreadAccess)
            {
                action();
                return;
            }

            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = ct.Register(() => tcs.TrySetCanceled(ct));

            if (!_dispatcher.TryEnqueue(() =>
                {
                    try
                    {
                        action();
                        tcs.TrySetResult();
                    }
                    catch (Exception ex)
                    {
                        tcs.TrySetException(ex);
                    }
                }))
            {
                tcs.TrySetCanceled();
            }

            await tcs.Task;
        }
    }
}
