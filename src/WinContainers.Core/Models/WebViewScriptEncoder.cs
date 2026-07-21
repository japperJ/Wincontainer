using System.Text.Json;

namespace WinContainers.Core.Models;

public static class WebViewScriptEncoder
{
    public static string BuildSetJsonScript(string json) =>
        $"setJson({JsonSerializer.Serialize(json)})";
}
