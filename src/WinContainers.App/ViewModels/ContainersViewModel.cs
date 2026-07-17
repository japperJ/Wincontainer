using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Pages;
using WinContainers_App.Services;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public partial class ContainersViewModel : ViewModelBase
{
    private const int BackgroundPollIntervalMs = 10000;
    private readonly IOutputService _output;
    private readonly ContainerService _containerService;
    private readonly IDialogService _dialog;
    private readonly INavigationService _navigation;

    private List<ContainerCardData> _allContainers = [];

    private readonly HashSet<string> _expandedProjects = [];

    private readonly Dictionary<string, string> _projectDisplayNames = [];
    private readonly Dictionary<string, string> _containerDisplayNames = [];

    private CancellationTokenSource? _pollCts;

    private ObservableCollection<object>? _containerItems;
    public ObservableCollection<object>? ContainerItems
    {
        get => _containerItems;
        set => SetProperty(ref _containerItems, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private string? _lastRefreshText;
    public string? LastRefreshText
    {
        get => _lastRefreshText;
        set => SetProperty(ref _lastRefreshText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand StartContainerCommand { get; }
    public ICommand StopContainerCommand { get; }
    public ICommand RemoveContainerCommand { get; }
    public ICommand StartGroupCommand { get; }
    public ICommand StopGroupCommand { get; }
    public ICommand RemoveGroupCommand { get; }

    public ContainersViewModel(
        IOutputService output,
        ContainerService containerService,
        IDialogService dialog,
        INavigationService navigation)
    {
        _output = output;
        _containerService = containerService;
        _dialog = dialog;
        _navigation = navigation;

        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        StartContainerCommand = new AsyncRelayCommand<string?>(async id =>
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            await RunContainerActionAsync("Start", id);
        });
        StopContainerCommand = new AsyncRelayCommand<string?>(async id =>
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            await RunContainerActionAsync("Stop", id);
        });
        RemoveContainerCommand = new AsyncRelayCommand<string?>(async id =>
        {
            if (string.IsNullOrWhiteSpace(id)) return;
            await RunContainerActionAsync("Remove", id);
        });
        StartGroupCommand = new AsyncRelayCommand<ContainerGroup?>(async group =>
        {
            if (group is null) return;
            await RunGroupActionAsync("Start", group);
        });
        StopGroupCommand = new AsyncRelayCommand<ContainerGroup?>(async group =>
        {
            if (group is null) return;
            await RunGroupActionAsync("Stop", group);
        });
        RemoveGroupCommand = new AsyncRelayCommand<ContainerGroup?>(async group =>
        {
            if (group is null) return;
            await RunGroupActionAsync("Remove", group);
        });
    }

    public async Task RefreshAsync()
    {
        try
        {
            var output = await App.ServiceClient.GetContainersAsync();
            var combined = _containerService.ParseContainerEntries(output ?? "");

            if (combined.Count == 0 && _allContainers.Count > 0)
            {
                _output.Write("Preserving existing container list (refresh returned 0 entries)", ServiceLogLevel.Warning);
                return;
            }

            _allContainers = combined;
            ApplyContainerDisplayNames(_allContainers);
            App.DispatcherQueue.TryEnqueue(() =>
            {
                RebuildGroupedList();
                LastRefreshText = $"{combined.Count} container(s) — {DateTime.Now:HH:mm:ss}";
            });
        }
        catch (Exception ex)
        {
            _output.Write($"Container refresh failed: {ex.Message}", ServiceLogLevel.Warning);
        }
    }

    private void RebuildGroupedList()
    {
        const string StandaloneKey = "\0";

        var groups = new Dictionary<string, List<ContainerCardData>>();
        foreach (var c in _allContainers)
        {
            var project = c.ProjectName;
            if (string.IsNullOrWhiteSpace(project))
                project = StandaloneKey;
            if (!groups.ContainsKey(project))
                groups[project] = [];
            groups[project].Add(c);
        }

        ContainerItems ??= [];
        ContainerItems.Clear();
        foreach (var kvp in groups)
        {
            var projectName = kvp.Key;
            var containers = kvp.Value;

            if (projectName == StandaloneKey)
            {
                foreach (var c in containers)
                {
                    c.IsInGroup = false;
                    ContainerItems.Add(c);
                }
            }
            else
            {
                var isExpanded = _expandedProjects.Contains(projectName);
                var displayName = _projectDisplayNames.TryGetValue(projectName, out var dn) ? dn : projectName;
                var group = new ContainerGroup
                {
                    ProjectName = projectName,
                    DisplayName = displayName,
                    IsExpanded = isExpanded,
                };
                foreach (var c in containers)
                {
                    c.IsInGroup = true;
                    group.Containers.Add(c);
                }

                ContainerItems.Add(group);

                if (isExpanded)
                {
                    foreach (var c in containers)
                        ContainerItems.Add(c);
                }
            }
        }
    }

    public void ToggleGroupExpanded(ContainerGroup group)
    {
        if (group.IsExpanded)
        {
            var headerIndex = ContainerItems?.IndexOf(group) ?? -1;
            if (headerIndex < 0) return;

            for (int i = 0; i < group.Containers.Count; i++)
                ContainerItems!.RemoveAt(headerIndex + 1);

            group.IsExpanded = false;
            _expandedProjects.Remove(group.ProjectName);
        }
        else
        {
            var headerIndex = ContainerItems?.IndexOf(group) ?? -1;
            if (headerIndex < 0) return;

            var insertIndex = headerIndex + 1;
            foreach (var container in group.Containers)
                ContainerItems!.Insert(insertIndex++, container);

            group.IsExpanded = true;
            _expandedProjects.Add(group.ProjectName);
        }
    }

    public ContainerCardData? FindContainer(string id)
        => _allContainers.FirstOrDefault(c => c.Id == id || c.Name == id);

    public async Task RunContainerActionAsync(string action, string id, string? newName = null, List<string>? volumesToRemove = null)
    {
        var container = _allContainers.FirstOrDefault(c => c.Id == id || c.Name == id);
        if (container is not null)
            container.IsBusy = true;

        _output.Write($"Running {action} for {id}...");

        try
        {
            if (action == "Remove" && volumesToRemove?.Count > 0)
            {
                foreach (var vol in volumesToRemove)
                {
                    var volOutput = await App.ServiceClient.RemoveVolumeAsync(vol);
                    _output.Write($"Removed volume '{vol}': {volOutput}");
                }
            }

            var output = action switch
            {
                "Start" => await App.ServiceClient.StartContainerAsync(id),
                "Stop" => await App.ServiceClient.StopContainerAsync(id),
                "Remove" => await App.ServiceClient.RemoveContainerAsync(id),
                "Rename" => RenameContainerAsync(id, newName),
                _ => null
            };

            if (output is not null)
                _output.Write($"{action} {id}: {output}");
        }
        finally
        {
            if (container is not null)
                container.IsBusy = false;
        }

        await RefreshAsync();
    }

    private string? RenameContainerAsync(string id, string? newName)
    {
        var normalized = newName?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
            return "Rename skipped: no new name provided.";

        var container = _allContainers.FirstOrDefault(c => c.Id == id || c.Name == id);
        if (container is null)
            return $"Rename skipped: container '{id}' was not found.";

        _containerDisplayNames[container.Id] = normalized;
        container.Name = normalized;
        _output.Write($"Renamed container display name '{container.Id}' -> '{normalized}' (UI-only).");
        return $"Display name updated to '{normalized}'.";
    }

    private void ApplyContainerDisplayNames(List<ContainerCardData> containers)
    {
        var activeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var container in containers)
        {
            activeIds.Add(container.Id);
            if (_containerDisplayNames.TryGetValue(container.Id, out var displayName) &&
                !string.IsNullOrWhiteSpace(displayName))
            {
                container.Name = displayName;
            }
        }

        foreach (var staleId in _containerDisplayNames.Keys.Where(id => !activeIds.Contains(id)).ToList())
            _containerDisplayNames.Remove(staleId);
    }

    public async Task RunGroupActionAsync(string action, ContainerGroup group)
    {
        group.IsBusy = true;
        foreach (var c in group.Containers)
            c.IsBusy = true;
        _output.Write($"Running {action} for group '{group.ProjectName}' ({group.Containers.Count} container(s))...");

        try
        {
            foreach (var container in group.Containers)
            {
                if (action == "Start" && ContainerService.IsRunningStatus(container.Status))
                {
                    _output.Write($"  Skipping {container.Name} (already running)");
                    continue;
                }

                var output = action switch
                {
                    "Start" => await App.ServiceClient.StartContainerAsync(container.Id),
                    "Stop" => await App.ServiceClient.StopContainerAsync(container.Id),
                    "Remove" => await App.ServiceClient.RemoveContainerAsync(container.Id),
                    _ => null
                };
                _output.Write($"  {container.Name}: {output ?? "(skipped)"}");
            }
        }
        finally
        {
            group.IsBusy = false;
            foreach (var c in group.Containers)
                c.IsBusy = false;
        }

        await RefreshAsync();
    }

    public async Task RunGroupRenameAsync(string newName, ContainerGroup group)
    {
        _output.Write($"Renaming group '{group.ProjectName}' → '{newName}' (UI-only rename, labels unchanged).");

        _projectDisplayNames[group.ProjectName] = newName;

        _expandedProjects.Remove(group.ProjectName);
        _expandedProjects.Add(newName);

        await RefreshAsync();
    }

    public void NavigateToDetail(ContainerCardData entry)
    {
        _output.Write($"Selected container: {entry.Name} ({entry.Id})");
        _navigation.NavigateTo<ContainerDetailPage>(entry);
    }

    public void StartPolling()
    {
        if (_pollCts is not null) return;
        _pollCts = new CancellationTokenSource();
        _ = PollLoopAsync(_pollCts.Token);
    }

    public void StopPolling()
    {
        _pollCts?.Cancel();
        _pollCts = null;
    }

    private async Task PollLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await RefreshAsync();
            await Task.Delay(BackgroundPollIntervalMs, ct);
        }
    }
}
