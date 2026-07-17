namespace WinContainers_App.Pages;

public sealed class ContainerCardEntry
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public string Ports { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
}
