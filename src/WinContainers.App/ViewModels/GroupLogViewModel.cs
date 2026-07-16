using System.Text;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class GroupLogViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly INavigationService _navigation;

    private string? _groupName;
    public string? GroupName
    {
        get => _groupName;
        set => SetProperty(ref _groupName, value);
    }

    private int _containerCount;
    public int ContainerCount
    {
        get => _containerCount;
        set => SetProperty(ref _containerCount, value);
    }

    private string? _logsContent;
    public string? LogsContent
    {
        get => _logsContent;
        set => SetProperty(ref _logsContent, value);
    }

    private string? _logsInfoText;
    public string? LogsInfoText
    {
        get => _logsInfoText;
        set => SetProperty(ref _logsInfoText, value);
    }

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    private List<ContainerCardData> _containers = [];

    public GroupLogViewModel(
        IOutputService output,
        INavigationService navigation)
    {
        _output = output;
        _navigation = navigation;
    }

    public void LoadGroup(ContainerGroup group)
    {
        GroupName = group.DisplayName;
        _containers = [.. group.Containers];
        ContainerCount = _containers.Count;
    }

    public async Task LoadLogsAsync()
    {
        IsLoading = true;
        LogsInfoText = $"Loading logs for {_containers.Count} container(s)...";
        try
        {
            var tasks = _containers.Select(async container =>
            {
                var output = await App.ServiceClient.GetContainerLogsAsync(container.Id, 500);
                return (Container: container, Output: output);
            }).ToArray();

            var results = await Task.WhenAll(tasks);

            var sb = new StringBuilder();
            var totalChars = 0;
            foreach (var (container, output) in results)
            {
                var prefix = $"[{container.Name}] ";
                var outputText = string.IsNullOrWhiteSpace(output) ? "(no logs)" : output;
                var lines = outputText.Split('\n', StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (line.Length == 0)
                        sb.AppendLine(prefix);
                    else
                        sb.AppendLine($"{prefix}{line}");
                }
                sb.AppendLine();
                totalChars += output?.Length ?? 0;
            }

            LogsContent = sb.ToString();
            LogsInfoText = $"{_containers.Count} container(s) — {totalChars} chars";
        }
        catch (Exception ex)
        {
            LogsContent = $"Failed to load logs: {ex.Message}";
            LogsInfoText = "Error";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void NavigateBack()
    {
        MainWindow.ReturnToPivotIndex = 1; // Containers tab
        _navigation.GoBack();
    }
}
