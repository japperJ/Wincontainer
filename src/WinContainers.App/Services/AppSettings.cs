namespace WinContainers_App.Services;

public sealed class AppSettings
{
    public string? ApiToken { get; set; }

    public bool ApiLoggingEnabled { get; set; }

    public bool RemoteApiLoggingEnabled { get; set; }

    /// <summary>When false, the MCP server is disabled and /mcp returns 404.</summary>
    public bool McpEnabled { get; set; } = true;

    /// <summary>When true, MCP activity is written to the Output window.</summary>
    public bool McpLoggingEnabled { get; set; } = true;

    /// <summary>When false, non-loopback /api requests are rejected with 403.</summary>
    public bool AllowRemoteApiAccess { get; set; } = true;

    public string UpdateChannel { get; set; } = "Stable";

    public DateTimeOffset? LastUpdateCheckUtc { get; set; }

    public string? DeferredUpdateVersion { get; set; }

    /// <summary>AI assistant provider kind, one of AiProviderKind names.</summary>
    public string AiProviderKind { get; set; } = "OpenAiCompatible";

    /// <summary>AI assistant endpoint, e.g. https://api.openai.com/v1 or http://localhost:11434/v1.</summary>
    public string AiEndpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>AI assistant model identifier, e.g. gpt-4o-mini or qwen2.5:3b.</summary>
    public string AiModel { get; set; } = "gpt-4o-mini";

    /// <summary>AI API key, stored DPAPI-protected like ApiToken.</summary>
    public string? AiApiKey { get; set; }

    /// <summary>When true, destructive AI actions require explicit confirmation.</summary>
    public bool AiConfirmDestructiveActions { get; set; } = true;

    /// <summary>When true, destructive MCP actions require a confirmation round-trip before execution.</summary>
    public bool McpDestructiveConfirmationEnabled { get; set; } = true;

    /// <summary>When true, the global AI assistant panel is pinned to the right side of the app.</summary>
    public bool ShowAiPanel { get; set; }

    /// <summary>Preferred width of the AI assistant panel in pixels.</summary>
    public double AiPanelWidth { get; set; } = 380;
}
