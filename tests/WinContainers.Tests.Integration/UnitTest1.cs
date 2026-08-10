using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using WinContainers.Service.Host;

namespace WinContainers.Tests.Integration;

public class UnitTest1
{
    private static Uri CreateLoopbackUri(string address)
    {
        var builder = new UriBuilder(address);

        if (IPAddress.TryParse(builder.Host, out var addressValue) &&
            (addressValue.Equals(IPAddress.Any) || addressValue.Equals(IPAddress.IPv6Any)))
        {
            builder.Host = "127.0.0.1";
        }

        return builder.Uri;
    }

    [Fact]
    public async Task ServiceHost_ShouldExposeRuntimeInfoForAuthorizedRequests()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        var app = ServiceHost.Build(Array.Empty<string>());

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");

            using var response = await client.GetAsync("/api/info");
            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("port").GetString().Should().NotBeNullOrWhiteSpace();
            document.RootElement.GetProperty("token").GetString().Should().Be("configured");
            document.RootElement.GetProperty("scripts").ValueKind.Should().Be(JsonValueKind.Array);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldRejectUnauthorizedRequests()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        var app = ServiceHost.Build(Array.Empty<string>());

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };

            using var response = await client.GetAsync("/api/info");

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldRejectUnauthorizedMcpRequests()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        var app = ServiceHost.Build(Array.Empty<string>());

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await client.PostAsync("/mcp", CreateMcpToolsListRequest());

            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldExposeMcpToolsForAuthorizedRequests()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", "test-token");

        var app = ServiceHost.Build(Array.Empty<string>());

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", "test-token");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await client.PostAsync("/mcp", CreateMcpToolsListRequest());
            var body = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("text/event-stream");

            var dataLine = body.Split('\n').First(line => line.StartsWith("data: ", StringComparison.Ordinal));
            using var document = JsonDocument.Parse(dataLine["data: ".Length..]);

            document.RootElement.GetProperty("jsonrpc").GetString().Should().Be("2.0");
            var toolNames = document.RootElement.GetProperty("result").GetProperty("tools")
                .EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString())
                .ToArray();

            toolNames.Should().Contain("health_check");
            toolNames.Should().Contain("load_image");
            toolNames.Should().Contain("start_image_upload");
            toolNames.Should().Contain("upload_image_chunk");
            toolNames.Should().Contain("finish_image_upload");

            var runContainer = document.RootElement.GetProperty("result").GetProperty("tools")
                .EnumerateArray()
                .Single(tool => tool.GetProperty("name").GetString() == "run_container");
            runContainer.GetProperty("inputSchema").GetProperty("properties")
                .TryGetProperty("network", out _).Should().BeTrue();
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldReturn404ForMcpWhenDisabled()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);

        var logger = new TestRequestLogger { McpEnabled = false };
        var app = ServiceHost.Build(Array.Empty<string>(), logger);

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };

            using var response = await client.PostAsync("/mcp", CreateMcpToolsListRequest());

            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_Health_ShouldReportToggleState()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);

        var logger = new TestRequestLogger
        {
            McpEnabled = false,
            AllowRemoteApiAccess = false,
            McpLoggingEnabled = true
        };
        var app = ServiceHost.Build(Array.Empty<string>(), logger);

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };

            using var response = await client.GetAsync("/api/health");
            var json = await response.Content.ReadAsStringAsync();

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var document = JsonDocument.Parse(json);
            document.RootElement.GetProperty("mcpEnabled").GetBoolean().Should().BeFalse();
            document.RootElement.GetProperty("apiRemoteAccessEnabled").GetBoolean().Should().BeFalse();
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldBlockRemoteApiWhenDisabled()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");
        var originalHost = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST");

        // Bind to all interfaces so a request from the machine's own non-loopback IP is treated as remote.
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", "0.0.0.0");

        var nonLoopback = Dns.GetHostAddresses(Dns.GetHostName())
            .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

        if (nonLoopback is null)
        {
            // No non-loopback address available (e.g. isolated CI) — cannot exercise the remote path.
            return;
        }

        var logger = new TestRequestLogger { AllowRemoteApiAccess = false };
        var app = ServiceHost.Build(Array.Empty<string>(), logger);

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);
            var remoteAddress = new UriBuilder(address) { Host = nonLoopback.ToString() }.Uri;

            // Localhost still works when remote access is disabled.
            using (var localClient = new HttpClient { BaseAddress = localAddress })
            {
                using var localResponse = await localClient.GetAsync("/api/health");
                localResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            }

            // A request from a non-loopback source is rejected with 403.
            using (var remoteClient = new HttpClient { BaseAddress = remoteAddress })
            {
                using var remoteResponse = await remoteClient.GetAsync("/api/health");
                remoteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            }
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", originalHost);
        }
    }

    [Fact]
    public async Task ServiceHost_McpLogging_ShouldLogToolCallsWhenEnabled()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);

        var logger = new TestRequestLogger { McpLoggingEnabled = true };
        var app = ServiceHost.Build(Array.Empty<string>(), logger);

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await client.PostAsync("/mcp", CreateMcpToolsListRequest());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            logger.McpLogs.Should().Contain(log => log.StartsWith("/mcp [tools/list]", StringComparison.Ordinal));
            logger.McpLogs.Should().Contain(log => log.EndsWith("|ok", StringComparison.Ordinal));
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_McpLogging_ShouldSkipWhenDisabled()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");

        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);

        var logger = new TestRequestLogger { McpLoggingEnabled = false };
        var app = ServiceHost.Build(Array.Empty<string>(), logger);

        try
        {
            await app.StartAsync();

            var address = app.Urls.First();
            var localAddress = CreateLoopbackUri(address);

            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            using var response = await client.PostAsync("/mcp", CreateMcpToolsListRequest());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            logger.McpLogs.Should().BeEmpty();
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    private static StringContent CreateMcpToolsListRequest()
    {
        return new StringContent(
            """
            {"jsonrpc":"2.0","id":1,"method":"tools/list","params":{}}
            """,
            Encoding.UTF8,
            "application/json");
    }

    [Fact(Skip = "WSLC is not available in CI by default")]
    public void WslcRuntime_ShouldBeReachable()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("wslc", "--version")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start().Should().BeTrue();
        process.WaitForExit();

        process.ExitCode.Should().Be(0);
    }
}

internal sealed class TestRequestLogger : WinContainers.Core.Models.IApiRequestLogger
{
    public bool McpEnabled { get; set; } = true;
    public bool AllowRemoteApiAccess { get; set; } = true;
    public bool McpLoggingEnabled { get; set; } = true;

    public List<string> McpLogs { get; } = [];

    public void LogRequest(string method, string path, string remoteIp, bool isRemote)
    {
    }

    public void LogMcpRequest(string methodInfo, string remoteIp, bool isRemote, string? outcome)
    {
        McpLogs.Add($"{methodInfo}|{outcome}");
    }
}
