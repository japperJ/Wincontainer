namespace WinContainers.Service.Mcp;

public static class McpDestructiveConfirmationPolicy
{
    public static bool Enabled { get; private set; } = true;

    public static void SetEnabled(bool enabled) => Enabled = enabled;
}
