namespace WinContainers.Core;

public static class WslcVersionFormatter
{
    /// <summary>
    /// Formats raw "wslc --version" output into a clean version string.
    /// Input typically looks like:
    ///   wslc compatibility bridge (nerdctl backend)
    ///   nerdctl version 2.3.1
    /// Returns "2.3.1" (or the raw output if the version line is not found).
    /// </summary>
    public static string Format(string versionOutput)
    {
        foreach (var line in versionOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("nerdctl version", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed["nerdctl version".Length..].Trim();
            }
        }

        return versionOutput;
    }
}
