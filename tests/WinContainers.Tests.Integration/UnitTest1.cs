using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
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
    [Fact(Skip = "nerdctl runtime is not available in CI by default")]
    public void NerdctlRuntime_ShouldBeReachableInWsl()
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo("wsl", "nerdctl --version")
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
