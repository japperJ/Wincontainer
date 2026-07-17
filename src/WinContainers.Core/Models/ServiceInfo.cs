namespace WinContainers.Core.Models;

public sealed record ServiceInfo(string Port, string Token)
{
    public IReadOnlyList<string> Scripts { get; init; } = Array.Empty<string>();
}
