using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using WinContainers.Runtime;
using WinContainers.Service.Host;
using WinContainers.Service.Mcp;

namespace WinContainers.Tests.Integration;

public class UnitTest1
{
    [Fact]
    public async Task ServiceHost_ShouldFailClosedWhenMcpClientDoesNotSupportElicitation()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");
        var driver = new IntegrationRecordingDriver();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var app = ServiceHost.Build(Array.Empty<string>(), null, driver);

        try
        {
            await app.StartAsync();

            var localAddress = CreateLoopbackUri(app.Urls.First());
            using var client = new HttpClient { BaseAddress = localAddress };
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));
            await InitializeMcpAsync(client);

            using var response = await client.PostAsync(
                "/mcp",
                CreateMcpToolCallRequest("remove_container", new { id = "integration-web" }));
            var text = ExtractMcpText(await response.Content.ReadAsStringAsync());

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            text.Should().Contain("elicitation");
            driver.RemoveContainerCalls.Should().Be(0);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldAllowMcpElicitationAndInvokeDestructiveDriverOnce()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");
        var driver = new IntegrationRecordingDriver();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var app = ServiceHost.Build(Array.Empty<string>(), null, driver);

        try
        {
            await app.StartAsync();
            var endpoint = CreateLoopbackUri(app.Urls.First()).ToString().TrimEnd('/');
            using var httpClient = new HttpClient();
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri($"{endpoint}/mcp") },
                httpClient,
                NullLoggerFactory.Instance,
                ownsHttpClient: false);
            await using var client = await McpClient.CreateAsync(
                transport,
                new McpClientOptions
                {
                    ProtocolVersion = "2025-11-25",
                    ClientInfo = new Implementation { Name = "WinContainers.Tests", Version = "1.0" },
                    Capabilities = new ClientCapabilities
                    {
                        Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
                    },
                    Handlers = new McpClientHandlers
                    {
                        ElicitationHandler = (_, _) => ValueTask.FromResult(new ElicitResult
                        {
                            Action = "accept",
                            Content = new Dictionary<string, JsonElement>
                            {
                                ["Allow"] = JsonSerializer.SerializeToElement("allow")
                            }
                        })
                    }
                },
                NullLoggerFactory.Instance);

            var result = await client.CallToolAsync(
                "remove_container",
                new Dictionary<string, object?> { ["id"] = "integration-web" });

            result.IsError.Should().NotBeTrue();
            driver.RemoveContainerCalls.Should().Be(1);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Theory]
    [InlineData("decline")]
    [InlineData("cancel")]
    [InlineData("wrong-allow")]
    [InlineData("non-string-allow")]
    [InlineData("missing-content")]
    [InlineData("handler-failure")]
    public async Task ServiceHost_ShouldBlockEveryNonAllowMcpElicitationOutcome(string outcome)
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");
        var driver = new IntegrationRecordingDriver();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var app = ServiceHost.Build(Array.Empty<string>(), null, driver);

        try
        {
            await app.StartAsync();
            var endpoint = CreateLoopbackUri(app.Urls.First()).ToString().TrimEnd('/');
            using var httpClient = new HttpClient();
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri($"{endpoint}/mcp") },
                httpClient,
                NullLoggerFactory.Instance,
                ownsHttpClient: false);
            await using var client = await McpClient.CreateAsync(
                transport,
                new McpClientOptions
                {
                    ProtocolVersion = "2025-11-25",
                    ClientInfo = new Implementation { Name = "WinContainers.Tests", Version = "1.0" },
                    Capabilities = new ClientCapabilities
                    {
                        Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
                    },
                    Handlers = new McpClientHandlers
                    {
                        ElicitationHandler = (_, _) =>
                        {
                            if (outcome == "handler-failure")
                                throw new InvalidOperationException("test elicitation failure");

                            return ValueTask.FromResult(new ElicitResult
                            {
                                Action = outcome == "cancel" ? "cancel" : outcome == "decline" ? "decline" : "accept",
                                Content = outcome == "missing-content"
                                    ? null
                                    : new Dictionary<string, JsonElement>
                                    {
                                        ["Allow"] = outcome == "non-string-allow"
                                            ? JsonSerializer.SerializeToElement(1)
                                            : JsonSerializer.SerializeToElement(
                                                outcome == "wrong-allow" ? "deny" : "allow")
                                    }
                            });
                        }
                    }
                },
                NullLoggerFactory.Instance);

            var result = await client.CallToolAsync(
                "remove_container",
                new Dictionary<string, object?> { ["id"] = "integration-web" });

            result.IsError.Should().NotBeTrue();
            driver.RemoveContainerCalls.Should().Be(0);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

    [Fact]
    public async Task ServiceHost_ShouldKeepRedeployApprovalPromptFreeOfSensitiveArguments()
    {
        var originalPort = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT");
        var originalToken = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN");
        var driver = new IntegrationRecordingDriver();
        string? elicitationMessage = null;
        const string tarDataSentinel = "dGFyLXNlY3JldC1wYXlsb2Fk";
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        McpDestructiveConfirmationPolicy.SetEnabled(true);
        var app = ServiceHost.Build(Array.Empty<string>(), null, driver);

        try
        {
            await app.StartAsync();
            var endpoint = CreateLoopbackUri(app.Urls.First()).ToString().TrimEnd('/');
            using var httpClient = new HttpClient();
            var transport = new HttpClientTransport(
                new HttpClientTransportOptions { Endpoint = new Uri($"{endpoint}/mcp") },
                httpClient,
                NullLoggerFactory.Instance,
                ownsHttpClient: false);
            await using var client = await McpClient.CreateAsync(
                transport,
                new McpClientOptions
                {
                    ProtocolVersion = "2025-11-25",
                    ClientInfo = new Implementation { Name = "WinContainers.Tests", Version = "1.0" },
                    Capabilities = new ClientCapabilities
                    {
                        Elicitation = new ElicitationCapability { Form = new FormElicitationCapability() }
                    },
                    Handlers = new McpClientHandlers
                    {
                        ElicitationHandler = (request, _) =>
                        {
                            elicitationMessage = request?.Message ?? string.Empty;
                            return ValueTask.FromResult(new ElicitResult
                            {
                                Action = "accept",
                                Content = new Dictionary<string, JsonElement>
                                {
                                    ["Allow"] = JsonSerializer.SerializeToElement("allow")
                                }
                            });
                        }
                    }
                },
                NullLoggerFactory.Instance);

            var result = await client.CallToolAsync(
                "redeploy_web_only",
                new Dictionary<string, object?>
                {
                    ["webContainerId"] = "web",
                    ["image"] = "replacement:image",
                    ["name"] = "replacement",
                    ["ports"] = "80:80",
                    ["volumes"] = "/secret/host:/secret/container",
                    ["env"] = $"SECRET_TOKEN=do-not-expose,TAR_DATA={tarDataSentinel}",
                    ["network"] = "app-network"
                });

            result.IsError.Should().NotBeTrue();
            elicitationMessage.Should().NotBeNull();
            elicitationMessage.Should().NotContain("do-not-expose");
            elicitationMessage.Should().NotContain(tarDataSentinel);
            elicitationMessage.Should().NotContain("/secret/host:/secret/container");
            driver.RemoveContainerCalls.Should().Be(1);
        }
        finally
        {
            await app.StopAsync();
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", originalPort);
            Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", originalToken);
        }
    }

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
            await InitializeMcpAsync(client);

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

        var nonLoopback = Dns.GetHostAddresses(Dns.GetHostName())
            .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip));

        if (nonLoopback is null)
        {
            // No non-loopback address available (e.g. isolated CI) — cannot exercise the remote path.
            return;
        }

        // Bind to all interfaces so a request from the machine's own non-loopback IP is treated as remote.
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", "0");
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", string.Empty);
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST", "0.0.0.0");

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
            await InitializeMcpAsync(client);

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
            await InitializeMcpAsync(client);

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

    private static async Task InitializeMcpAsync(HttpClient client)
    {
        using var response = await client.PostAsync(
            "/mcp",
            new StringContent(
                """
                {"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2025-11-25","capabilities":{},"clientInfo":{"name":"WinContainers.Tests","version":"1.0"}}}
                """,
                Encoding.UTF8,
                "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.TryGetValues("Mcp-Session-Id", out var values).Should().BeTrue();
        client.DefaultRequestHeaders.Add("Mcp-Session-Id", values!.Single());
    }

    private static StringContent CreateMcpToolCallRequest(string toolName, object arguments)
    {
        return new StringContent(
            JsonSerializer.Serialize(new
            {
                jsonrpc = "2.0",
                id = Guid.NewGuid().ToString("N"),
                method = "tools/call",
                @params = new { name = toolName, arguments }
            }),
            Encoding.UTF8,
            "application/json");
    }

    private static string ExtractMcpText(string body)
    {
        var dataLine = body
            .Split('\n')
            .Select(line => line.Trim())
            .First(line => line.StartsWith("data: ", StringComparison.Ordinal));
        using var document = JsonDocument.Parse(dataLine["data: ".Length..]);
        return document.RootElement
            .GetProperty("result")
            .GetProperty("content")
            .EnumerateArray()
            .Single()
            .GetProperty("text")
            .GetString()!;
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

internal sealed class IntegrationRecordingDriver : IWslcDriver
{
    public int RemoveContainerCalls { get; private set; }

    public Task<bool> IsAvailableAsync(CancellationToken ct) => Task.FromResult(true);
    public Task<string> GetVersionAsync(CancellationToken ct) => Task.FromResult("integration");
    public Task<string> GetContainersAsync(CancellationToken ct) => Task.FromResult("[]");
    public Task<string> StartContainerAsync(string id, CancellationToken ct) => Task.FromResult($"started {id}");
    public Task<string> StopContainerAsync(string id, CancellationToken ct) => Task.FromResult($"stopped {id}");
    public Task<string> RestartContainerAsync(string id, CancellationToken ct) => Task.FromResult($"restarted {id}");
    public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) => Task.FromResult($"renamed {id}");

    public Task<string> RemoveContainerAsync(string id, CancellationToken ct)
    {
        RemoveContainerCalls++;
        return Task.FromResult($"removed {id}");
    }

    public Task<string> InspectContainerAsync(string id, CancellationToken ct) => Task.FromResult("{}");
    public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) => Task.FromResult(string.Empty);
    public Task<string> GetImagesAsync(CancellationToken ct) => Task.FromResult("[]");
    public Task<string> PullImageAsync(string image, CancellationToken ct) => Task.FromResult($"pulled {image}");
    public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct) => Task.FromResult(string.Empty);
    public Task<string> RemoveImageAsync(string id, CancellationToken ct) => Task.FromResult($"removed {id}");
    public Task<string> InspectImageAsync(string id, CancellationToken ct) => Task.FromResult("{}");
    public Task<string> GetVolumesAsync(CancellationToken ct) => Task.FromResult("[]");
    public Task<string> CreateVolumeAsync(string name, CancellationToken ct) => Task.FromResult($"created {name}");
    public Task<string> RemoveVolumeAsync(string name, CancellationToken ct) => Task.FromResult($"removed {name}");
    public Task<string> InspectVolumeAsync(string name, CancellationToken ct) => Task.FromResult("{}");
    public Task<string> GetNetworksAsync(CancellationToken ct) => Task.FromResult("[]");
    public Task<string> CreateNetworkAsync(string name, CancellationToken ct) => Task.FromResult($"created {name}");
    public Task<string> RemoveNetworkAsync(string name, CancellationToken ct) => Task.FromResult($"removed {name}");
    public Task<string> RunContainerAsync(
        string image,
        string? name = null,
        IEnumerable<string>? ports = null,
        IEnumerable<string>? volumes = null,
        IEnumerable<string>? env = null,
        CancellationToken ct = default,
        string? network = null) => Task.FromResult($"ran {image}");
    public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default) => Task.FromResult(string.Empty);
    public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default) => Task.FromResult(string.Empty);
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
