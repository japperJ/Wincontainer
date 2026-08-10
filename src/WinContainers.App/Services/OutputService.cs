using WinContainers.Core.Models;

namespace WinContainers_App.Services;

public sealed class OutputService : IOutputService, IApiRequestLogger
{
    private const int MaxHistoryEntries = 1000;
    private static readonly Lazy<OutputService> _instance = new(() => new());
    public static OutputService Instance => _instance.Value;

    public OutputService() { }

    public event EventHandler<OutputWrittenEventArgs>? OutputWritten;
    public event EventHandler? OutputCleared;

    public string LastOutput { get; private set; } = string.Empty;
    public IReadOnlyList<(LogLevel Level, string Message)> History => _history;
    public bool ApiLoggingEnabled { get; set; }
    public bool RemoteApiLoggingEnabled { get; set; }
    public bool McpEnabled { get; set; } = true;
    public bool AllowRemoteApiAccess { get; set; } = true;
    public bool McpLoggingEnabled { get; set; } = true;
    private readonly List<(LogLevel Level, string Message)> _history = [];

    public void Write(string text) => Write(text, LogLevel.Info);

    public void Write(string text, LogLevel level)
    {
        LastOutput = text;
        // History is an in-memory diagnostic buffer, not an archival log.
        if (_history.Count >= MaxHistoryEntries)
            _history.RemoveAt(0);
        _history.Add((level, text));
        OutputWritten?.Invoke(this, new OutputWrittenEventArgs(text, level));
    }

    public void Clear()
    {
        LastOutput = string.Empty;
        OutputCleared?.Invoke(this, EventArgs.Empty);
    }

    public void LogRequest(string method, string path, string remoteIp, bool isRemote)
    {
        if (!ApiLoggingEnabled)
        {
            return;
        }

        if (RemoteApiLoggingEnabled && !isRemote)
        {
            return;
        }

        Write($"[API][Remote:{isRemote}] {method} {path} from {remoteIp}", LogLevel.Info);
    }

    public void LogMcpRequest(string methodInfo, string remoteIp, bool isRemote, string? outcome)
    {
        if (!McpLoggingEnabled)
        {
            return;
        }

        var suffix = string.IsNullOrWhiteSpace(outcome) ? string.Empty : $" -> {outcome}";
        Write($"[MCP][Remote:{isRemote}] {methodInfo} from {remoteIp}{suffix}", LogLevel.Info);
    }
}
