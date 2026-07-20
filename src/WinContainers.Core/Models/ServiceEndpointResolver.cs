namespace WinContainers.Core.Models;

public static class ServiceEndpointResolver
{
    private static string? _tokenOverride;
    private static string? _hostOverride;

    public static string ResolveServicePort()
    {
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
    }

    public static string ResolveServiceHost()
    {
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST")
            ?? _hostOverride
            ?? (string.IsNullOrWhiteSpace(ResolveToken()) ? "127.0.0.1" : "0.0.0.0");
    }

    public static void SetToken(string token)
    {
        _tokenOverride = token;
    }

    public static void SetListenHost(string host)
    {
        _hostOverride = host;
    }

    public static string ResolveServiceProjectPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, ".worktrees", "sprint1", "src", "WinContainers.Service", "WinContainers.Service.csproj");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            current = current.Parent;
        }

        var fallback = Path.Combine(Directory.GetCurrentDirectory(), ".worktrees", "sprint1", "src", "WinContainers.Service", "WinContainers.Service.csproj");
        return File.Exists(fallback) ? fallback : string.Empty;
    }

    public static string Resolve()
    {
        return $"http://127.0.0.1:{ResolveServicePort()}";
    }

    public static string ResolveToken()
    {
        return _tokenOverride
            ?? Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN")
            ?? string.Empty;
    }
}
