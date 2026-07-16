namespace WinContainers.Core.Models;

public static class ServiceEndpointResolver
{
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
        var port = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
        return $"http://127.0.0.1:{port}";
    }

    public static string ResolveToken()
    {
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN") ?? "dev-token";
    }
}
