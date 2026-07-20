namespace WinContainers.Runtime;

public static class RuntimeTools
{
    public static bool IsExecutableAvailable(string name)
    {
        return !string.IsNullOrEmpty(ResolveExecutablePath(name));
    }

    public static string ResolveExecutablePath(string name)
    {
        var pathVariable = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var extensions = (Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var directory in pathVariable.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var extension in extensions.DefaultIfEmpty(string.Empty))
            {
                var candidate = Path.Combine(directory, name + extension);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }
        }

        foreach (var fallback in GetFallbackPaths(name))
        {
            if (File.Exists(fallback))
            {
                return fallback;
            }
        }

        return string.Empty;
    }

    private static IEnumerable<string> GetFallbackPaths(string name)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        var systemRoot = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

        return
        [
            Path.Combine(localAppData, "Microsoft", "WindowsApps", $"{name}.exe"),
            Path.Combine(programFiles, "WSL", $"{name}.exe"),
            Path.Combine(systemRoot, "System32", $"{name}.exe"),
            Path.Combine(systemRoot, $"{name}.exe"),
        ];
    }
}
