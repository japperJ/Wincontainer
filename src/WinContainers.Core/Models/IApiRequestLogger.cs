namespace WinContainers.Core.Models;

public interface IApiRequestLogger
{
    /// <summary>When false, the MCP server rejects all /mcp requests with 404.</summary>
    bool McpEnabled { get; set; }

    /// <summary>When false, non-loopback /api requests are rejected with 403. Localhost always works.</summary>
    bool AllowRemoteApiAccess { get; set; }

    /// <summary>When true, MCP activity is written to the Output window.</summary>
    bool McpLoggingEnabled { get; set; }

    void LogRequest(string method, string path, string remoteIp, bool isRemote);

    /// <summary>Logs one MCP activity line. <paramref name="outcome"/> is a short summary such as "ok" or "error: message".</summary>
    void LogMcpRequest(string methodInfo, string remoteIp, bool isRemote, string? outcome);
}
