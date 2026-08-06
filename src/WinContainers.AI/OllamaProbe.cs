namespace WinContainers.AI;

/// <summary>
/// Detects a local Ollama server so the app can choose the local AI path
/// without asking the user to install anything manually.
/// </summary>
public static class OllamaProbe
{
    private static readonly Uri OllamaEndpoint = new("http://localhost:11434/api/tags");

    public static async Task<bool> IsRunningAsync(CancellationToken ct = default)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
        try
        {
            using var response = await client.GetAsync(OllamaEndpoint, ct);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return false;
        }
    }
}
