using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinContainers_App.Models;

public sealed class ContainerGroupViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProjectName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsExpanded { get; set; } = true;
    public List<ContainerViewModel> Containers { get; set; } = [];

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set { if (SetProperty(ref _isBusy, value)) { OnPropertyChanged(nameof(CanAct)); OnPropertyChanged(nameof(CanRemove)); OnPropertyChanged(nameof(StatusDisplay)); } }
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

    public bool CanRemove => !IsBusy && RunningCount == 0;
    public string StatusDisplay => IsBusy ? "Working..." : StatusSummary;

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
