namespace WinContainers.AI;

/// <summary>
/// Receives agent progress so the UI can stream text, render step cards, and
/// confirm destructive actions. Implemented by the host application.
/// </summary>
public interface IAgentObserver
{
    /// <summary>Called as the assistant reply streams in, usually one word or token at a time.</summary>
    Task OnTextDeltaAsync(string delta, CancellationToken ct);

    /// <summary>Called before a tool is invoked so the UI can show a step card.</summary>
    Task OnStepStartingAsync(AgentStep step, CancellationToken ct);

    /// <summary>Called after a tool finishes (success, failure, or declined).</summary>
    Task OnStepFinishedAsync(AgentStep step, CancellationToken ct);

    /// <summary>
    /// Called before a destructive action is invoked. Return true to allow it,
    /// false to decline. The exact action is shown in <paramref name="step"/>.
    /// </summary>
    Task<bool> OnConfirmDestructiveAsync(AgentStep step, CancellationToken ct);
}
