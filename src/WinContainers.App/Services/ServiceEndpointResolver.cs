namespace WinContainers_App.Services;

/// <summary>
/// Resolves the service endpoint URL based on environment configuration.
/// </summary>
public static class ServiceEndpointResolver
{
    /// <summary>
    /// Resolves the service HTTP endpoint.
    /// </summary>
    public static string Resolve()
    {
        var port = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
        return $"http://localhost:{port}";
    }

    /// <summary>
    /// Resolves the service project path.
    /// </summary>
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

    /// <summary>
    /// Resolves the bearer token for authorization.
    /// </summary>
    public static string ResolveToken()
    {
        // For now, return a placeholder token. This should be replaced with actual token resolution.
        return Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN") ?? "dev-token";
    }
}
