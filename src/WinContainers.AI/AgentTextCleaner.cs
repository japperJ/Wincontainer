using System.Text.RegularExpressions;

namespace WinContainers.AI;

/// <summary>
/// Cleans assistant output before it is shown. Some models emit DSML-style
/// special tokens (for example &lt;｜DSML｜...｜&gt;) in place of tool calls.
/// The app does not interpret them, so they would otherwise appear as raw
/// text in the reply.
/// </summary>
public static class AgentTextCleaner
{
    private static readonly Regex ToolCallBlockRegex = new(
        "<｜DSML｜tool_call_start｜>.*?<｜DSML｜tool_call_end｜>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex MarkerRegex = new(
        "<｜DSML｜.*?(?:｜>|$)",
        RegexOptions.Singleline | RegexOptions.Compiled);

    /// <summary>Removes DSML-style special tokens from assistant text.</summary>
    public static string StripSpecialTokens(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var cleaned = ToolCallBlockRegex.Replace(text, string.Empty);
        cleaned = MarkerRegex.Replace(cleaned, string.Empty);
        return cleaned.Trim();
    }
}
