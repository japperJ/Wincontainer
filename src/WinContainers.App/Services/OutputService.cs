namespace WinContainers_App.Services;

public sealed class OutputService : IOutputService
{
    private static readonly Lazy<OutputService> _instance = new(() => new());
    public static OutputService Instance => _instance.Value;

    public OutputService() { }

    public event EventHandler<OutputWrittenEventArgs>? OutputWritten;
    public event EventHandler? OutputCleared;

    public string LastOutput { get; private set; } = string.Empty;
    public IReadOnlyList<(LogLevel Level, string Message)> History => _history;
    public bool ApiLoggingEnabled { get; set; }
    public bool RemoteApiLoggingEnabled { get; set; }
    private readonly List<(LogLevel Level, string Message)> _history = [];

    public void Write(string text) => Write(text, LogLevel.Info);

    public void Write(string text, LogLevel level)
    {
        LastOutput = text;
        _history.Add((level, text));
        OutputWritten?.Invoke(this, new OutputWrittenEventArgs(text, level));
    }

    public void Clear()
    {
        LastOutput = string.Empty;
        OutputCleared?.Invoke(this, EventArgs.Empty);
    }
}
