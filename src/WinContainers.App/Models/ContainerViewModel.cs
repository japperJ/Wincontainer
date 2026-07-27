using System.ComponentModel;
using System.Runtime.CompilerServices;
using WinContainers.Runtime.Models;

namespace WinContainers_App.Models;

public sealed record PortLinkViewModel(string Url, string Detail);
public sealed record MountInfoViewModel(string Source, string Target);

public sealed class ContainerViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string Id { get; set; } = string.Empty;

    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set { if (SetProperty(ref _name, value ?? string.Empty)) { OnPropertyChanged(nameof(CanRemove)); OnPropertyChanged(nameof(StatusDisplay)); } }
    }

    public bool IsInGroup { get; set; }

    private string _status = string.Empty;
    public string Status
    {
        get => _status;
        set { if (SetProperty(ref _status, value)) { OnPropertyChanged(nameof(CanRemove)); OnPropertyChanged(nameof(StatusDisplay)); } }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (SetProperty(ref _isBusy, value)) { OnPropertyChanged(nameof(CanAct)); OnPropertyChanged(nameof(CanRemove)); OnPropertyChanged(nameof(StatusDisplay)); } }
    }

    public bool CanAct => !IsBusy;
    public string StatusDisplay => IsBusy ? "Working..." : Status;

    public string Image { get; set; } = string.Empty;
    public string Ports { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public List<PortLinkViewModel> PortLinks { get; set; } = [];

    public bool CanRemove => !IsBusy
        && !Status.StartsWith("Up", StringComparison.OrdinalIgnoreCase)
        && !Status.StartsWith("Running", StringComparison.OrdinalIgnoreCase);

    public Dictionary<string, string>? Labels { get; set; }
    public List<MountInfoViewModel> MountInfos { get; set; } = [];

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

    public static ContainerViewModel FromCardData(ContainerCardData card) => new()
    {
        Id = card.Id,
        Name = card.Name,
        IsInGroup = card.IsInGroup,
        Status = card.Status,
        IsBusy = card.IsBusy,
        Image = card.Image,
        Ports = card.Ports,
        Command = card.Command,
        CreatedAt = card.CreatedAt,
        PortLinks = card.PortLinks.Select(p => new PortLinkViewModel(p.Url, p.Detail)).ToList(),
        Labels = card.Labels,
        MountInfos = card.MountInfos.Select(m => new MountInfoViewModel(m.Source, m.Target)).ToList(),
    };

    public static List<ContainerViewModel> FromCardDataList(List<ContainerCardData> cards)
        => cards.Select(FromCardData).ToList();

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
