using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;

namespace WinContainers.Service.Host;

public static class ServiceHost
{
    public static WebApplication Build(
        string[] args,
        IApiRequestLogger? requestLogger = null,
        IWslcDriver? driverOverride = null)
    {
        var builder = WebApplication.CreateBuilder(args);

        var port = int.Parse(ServiceEndpointResolver.ResolveServicePort());
        var listenHost = ServiceEndpointResolver.ResolveServiceHost();

        builder.WebHost.UseKestrel(options =>
        {
            if (string.IsNullOrWhiteSpace(listenHost))
            {
                options.ListenLocalhost(port);
            }
            else if (string.Equals(listenHost, "0.0.0.0", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listenHost, "::", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listenHost, "[::]", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenAnyIP(port);
            }
            else if (string.Equals(listenHost, "localhost", StringComparison.OrdinalIgnoreCase)
                || string.Equals(listenHost, "loopback", StringComparison.OrdinalIgnoreCase))
            {
                options.ListenLocalhost(port);
            }
            else if (IPAddress.TryParse(listenHost, out var address))
            {
                options.Listen(address, port);
            }
            else
            {
                throw new InvalidOperationException($"Unsupported WINCONTAINERS_SERVICE_HOST '{listenHost}'. Use a valid IP address, 'localhost', or leave it unset.");
            }

            options.Limits.MaxRequestBodySize = null;
        });

        builder.Services.AddSingleton<IWslcDriver>(_ => driverOverride ?? new WslcDriver());
        builder.Services.AddSingleton<ImageUploadStore>();

        builder.Services.Configure<FormOptions>(o =>
        {
            o.MultipartBodyLengthLimit = 524288000;
        });

        // Configure JSON options with non-nullable reference types for clean MCP schema generation
        var jsonOptions = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };
        jsonOptions.MakeReadOnly(populateMissingResolver: true);

        builder.Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                // Elicitation sends a server request over the originating stream,
                // so the transport must retain the MCP session.
                options.Stateless = false;
            })
.WithTools<global::WinContainers.Service.Mcp.WincontainerTools>(jsonOptions);

        var app = builder.Build();

        var driver = app.Services.GetRequiredService<IWslcDriver>();

        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await next();
                return;
            }

            var remoteIp = context.Connection.RemoteIpAddress;
            var remoteIpText = remoteIp?.ToString() ?? "unknown";
            var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIpText));

            requestLogger?.LogRequest(context.Request.Method, context.Request.Path.ToString(), remoteIpText, isRemote);

            await next();
        });

        // Remote API access gate — blocks non-loopback /api calls when AllowRemoteApiAccess is off.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/api") && requestLogger is { AllowRemoteApiAccess: false })
            {
                var remoteIp = context.Connection.RemoteIpAddress;
                var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIp?.ToString() ?? string.Empty));
                if (isRemote)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new { error = "Remote API access is disabled" });
                    return;
                }
            }

            await next();
        });

        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/api"))
            {
                await next();
                return;
            }

            var expectedToken = ServiceEndpointResolver.ResolveToken();
            var remoteIp = context.Connection.RemoteIpAddress;
            var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIp?.ToString() ?? string.Empty));

            if (BearerTokenValidator.RequiresAuthorization(isRemote, expectedToken)
                && !BearerTokenValidator.IsAuthorized(context.Request.Headers.Authorization.ToString(), expectedToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }

            await next();
        });

        app.MapGet("/api/health", async (CancellationToken ct) =>
        {
            string version;
            try
            {
                version = await driver.GetVersionAsync(ct);
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                version = "unavailable";
            }

            var runtimeAvailable = await driver.IsAvailableAsync(ct);
            return Results.Ok(new
            {
                ok = true,
                wslcAvailable = runtimeAvailable,
                wslcVersion = version,
                appVersion = "WinContainers WSLC",
                mcpEnabled = requestLogger?.McpEnabled ?? true,
                apiRemoteAccessEnabled = requestLogger?.AllowRemoteApiAccess ?? true
            });
        });

        app.MapGet("/api/info", (HttpContext context) =>
        {
            var resolvedPort = context.Connection.LocalPort.ToString();
            var tokenConfigured = !string.IsNullOrWhiteSpace(ServiceEndpointResolver.ResolveToken());
            return Results.Ok(new ServiceInfo(resolvedPort, tokenConfigured ? "configured" : string.Empty));
        });

        app.MapGet("/api/containers", async (CancellationToken ct) =>
            Results.Ok(new { output = await driver.GetContainersAsync(ct) }));

        app.MapPost("/api/containers/run", async (RunContainerRequest request, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RunContainerAsync(request.Image, request.Name, request.Ports, request.Volumes, request.Env, ct, request.Network) }));

        app.MapPost("/api/containers/{id}/start", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.StartContainerAsync(id, ct) }));

        app.MapPost("/api/containers/{id}/stop", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.StopContainerAsync(id, ct) }));

        app.MapPost("/api/containers/{id}/restart", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RestartContainerAsync(id, ct) }));

        app.MapPost("/api/containers/{id}/rename", async (string id, RenameContainerRequest request, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RenameContainerAsync(id, request.Name, ct) }));

        app.MapDelete("/api/containers/{id}", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RemoveContainerAsync(id, ct) }));

        app.MapGet("/api/containers/{id}/inspect", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.InspectContainerAsync(id, ct) }));

        app.MapPost("/api/containers/{id}/exec", async (string id, ExecCommandRequest request, CancellationToken ct) =>
        {
            var output = request.UseShell
                ? await driver.ExecShellAsync(id, request.Command, request.Shell, ct)
                : await driver.ExecCommandAsync(id, request.Command, ct);
            return Results.Ok(new { output });
        });

        app.MapGet("/api/containers/{id}/logs", async (string id, int? tail, CancellationToken ct) =>
            Results.Ok(new { output = await driver.GetContainerLogsAsync(id, tail ?? 500, ct) }));

        app.MapGet("/api/images", async (CancellationToken ct) =>
            Results.Ok(new { output = await driver.GetImagesAsync(ct) }));

        app.MapPost("/api/images/pull", async (PullImageRequest request, CancellationToken ct) =>
            Results.Ok(new { output = await driver.PullImageAsync(request.Image, ct) }));

        app.MapDelete("/api/images/{id}", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RemoveImageAsync(id, ct) }));

        app.MapGet("/api/images/{id}/inspect", async (string id, CancellationToken ct) =>
            Results.Ok(new { output = await driver.InspectImageAsync(id, ct) }));

        app.MapGet("/api/volumes", async (CancellationToken ct) =>
            Results.Ok(new { output = await driver.GetVolumesAsync(ct) }));

        app.MapPost("/api/volumes", async (CreateVolumeRequest request, CancellationToken ct) =>
            Results.Ok(new { output = await driver.CreateVolumeAsync(request.Name, ct) }));

        app.MapDelete("/api/volumes/{name}", async (string name, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RemoveVolumeAsync(name, ct) }));

        app.MapGet("/api/volumes/{name}/inspect", async (string name, CancellationToken ct) =>
            Results.Ok(new { output = await driver.InspectVolumeAsync(name, ct) }));

        app.MapGet("/api/networks", async (CancellationToken ct) =>
            Results.Ok(new { output = await driver.GetNetworksAsync(ct) }));

        app.MapPost("/api/networks", async (CreateNetworkRequest request, CancellationToken ct) =>
            Results.Ok(new { output = await driver.CreateNetworkAsync(request.Name, ct) }));

        app.MapDelete("/api/networks/{name}", async (string name, CancellationToken ct) =>
            Results.Ok(new { output = await driver.RemoveNetworkAsync(name, ct) }));

        app.MapGet("/api/runtime/version", async (CancellationToken ct) =>
            Results.Ok(new { version = await driver.GetVersionAsync(ct) }));

        // MCP enable gate — rejects all /mcp traffic when the MCP server is disabled.
        app.Use(async (context, next) =>
        {
            if (context.Request.Path.StartsWithSegments("/mcp") && requestLogger is { McpEnabled: false })
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                await context.Response.WriteAsJsonAsync(new { error = "MCP server is disabled" });
                return;
            }

            await next();
        });

        // MCP authorization middleware — enforce the same bearer token rules as /api
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/mcp"))
            {
                await next();
                return;
            }

            var expectedToken = ServiceEndpointResolver.ResolveToken();
            var remoteIp = context.Connection.RemoteIpAddress;
            var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIp?.ToString() ?? string.Empty));

            if (BearerTokenValidator.RequiresAuthorization(isRemote, expectedToken)
                && !BearerTokenValidator.IsAuthorized(context.Request.Headers.Authorization.ToString(), expectedToken))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Unauthorized" });
                return;
            }

            await next();
        });

        // MCP request logging middleware — logs every MCP method to the output window
        // when McpLoggingEnabled is on, with result status and error summary for tool calls.
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/mcp") || !HttpMethods.IsPost(context.Request.Method))
            {
                await next();
                return;
            }

            var shouldLog = requestLogger is { McpLoggingEnabled: true };

            string methodInfo;
            if (!shouldLog)
            {
                methodInfo = "mcp";
            }
            else
            {
                context.Request.EnableBuffering();

                var contentLength = context.Request.ContentLength;
                if (contentLength is null || contentLength > 64 * 1024)
                {
                    methodInfo = "mcp (body too large)";
                }
                else
                {
                    try
                    {
                        using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                        var body = await reader.ReadToEndAsync();
                        context.Request.Body.Position = 0;

                        using var doc = JsonDocument.Parse(body);
                        var method = doc.RootElement.GetProperty("method").GetString() ?? "unknown";

                        if (method == "tools/call"
                            && doc.RootElement.TryGetProperty("params", out var paramsEl)
                            && paramsEl.TryGetProperty("name", out var nameEl))
                        {
                            methodInfo = $"{method} {nameEl.GetString()}";
                        }
                        else
                        {
                            methodInfo = method;
                        }
                    }
                    catch
                    {
                        methodInfo = "mcp (parse error)";
                    }
                }
            }

            string? outcome = null;
            ResponseCaptureStream? capture = null;
            var originalBody = context.Response.Body;
            if (shouldLog)
            {
                capture = new ResponseCaptureStream(originalBody);
                context.Response.Body = capture;
            }

            try
            {
                await next();
            }
            finally
            {
                context.Response.Body = originalBody;
            }

            if (shouldLog && requestLogger is { } logger)
            {
                outcome = capture is null
                    ? (context.Response.StatusCode >= 400 ? $"http {context.Response.StatusCode}" : null)
                    : SummarizeMcpOutcome(capture, context.Response.StatusCode);

                var remoteIp = context.Connection.RemoteIpAddress;
                var remoteIpText = remoteIp?.ToString() ?? "unknown";
                var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIpText));
                logger.LogMcpRequest($"/mcp [{methodInfo}]", remoteIpText, isRemote, outcome);
            }
        });

        app.MapMcp("/mcp");

        return app;
    }

    private static bool IsLocalHostAddress(string address)
    {
        return string.Equals(address, "127.0.0.1", StringComparison.Ordinal)
            || string.Equals(address, "::1", StringComparison.Ordinal)
            || string.Equals(address, "localhost", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Writes through to the real response body while keeping a bounded capture for diagnostics.</summary>
    private sealed class ResponseCaptureStream : Stream
    {
        private const int MaxCaptureBytes = 1 * 1024 * 1024;
        private readonly Stream _inner;
        private readonly MemoryStream _capture = new();

        public ResponseCaptureStream(Stream inner) => _inner = inner;

        public string CapturedText => Encoding.UTF8.GetString(_capture.ToArray());

        public override bool CanRead => _inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => _inner.CanWrite;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => _inner.Position = value; }

        public override void Flush() => _inner.Flush();

        public override Task FlushAsync(CancellationToken cancellationToken) => _inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

        public override void SetLength(long value) => _inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count)
        {
            _inner.Write(buffer, offset, count);
            Capture(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            await _inner.WriteAsync(buffer, offset, count, cancellationToken);
            Capture(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            var innerTask = _inner.WriteAsync(buffer, cancellationToken);
            Capture(buffer.Span);
            return innerTask;
        }

        private void Capture(byte[] buffer, int offset, int count)
        {
            var remaining = MaxCaptureBytes - checked((int)_capture.Length);
            if (remaining <= 0)
            {
                return;
            }

            var take = Math.Min(count, remaining);
            _capture.Write(buffer, offset, take);
        }

        private void Capture(ReadOnlySpan<byte> span)
        {
            var remaining = MaxCaptureBytes - checked((int)_capture.Length);
            if (remaining <= 0)
            {
                return;
            }

            var take = Math.Min(span.Length, remaining);
            _capture.Write(span[..take]);
        }
    }

    /// <summary>Builds a one-line outcome summary from the captured MCP response body (SSE or JSON).</summary>
    private static string? SummarizeMcpOutcome(ResponseCaptureStream capture, int statusCode)
    {
        if (statusCode >= 400)
        {
            return $"http {statusCode}";
        }

        var text = capture.CapturedText;
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        try
        {
            // MCP over HTTP responds with SSE: one or more "data: {...}" lines.
            var dataLine = text
                .Split('\n')
                .Select(line => line.Trim())
                .FirstOrDefault(line => line.StartsWith("data:", StringComparison.OrdinalIgnoreCase));

            if (dataLine is null)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(dataLine["data:".Length..].Trim());

            if (doc.RootElement.TryGetProperty("error", out var errorEl))
            {
                var message = errorEl.TryGetProperty("message", out var msgEl)
                    ? msgEl.GetString()
                    : null;
                return string.IsNullOrWhiteSpace(message) ? "error" : $"error: {message}";
            }

            if (doc.RootElement.TryGetProperty("result", out var resultEl))
            {
                var isError = resultEl.TryGetProperty("isError", out var flagEl) && flagEl.GetBoolean();
                if (isError)
                {
                    var content = resultEl.TryGetProperty("content", out var contentEl)
                        ? ExtractTextContent(contentEl)
                        : null;
                    return string.IsNullOrWhiteSpace(content) ? "failed" : $"failed: {Truncate(content, 120)}";
                }

                return "ok";
            }

            return "ok";
        }
        catch
        {
            return null;
        }
    }

    private static string? ExtractTextContent(JsonElement contentEl)
    {
        if (contentEl.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var item in contentEl.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Object
                && item.TryGetProperty("text", out var textEl)
                && textEl.ValueKind == JsonValueKind.String)
            {
                var text = textEl.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }
        }

        return null;
    }

    private static string Truncate(string value, int maxLength)
        => value.Length <= maxLength ? value : value[..maxLength] + "…";
}

public sealed record PullImageRequest(string Image);
public sealed record RunContainerRequest(string Image, string? Name, List<string>? Ports, List<string>? Volumes, List<string>? Env, string? Network = null);
public sealed record RenameContainerRequest(string Name);
public sealed record CreateVolumeRequest(string Name);
public sealed record CreateNetworkRequest(string Name);
public sealed record ExecCommandRequest(string Command, bool UseShell = false, string? Shell = null);
