namespace WinContainers.AI;

/// <summary>
/// A single tool invocation performed by the agent. The UI renders it as a step card.
/// </summary>
public sealed class AgentStep
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    /// <summary>The tool name, e.g. stop_container.</summary>
    public string Name { get; init; } = "";

    /// <summary>A short human-readable description of the action, e.g. "Stop container 'web'".</summary>
    public string Preview { get; init; } = "";

    /// <summary>The raw tool output (or error message).</summary>
    public string? Output { get; set; }

    /// <summary>True when the tool completed without error.</summary>
    public bool Success { get; set; }

    /// <summary>True when the user declined a destructive action.</summary>
    public bool Declined { get; set; }
}
