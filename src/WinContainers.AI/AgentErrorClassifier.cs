namespace WinContainers.AI;

/// <summary>
/// Classifies agent exceptions so the caller can decide whether a transient
/// provider error (for example HTTP 503 "chat_admission_busy") is worth
/// retrying after a short pause.
/// </summary>
public static class AgentErrorClassifier
{
    private static readonly string[] RetryableMarkers =
    [
        "429",
        "502",
        "503",
        "504",
        "chat_admission_busy",
        "rate limit",
        "too many requests",
        "service unavailable",
        "server busy",
        "server_error",
        "temporarily unavailable",
        "overloaded",
    ];

    /// <summary>
    /// Returns true when the exception, or any of its inner exceptions, looks
    /// like a transient provider error worth retrying.
    /// </summary>
    public static bool IsRetryable(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);

        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (ContainsAnyMarker(current.Message))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsAnyMarker(string text)
    {
        foreach (var marker in RetryableMarkers)
        {
            if (text.Contains(marker, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
