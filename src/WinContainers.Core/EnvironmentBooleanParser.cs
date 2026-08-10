namespace WinContainers.Core;

public static class EnvironmentBooleanParser
{
    public static bool TryParse(string? value, out bool result)
    {
        switch (value?.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
                result = true;
                return true;
            case "0":
            case "false":
                result = false;
                return true;
            default:
                result = default;
                return false;
        }
    }
}
