namespace WinContainers.Runtime.Models;

public sealed class ImageEntryData
{
    public string Repository { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string ID { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public bool InUse { get; set; }
    public string FullTag => $"{Repository}:{Tag}";
}

public sealed class ImageLayerData
{
    public string Snapshot { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string CreatedSince { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
}
