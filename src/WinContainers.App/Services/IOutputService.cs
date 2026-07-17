namespace WinContainers_App.Services;

public enum LogLevel { Debug, Info, Warning, Error }

public sealed record OutputWrittenEventArgs(string Message, LogLevel Level);

public interface IOutputService
{
    event EventHandler<OutputWrittenEventArgs>? OutputWritten;
    event EventHandler? OutputCleared;

    string LastOutput { get; }
    IReadOnlyList<(LogLevel Level, string Message)> History { get; }

    void Write(string text);
    void Write(string text, LogLevel level);
    void Clear();
}
