using System.Text;
using System.Text.Json;

namespace WinContainers.Tests.Ui;

public sealed class WinAppDriverSession : IDisposable
{
    private readonly HttpClient _http;
    private readonly string _sessionId;

    public WinAppDriverSession(string appExePath)
    {
        _http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:4723") };

        var escapedPath = appExePath.Replace("\\", "\\\\");
        var json = "{\"desiredCapabilities\":{\"app\":\"" + escapedPath + "\",\"platformName\":\"Windows\",\"deviceName\":\"WindowsPC\"}}";

        Console.Error.WriteLine($"Sending JSON: {json}");

        var response = _http.PostAsync("/session",
            new StringContent(json, Encoding.UTF8, "application/json")).Result;
        var responseBody = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException(
                $"WinAppDriver session creation failed ({response.StatusCode}): {responseBody}");
        }

        var doc = JsonDocument.Parse(responseBody);
        _sessionId = doc.RootElement.GetProperty("sessionId").GetString()!;
    }

    public string? FindElementByAccessibilityId(string id)
    {
        return FindElement("accessibility id", id);
    }

    public string? FindElementByName(string name)
    {
        return FindElement("name", name);
    }

    private string? FindElement(string usingStrategy, string value)
    {
        var json = "{\"using\":\"" + usingStrategy + "\",\"value\":\"" + value + "\"}";

        var response = _http.PostAsync(
            $"/session/{_sessionId}/element",
            new StringContent(json, Encoding.UTF8, "application/json")).Result;
        var responseBody = response.Content.ReadAsStringAsync().Result;

        if ((int)response.StatusCode == 404)
            return null;

        response.EnsureSuccessStatusCode();

        var doc = JsonDocument.Parse(responseBody);
        var val = doc.RootElement.GetProperty("value");

        // W3C WebDriver element key
        if (val.TryGetProperty("element-6066-11e4-a52e-4f735466cecf", out var w3cId))
            return w3cId.GetString();

        // JSON Wire Protocol element key
        if (val.TryGetProperty("ELEMENT", out var jwpId))
            return jwpId.GetString();

        return null;
    }

    public void Click(string elementId)
    {
        if (elementId == null)
            throw new ArgumentNullException(nameof(elementId));

        var response = _http.PostAsync(
            $"/session/{_sessionId}/element/{elementId}/click", null).Result;
        response.EnsureSuccessStatusCode();
    }

    public void SendKeys(string elementId, string text)
    {
        if (elementId == null)
            throw new ArgumentNullException(nameof(elementId));

        var json = "{\"text\":\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"}";
        var response = _http.PostAsync(
            $"/session/{_sessionId}/element/{elementId}/value",
            new StringContent(json, Encoding.UTF8, "application/json")).Result;
        response.EnsureSuccessStatusCode();
    }

    public void SendKeysToSession(string text)
    {
        var chars = text.ToCharArray().Select(c => JsonSerializer.Serialize(c.ToString())).ToArray();
        var json = "{\"value\":[" + string.Join(",", chars) + "]}";
        var response = _http.PostAsync(
            $"/session/{_sessionId}/keys",
            new StringContent(json, Encoding.UTF8, "application/json")).Result;
        response.EnsureSuccessStatusCode();
    }

    public string? GetText(string elementId)
    {
        if (elementId == null)
            return null;

        var response = _http.GetAsync(
            $"/session/{_sessionId}/element/{elementId}/text").Result;
        var responseBody = response.Content.ReadAsStringAsync().Result;
        response.EnsureSuccessStatusCode();

        var doc = JsonDocument.Parse(responseBody);
        return doc.RootElement.GetProperty("value").GetString();
    }

    public string? WaitForElementByAccessibilityId(string id, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var result = FindElementByAccessibilityId(id);
            if (result != null)
                return result;
            Thread.Sleep(200);
        }
        return null;
    }

    public string? WaitForElementByName(string name, int timeoutMs = 5000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            var result = FindElementByName(name);
            if (result != null)
                return result;
            Thread.Sleep(200);
        }
        return null;
    }

    public void Dispose()
    {
        try { _http.DeleteAsync($"/session/{_sessionId}").Wait(); } catch { }
        _http.Dispose();
    }
}
