using Microsoft.Extensions.AI;
using OpenAI;
using System.ClientModel;

namespace WinContainers.AI;

/// <summary>
/// Creates model chat clients from an <see cref="AiProviderConfig"/>.
/// </summary>
public interface IChatClientFactory
{
    IChatClient Create(AiProviderConfig config);
}

/// <summary>
/// Creates clients for any OpenAI-compatible endpoint, including local Ollama
/// servers that expose the /v1 OpenAI-compatible API.
/// </summary>
public sealed class OpenAiCompatibleChatClientFactory : IChatClientFactory
{
    public IChatClient Create(AiProviderConfig config)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Endpoint);
        ArgumentException.ThrowIfNullOrWhiteSpace(config.Model);

        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(config.Endpoint) };
        var credential = new ApiKeyCredential(config.ApiKey ?? "local");

        return new OpenAIClient(credential, clientOptions)
            .GetChatClient(config.Model)
            .AsIChatClient();
    }
}
