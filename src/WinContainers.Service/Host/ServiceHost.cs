using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.Features;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;

namespace WinContainers.Service.Host;

public static class ServiceHost
{
    public static WebApplication Build(string[] args, IApiRequestLogger? requestLogger = null)
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

        builder.Services.AddSingleton<IWslcDriver, WslcDriver>();
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
                options.Stateless = true;
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
            var version = await driver.GetVersionAsync(ct);
            var runtimeAvailable = await driver.IsAvailableAsync(ct);
            return Results.Ok(new
            {
                ok = true,
                wslcAvailable = runtimeAvailable,
                wslcVersion = version,
                appVersion = "WinContainers WSLC"
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
            Results.Ok(new { output = await driver.RunContainerAsync(request.Image, request.Name, request.Ports, request.Volumes, request.Env, ct) }));

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

        // MCP request logging middleware — logs every MCP tool invocation to the output window
        app.Use(async (context, next) =>
        {
            if (!context.Request.Path.StartsWithSegments("/mcp") || !HttpMethods.IsPost(context.Request.Method))
            {
                await next();
                return;
            }

            context.Request.EnableBuffering();

            string methodInfo;
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

            var remoteIp = context.Connection.RemoteIpAddress;
            var remoteIpText = remoteIp?.ToString() ?? "unknown";
            var isRemote = remoteIp is null || (!IPAddress.IsLoopback(remoteIp) && !IsLocalHostAddress(remoteIpText));
            requestLogger?.LogRequest("MCP", $"/mcp [{methodInfo}]", remoteIpText, isRemote);

            await next();
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
}

public sealed record PullImageRequest(string Image);
public sealed record RunContainerRequest(string Image, string? Name, List<string>? Ports, List<string>? Volumes, List<string>? Env);
public sealed record RenameContainerRequest(string Name);
public sealed record CreateVolumeRequest(string Name);
public sealed record CreateNetworkRequest(string Name);
public sealed record ExecCommandRequest(string Command, bool UseShell = false, string? Shell = null);
