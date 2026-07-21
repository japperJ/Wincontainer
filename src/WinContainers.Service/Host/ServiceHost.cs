using System.Net;
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

        builder.Services.AddSingleton<WslcDriver>();

        builder.Services.Configure<FormOptions>(o =>
        {
            o.MultipartBodyLengthLimit = 524288000;
        });

        var app = builder.Build();

        var driver = app.Services.GetRequiredService<WslcDriver>();

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
            Results.Ok(new { output = await driver.RunContainerAsync(request.Image, request.Name, request.Ports, request.Volumes, request.Env, request.Restart, ct) }));

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
public sealed record RunContainerRequest(string Image, string? Name, List<string>? Ports, List<string>? Volumes, List<string>? Env, string? Restart);
public sealed record RenameContainerRequest(string Name);
public sealed record CreateVolumeRequest(string Name);
public sealed record CreateNetworkRequest(string Name);
public sealed record ExecCommandRequest(string Command, bool UseShell = false, string? Shell = null);
