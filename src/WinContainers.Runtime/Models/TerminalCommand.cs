using System.Collections.ObjectModel;

namespace WinContainers.Runtime.Models;

public enum CommandParamType
{
    Text,
    ContainerId,
    ImageName,
    RestartPolicy,
    Format
}

public sealed class TerminalCommand
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool HasOutput { get; set; } = true;
    public List<CommandParamDef> Parameters { get; set; } = new();
}

public sealed class CommandParamDef
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public CommandParamType Type { get; set; }
    public bool Required { get; set; } = true;
}

public sealed class TerminalCategory
{
    public string Name { get; set; } = string.Empty;
    public ObservableCollection<TerminalCommand> Commands { get; set; } = new();
}

public sealed class TerminalHistoryEntry
{
    public string ScriptName { get; set; } = string.Empty;
    public Dictionary<string, string> Parameters { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public bool IsFavorite { get; set; }
    public string? Output { get; set; }
    public string Summary => $"{ScriptName} ({string.Join(", ", Parameters.Values)})";
}
