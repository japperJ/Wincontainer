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
            document.RootElement.GetProperty("result").GetProperty("tools")
                .EnumerateArray()
                .Select(tool => tool.GetProperty("name").GetString())
                .Should()
                .Contain(new[] { "health_check", "load_image" });
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
