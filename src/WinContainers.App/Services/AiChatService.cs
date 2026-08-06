using System.Text;
using Microsoft.Extensions.AI;
using WinContainers.AI;
using WinContainers.Runtime;

namespace WinContainers_App.Services;

/// <summary>
/// Builds the AI agent for the chat page from the persisted settings and
/// provides the Ollama setup flow. The agent talks to the runtime through
/// <see cref="IWslcDriver"/> directly, the same layer the MCP tools use.
/// </summary>
public sealed class AiChatService
{
    /// <summary>Default model pulled on one-click Ollama install.</summary>
    public const string DefaultOllamaModel = "qwen2.5:3b";

    private readonly AppSettingsService _settingsService;
    private readonly IChatClientFactory _clientFactory;
    private readonly IWslcDriver _driver;

    public AiChatService(AppSettingsService settingsService, IChatClientFactory clientFactory, IWslcDriver driver)
    {
        _settingsService = settingsService;
        _clientFactory = clientFactory;
        _driver = driver;
    }

    /// <summary>Where generated compose files are saved.</summary>
    public string ComposeDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "WinContainers", "compose");

    public AiProviderConfig LoadConfig()
    {
        var settings = _settingsService.Load();
        return new AiProviderConfig
        {
            Kind = Enum.TryParse<AiProviderKind>(settings.AiProviderKind, ignoreCase: true, out var kind)
                ? kind
                : AiProviderKind.OpenAiCompatible,
            Endpoint = string.IsNullOrWhiteSpace(settings.AiEndpoint)
                ? "https://api.openai.com/v1"
                : settings.AiEndpoint,
            Model = string.IsNullOrWhiteSpace(settings.AiModel)
                ? "gpt-4o-mini"
                : settings.AiModel,
            ApiKey = settings.AiApiKey,
            ConfirmDestructiveActions = settings.AiConfirmDestructiveActions,
        };
    }

    public void SaveConfig(AiProviderConfig config)
    {
        var settings = _settingsService.Load();
        settings.AiProviderKind = config.Kind.ToString();
        settings.AiEndpoint = config.Endpoint;
        settings.AiModel = config.Model;
        settings.AiApiKey = config.ApiKey;
        settings.AiConfirmDestructiveActions = config.ConfirmDestructiveActions;
        _settingsService.Save(settings);
    }

    public static bool IsConfigured(AiProviderConfig config) =>
        !string.IsNullOrWhiteSpace(config.Endpoint) && !string.IsNullOrWhiteSpace(config.Model);

    public ContainerAgent CreateAgent(IAgentObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        var config = LoadConfig();
        var client = _clientFactory.Create(config);
        var compose = new ComposeFileSaver(ComposeDirectory);
        var registry = new AgentToolRegistry(_driver, compose);
        var snapshotBuilder = new ContainerSnapshotBuilder(_driver);

        return new ContainerAgent(client, registry, observer, snapshotBuilder.BuildAsync, config.ConfirmDestructiveActions);
    }

    public Task<bool> IsOllamaRunningAsync(CancellationToken ct = default) => OllamaProbe.IsRunningAsync(ct);

    /// <summary>
    /// Installs the Ollama container (image pull + detached run with a
    /// persistent volume) and pulls the default model into it.
    /// </summary>
    public async Task<string> InstallOllamaAsync(CancellationToken ct)
    {
        var sb = new StringBuilder();

        sb.AppendLine(await _driver.PullImageAsync("ollama/ollama", ct));
        sb.AppendLine(await _driver.RunContainerAsync(
            "ollama/ollama",
            name: "ollama",
            ports: ["11434:11434"],
            volumes: ["ollama-data:/root/.ollama"],
            ct: ct));

        sb.AppendLine(await _driver.ExecCommandAsync("ollama", $"ollama pull {DefaultOllamaModel}", ct));
        return sb.ToString().Trim();
    }
}
