using System.ComponentModel;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinContainers.Runtime.Models;

public sealed record PortLinkItem(string Url, string Detail);

public sealed record MountInfo(string Source, string Target);

public sealed partial class ContainerCardData : ObservableObject
{
    public string Id { get; set; } = string.Empty;
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }
    public bool IsInGroup { get; set; }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set
    {
        if (SetProperty(ref _status, value))
        {
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanAct));
                OnPropertyChanged(nameof(CanRemove));
                OnPropertyChanged(nameof(StatusDisplay));
            }
        }
    }

    public bool CanAct => !IsBusy;

    public string StatusDisplay => IsBusy ? "Working..." : Status;

    public string Image { get; set; } = string.Empty;
    public string Ports { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public List<PortLinkItem> PortLinks { get; set; } = [];

    public bool CanRemove => !IsBusy
        && !Status.StartsWith("Up", StringComparison.OrdinalIgnoreCase)
        && !Status.StartsWith("Running", StringComparison.OrdinalIgnoreCase);

    public Dictionary<string, string>? Labels { get; set; }
    public List<MountInfo> MountInfos { get; set; } = [];

    public string? ProjectName
    {
        get
        {
            if (Labels == null) return null;
            if (Labels.TryGetValue("com.docker.compose.project", out var project))
                return project;
            if (Labels.TryGetValue("com.wincontainers.project", out var wcProject))
                return wcProject;
            return null;
        }
    }

    public static List<PortLinkItem> ParsePortLinksStatic(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return [];
        var links = new List<PortLinkItem>();
        foreach (var part in raw.Split(',', StringSplitOptions.TrimEntries))
        {
            var m = Regex.Match(part, @":(\d+)\s*->\s*(\d+)/(\w+)");
            if (m.Success)
                links.Add(new PortLinkItem(
                    $"localhost:{m.Groups[1].Value}",
                    $" -> {m.Groups[2].Value}/{m.Groups[3].Value}"));
        }
        return links;
    }
}

public sealed class ContainerGroup : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProjectName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsExpanded { get; set; } = true;
    public List<ContainerCardData> Containers { get; set; } = [];

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value) return;
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanAct));
            OnPropertyChanged(nameof(CanRemove));
            OnPropertyChanged(nameof(StatusDisplay));
        }
    }

    public bool CanAct => !IsBusy;

    public int ContainerCount => Containers.Count;

    public int RunningCount => Containers.Count(c =>
        c.Status.StartsWith("Up", StringComparison.OrdinalIgnoreCase) ||
        c.Status.StartsWith("Running", StringComparison.OrdinalIgnoreCase));

    public string StatusSummary => ContainerCount == 0 ? "No containers"
        : RunningCount == 0 ? "All stopped"
        : RunningCount == ContainerCount ? "All running"
        : $"{RunningCount}/{ContainerCount} running";

    public string GroupDotStatus => ContainerCount == 0 ? "Unknown"
        : RunningCount == 0 ? "Exited (0)"
        : RunningCount == ContainerCount ? "Running"
        : "Partial";
    public bool CanRemove => !IsBusy
        && RunningCount == 0;

    public string StatusDisplay => IsBusy ? "Working..." : StatusSummary;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
