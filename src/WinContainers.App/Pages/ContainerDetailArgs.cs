namespace WinContainers_App.Pages;

public sealed record ContainerDetailArgs(
    string Id,
    string Name,
    string Status,
    string Image,
    string Ports,
    string CreatedAt);
