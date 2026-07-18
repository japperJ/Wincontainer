namespace WinContainers.Core.Models;

public static class ServiceEndpointResolver
{
    public static string ResolveServicePort()
    {
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
    }

    public static string ResolveServiceHost()
    {
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_HOST") ?? "0.0.0.0";
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
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN") ?? "dev-token";
    }
}
