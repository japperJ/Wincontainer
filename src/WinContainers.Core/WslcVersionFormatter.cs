namespace WinContainers.Core;

using System.Text.RegularExpressions;

public static class WslcVersionFormatter
{
    /// <summary>
    /// Formats raw "wslc --version" output into a clean version string.
    /// Returns the first semantic version found (or the raw output if no version is found).
    /// </summary>
    public static string Format(string versionOutput)
    {
        foreach (var line in versionOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            var match = Regex.Match(trimmed, @"\b\d+\.\d+\.\d+(?:\.\d+)?\b");
            if (match.Success)
            {
                return match.Value;
            }
        }

        return versionOutput;
    }
}
