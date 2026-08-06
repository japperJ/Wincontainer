namespace WinContainers.AI;

/// <summary>
/// The outcome of one agent turn (a single user message).
/// </summary>
public sealed record AgentTurnResult
{
    /// <summary>The final assistant reply text, or null when the turn produced none.</summary>
    public string? Text { get; init; }

    /// <summary>True when the turn was stopped by the user before completing.</summary>
    public bool Cancelled { get; init; }
}
