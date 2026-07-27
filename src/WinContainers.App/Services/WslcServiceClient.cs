using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using WinContainers.Core;
using WinContainers.Core.Models;

namespace WinContainers_App.Services;

public sealed class WslcServiceClient : IWslcServiceClient
{
    private readonly HttpClient _http = HttpClientTimeouts.Create(HttpClientTimeouts.ServiceTimeout);
    private readonly string _baseUrl;
    private readonly IOutputService _output;

    public WslcServiceClient(string baseUrl, IOutputService output)
    {
        _baseUrl = baseUrl.TrimEnd('/');
        _output = output;
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        var token = ServiceEndpointResolver.ResolveToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return;
        }

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token);
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/health");
            ApplyAuth(request);
            var (_, isSuccessStatusCode) = await SendAndReadBodyAsync(request);
            return isSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WslcServiceClient] IsHealthyAsync failed: {ex}");
            return false;
        }
    }

    public async Task<string> GetVersionAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/runtime/version");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "version") ?? "(unknown)";
    }

    public async Task<string> GetContainersAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/containers");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> StartContainerAsync(string id)
        => await PostCommandAsync($"/api/containers/{id}/start");

    public async Task<string> StopContainerAsync(string id)
        => await PostCommandAsync($"/api/containers/{id}/stop");

    public async Task<string> RestartContainerAsync(string id)
        => await PostCommandAsync($"/api/containers/{id}/restart");

    public async Task<string> RenameContainerAsync(string id, string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/containers/{id}/rename");
        ApplyAuth(request);
        request.Content = JsonContent.Create(new { name });
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> InspectContainerAsync(string id)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/containers/{id}/inspect");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> RemoveContainerAsync(string id)
        => await DeleteCommandAsync($"/api/containers/{id}");

    public async Task<string> GetContainerLogsAsync(string id, int tail = 500)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/containers/{id}/logs?tail={tail}");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> GetImagesAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/images");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> PullImageAsync(string image)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/images/pull");
        ApplyAuth(request);
        request.Content = JsonContent.Create(new { image });
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/containers/run");
        ApplyAuth(request);
        request.Content = JsonContent.Create(new { image, name, ports, volumes, env });
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> RemoveImageAsync(string id)
        => await DeleteCommandAsync($"/api/images/{id}");

    public async Task<string> GetVolumesAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/volumes");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> CreateVolumeAsync(string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/volumes");
        ApplyAuth(request);
        request.Content = JsonContent.Create(new { name });
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> RemoveVolumeAsync(string name)
        => await DeleteCommandAsync($"/api/volumes/{name}");

    public async Task<string> GetNetworksAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/api/networks");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> CreateNetworkAsync(string name)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/networks");
        ApplyAuth(request);
        request.Content = JsonContent.Create(new { name });
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    public async Task<string> RemoveNetworkAsync(string name)
        => await DeleteCommandAsync($"/api/networks/{name}");

    public async Task<string> ExecContainerAsync(string id, string command, bool useShell = false, string? shell = null)
    {
        var payload = JsonSerializer.Serialize(new { command, useShell, shell });
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/api/containers/{id}/exec")
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    private async Task<(string Body, bool IsSuccessStatusCode)> SendAndReadBodyAsync(HttpRequestMessage request)
    {
        if (ShouldLogRequest(request.RequestUri))
        {
            _output.Write($"[API][Request] {request.Method} {request.RequestUri}", LogLevel.Debug);
        }

        using var response = await _http.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();

        if (ShouldLogRequest(request.RequestUri))
        {
            var preview = body.Length > 1000 ? $"{body[..1000]}..." : body;
            _output.Write($"[API][Response] {(int)response.StatusCode} {response.ReasonPhrase}: {preview}", LogLevel.Debug);
        }

        return (body, response.IsSuccessStatusCode);
    }

    private bool ShouldLogRequest(Uri? requestUri)
    {
        if (!_output.ApiLoggingEnabled)
        {
            return false;
        }

        if (!_output.RemoteApiLoggingEnabled)
        {
            return true;
        }

        return requestUri is not null && !requestUri.IsLoopback;
    }

    private async Task<string> PostCommandAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}{path}");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    private async Task<string> DeleteCommandAsync(string path)
    {
        using var request = new HttpRequestMessage(HttpMethod.Delete, $"{_baseUrl}{path}");
        ApplyAuth(request);
        var (json, _) = await SendAndReadBodyAsync(request);
        return ExtractField(json, "output") ?? json;
    }

    private static string? ExtractField(string json, string field)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.TryGetProperty(field, out var el) &&
                el.ValueKind == JsonValueKind.String)
                return el.GetString();
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WslcServiceClient] ExtractField('{field}') parse failed: {ex.Message}");
        }
        return null;
    }
}
