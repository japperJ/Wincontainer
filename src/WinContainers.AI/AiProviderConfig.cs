namespace WinContainers.AI;

/// <summary>
/// The kind of model backend the AI assistant connects to.
/// </summary>
public enum AiProviderKind
{
    /// <summary>Any OpenAI-compatible REST endpoint (OpenAI, Azure OpenAI, local gateways).</summary>
    OpenAiCompatible = 0,

    /// <summary>A local Ollama server (its OpenAI-compatible endpoint is used).</summary>
    Ollama = 1,
}

/// <summary>
/// Configuration for connecting the AI assistant to a model backend.
/// </summary>
public sealed class AiProviderConfig
{
    /// <summary>Which backend kind this configuration represents.</summary>
    public AiProviderKind Kind { get; set; } = AiProviderKind.OpenAiCompatible;

    /// <summary>Base endpoint including the /v1 path, e.g. https://api.openai.com/v1 or http://localhost:11434/v1.</summary>
    public string Endpoint { get; set; } = "https://api.openai.com/v1";

    /// <summary>Model identifier, e.g. gpt-4o-mini or qwen2.5:3b.</summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>Optional API key. Not needed for local Ollama.</summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// When true, destructive actions (removing containers, images, volumes,
    /// or networks) require explicit user confirmation.
    /// </summary>
    public bool ConfirmDestructiveActions { get; set; } = true;
}
