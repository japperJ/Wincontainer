namespace WinContainers.Runtime.Models;

public sealed class ResourceEntryData
{
    public string Name { get; init; } = string.Empty;
    public string Details { get; init; } = string.Empty;
    public bool CanDelete { get; init; } = true;
}
