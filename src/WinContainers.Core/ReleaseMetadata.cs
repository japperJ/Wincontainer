namespace WinContainers.Core;

public static class ReleaseMetadata
{
    public static bool IsValidTag(string tag) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            tag,
            "^v[0-9]+\\.[0-9]+\\.[0-9]+(?:-[0-9A-Za-z.-]+)?$",
            System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static string GetChannel(string tag) =>
        tag.Contains('-', StringComparison.Ordinal) ? "Beta" : "Stable";

    public static string GetVersion(string tag) =>
        IsValidTag(tag) ? tag[1..] : throw new ArgumentException("Tag must be SemVer.", nameof(tag));
}
