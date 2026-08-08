using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;

namespace WinContainers.AI;

/// <summary>
/// Cleans assistant output before it is shown and recovers tool calls that a
/// model emits in DSML-style special tokens (for example
/// &lt;｜DSML｜tool_call_start｜&gt;{...}&lt;｜DSML｜tool_call_end｜&gt;) instead of
/// standard function calling. The recovered calls let the agent continue the
/// turn instead of stopping on the raw markup.
/// </summary>
public static class AgentTextCleaner
{
    private static readonly Regex ToolCallBlockRegex = new(
        "<｜DSML｜tool_call_start｜>(?<json>.*?)<｜DSML｜tool_call_end｜>",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex StandaloneMarkerRegex = new(
        "<｜DSML｜[^｜]*?｜>",
        RegexOptions.Compiled);

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
        cleaned = StandaloneMarkerRegex.Replace(cleaned, string.Empty);
        cleaned = MarkerRegex.Replace(cleaned, string.Empty);
        return cleaned.Trim();
    }

    /// <summary>
    /// Removes only complete DSML tool-call blocks from text. Incomplete
    /// markers are left in place because the closing tag has not arrived yet;
    /// <see cref="StripSpecialTokens"/> removes them once the turn ends.
    /// </summary>
    public static string SanitizeStreaming(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        return ToolCallBlockRegex.Replace(text, string.Empty);
    }

    /// <summary>
    /// Extracts tool calls encoded as DSML blocks from assistant text and
    /// returns the text with those blocks removed. Blocks whose JSON cannot be
    /// parsed are dropped (they are never shown to the user).
    /// </summary>
    public static List<FunctionCallContent> ExtractToolCalls(string? text, out string cleanedText)
    {
        var calls = new List<FunctionCallContent>();
        if (string.IsNullOrWhiteSpace(text))
        {
            cleanedText = string.Empty;
            return calls;
        }

        var builder = new StringBuilder();
        var position = 0;

        foreach (Match match in ToolCallBlockRegex.Matches(text))
        {
            builder.Append(text, position, match.Index - position);
            position = match.Index + match.Length;

            var call = TryParseToolCall(match.Groups["json"].Value);
            if (call is not null)
            {
                calls.Add(call);
            }
        }

        builder.Append(text, position, text.Length - position);
        cleanedText = builder.ToString();
        return calls;
    }

    private static FunctionCallContent? TryParseToolCall(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            if (!root.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
            {
                return null;
            }

            var name = nameElement.GetString();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            var arguments = new Dictionary<string, object?>();
            if (root.TryGetProperty("arguments", out var argumentsElement))
            {
                arguments = ParseArguments(argumentsElement);
            }

            return new FunctionCallContent("dsml-" + Guid.NewGuid().ToString("N"), name, arguments);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Dictionary<string, object?> ParseArguments(JsonElement element)
    {
        var result = new Dictionary<string, object?>();

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    result[property.Name] = ToObject(property.Value);
                }

                break;

            case JsonValueKind.String:
                // Some models emit the arguments as an encoded JSON string.
                try
                {
                    using var doc = JsonDocument.Parse(element.GetString() ?? string.Empty);
                    return ParseArguments(doc.RootElement);
                }
                catch (JsonException)
                {
                    // Fall through and return an empty arguments map.
                }

                break;
        }

        return result;
    }

    private static object? ToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var whole) ? whole : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        JsonValueKind.Array => element.EnumerateArray().Select(ToObject).ToArray(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ToObject(p.Value)),
        _ => null,
    };
}
