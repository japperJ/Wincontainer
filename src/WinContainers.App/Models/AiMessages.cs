using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using WinContainers.AI;

namespace WinContainers_App.Models;

/// <summary>A user message bubble.</summary>
public sealed class UserChatMessage : ObservableObject
{
    private string _text;

    public UserChatMessage(string text) => _text = text;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}

/// <summary>
/// An assistant message bubble. Text streams in while the turn runs; when the
/// turn finishes the page renders the markdown into the bubble's Inlines.
/// </summary>
public sealed class AssistantChatMessage : ObservableObject
{
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }

    /// <summary>True once the turn finished and the text is final.</summary>
    public bool IsComplete { get; set; }

    /// <summary>Managed by the page so each bubble renders its markdown once.</summary>
    public bool MarkdownRendered { get; set; }
}

/// <summary>
/// A step card showing one tool invocation the agent performed, with live
/// status, preview, and (when finished) its output.
/// </summary>
public sealed class StepCardMessage : ObservableObject
{
    private bool _isRunning;
    private bool _isSuccess;
    private bool _isDeclined;
    private string? _output;

    public StepCardMessage(AgentStep step)
    {
        Id = step.Id;
        Preview = step.Preview;
    }

    public string Id { get; }

    public string Preview { get; }

    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(Glyph));
            }
        }
    }

    public bool IsSuccess
    {
        get => _isSuccess;
        set
        {
            if (SetProperty(ref _isSuccess, value))
            {
                OnPropertyChanged(nameof(Glyph));
            }
        }
    }

    public bool IsDeclined
    {
        get => _isDeclined;
        set
        {
            if (SetProperty(ref _isDeclined, value))
            {
                OnPropertyChanged(nameof(Glyph));
            }
        }
    }

    public string? Output
    {
        get => _output;
        set
        {
            if (SetProperty(ref _output, value))
            {
                OnPropertyChanged(nameof(HasOutput));
                OnPropertyChanged(nameof(OutputVisibility));
            }
        }
    }

    public bool HasOutput => !string.IsNullOrWhiteSpace(Output);

    public Visibility OutputVisibility => HasOutput ? Visibility.Visible : Visibility.Collapsed;

    public string Glyph => IsDeclined
        ? "\uE711"   // Cancel
        : IsRunning
            ? "\uE895" // Sync
            : IsSuccess
                ? "\uE73E" // CheckMark
                : "\uE783"; // Error
}

/// <summary>A small centered status line (e.g. turn cancelled).</summary>
public sealed class SystemChatMessage : ObservableObject
{
    public SystemChatMessage(string text) => Text = text;

    public string Text { get; }
}

/// <summary>
/// A centered status line shown while the agent waits before retrying a turn
/// after a transient provider error. Text updates with a live countdown.
/// </summary>
public sealed class RetryWaitChatMessage : ObservableObject
{
    private string _text = string.Empty;

    public string Text
    {
        get => _text;
        set => SetProperty(ref _text, value);
    }
}

/// <summary>
/// A chat indicator shown while the assistant is working. It is added to the
/// message list when a turn starts and removed when the turn finishes, so it
/// always sits below the streaming answer and step cards.
/// </summary>
public sealed class ThinkingChatMessage
{
}
