using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Services;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public sealed class CommandParamValue : ObservableObject
{
    private string? _value;
    public string? Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public CommandParamType Type { get; set; }
    public bool Required { get; set; } = true;
    public ObservableCollection<string> Options { get; set; } = new();

    public bool IsDropdown => Type != CommandParamType.Text;
    public bool IsTextbox => Type == CommandParamType.Text;
}

public partial class TerminalViewModel : ViewModelBase
{

    #region Constructor and Fields

    private readonly IOutputService _output;

    public TerminalViewModel(IOutputService output)
    {
        _output = output;
        BuildCommandList();
    }

    #endregion

    #region Observable Properties

    public ObservableCollection<TerminalCategory> Categories { get; } = new();
    public ObservableCollection<CommandParamValue> ParameterValues { get; } = new();
    public ObservableCollection<TerminalHistoryEntry> History { get; } = new();

    private TerminalCommand? _selectedCommand;
    public TerminalCommand? SelectedCommand
    {
        get => _selectedCommand;
        set
        {
            if (SetProperty(ref _selectedCommand, value))
            {
                OnCommandChanged();
                OnPropertyChanged(nameof(HasSelectedCommand));
            }
        }
    }

    public bool HasSelectedCommand => _selectedCommand is not null;

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set => SetProperty(ref _isRunning, value);
    }

    private string? _outputText;
    public string? OutputText
    {
        get => _outputText;
        set
        {
            if (SetProperty(ref _outputText, value))
                OnPropertyChanged(nameof(HasOutput));
        }
    }

    public bool HasOutput => !string.IsNullOrWhiteSpace(_outputText);

    private string? _commandPreview;
    public string? CommandPreview
    {
        get => _commandPreview;
        set
        {
            if (SetProperty(ref _commandPreview, value))
                OnPropertyChanged(nameof(HasCommandPreview));
        }
    }

    public bool HasCommandPreview => !string.IsNullOrWhiteSpace(_commandPreview);

    private string? _wslcCommandPreview;
    public string? WslcCommandPreview
    {
        get => _wslcCommandPreview;
        set
        {
            if (SetProperty(ref _wslcCommandPreview, value))
                OnPropertyChanged(nameof(HasWslcCommandPreview));
        }
    }

    public bool HasWslcCommandPreview => !string.IsNullOrWhiteSpace(_wslcCommandPreview);

    #endregion

    #region Command Management

    private sealed record CommandParameterMetadata(
        string Name,
        string DisplayName,
        CommandParamType Type,
        bool Required = true);

    private sealed record TerminalCommandMetadata(
        string Name,
        string DisplayName,
        string Category,
        string Description,
        string ApiTemplate,
        IReadOnlyList<CommandParameterMetadata> Parameters,
        Func<IReadOnlyDictionary<string, string>, string?> BuildWslcArgs,
        Func<IReadOnlyDictionary<string, string>, Task<string>> ExecuteAsync);

    private static readonly IReadOnlyList<TerminalCommandMetadata> CommandDefinitions =
    [
        new(
            Name: "Get-Health",
            DisplayName: "Check Health",
            Category: "System",
            Description: "Check if WSLC service is healthy",
            ApiTemplate: "GET /api/health",
            Parameters: [],
            BuildWslcArgs: _ => WslcCommands.Version(),
            ExecuteAsync: async _ => await App.ServiceClient.IsHealthyAsync() ? "Healthy" : "Unhealthy"),
        new(
            Name: "Get-Version",
            DisplayName: "Get Version",
            Category: "System",
            Description: "Show WSLC runtime version",
            ApiTemplate: "GET /api/runtime/version",
            Parameters: [],
            BuildWslcArgs: _ => WslcCommands.Version(),
            ExecuteAsync: async _ => await App.ServiceClient.GetVersionAsync()),
        new(
            Name: "Get-Container",
            DisplayName: "List Containers",
            Category: "Containers",
            Description: "List all containers",
            ApiTemplate: "GET /api/containers",
            Parameters: [new("Format", "Output Format", CommandParamType.Format, Required: false)],
            BuildWslcArgs: _ => WslcCommands.ContainerPs(),
            ExecuteAsync: async _ => await App.ServiceClient.GetContainersAsync()),
        new(
            Name: "Start-Container",
            DisplayName: "Start Container",
            Category: "Containers",
            Description: "Start a stopped container",
            ApiTemplate: "POST /api/containers/{Id}/start",
            Parameters: [new("Id", "Container", CommandParamType.ContainerId)],
            BuildWslcArgs: p => WslcCommands.ContainerStart(GetParam(p, "Id", "<id>")),
            ExecuteAsync: async p => await App.ServiceClient.StartContainerAsync(GetParam(p, "Id"))),
        new(
            Name: "Stop-Container",
            DisplayName: "Stop Container",
            Category: "Containers",
            Description: "Stop a running container",
            ApiTemplate: "POST /api/containers/{Id}/stop",
            Parameters: [new("Id", "Container", CommandParamType.ContainerId)],
            BuildWslcArgs: p => WslcCommands.ContainerStop(GetParam(p, "Id", "<id>")),
            ExecuteAsync: async p => await App.ServiceClient.StopContainerAsync(GetParam(p, "Id"))),
        new(
            Name: "Restart-Container",
            DisplayName: "Restart Container",
            Category: "Containers",
            Description: "Restart a container",
            ApiTemplate: "POST /api/containers/{Id}/restart",
            Parameters: [new("Id", "Container", CommandParamType.ContainerId)],
            BuildWslcArgs: p => WslcCommands.ContainerRestart(GetParam(p, "Id", "<id>")),
            ExecuteAsync: async p => await App.ServiceClient.RestartContainerAsync(GetParam(p, "Id"))),
        new(
            Name: "Remove-Container",
            DisplayName: "Remove Container",
            Category: "Containers",
            Description: "Delete a container",
            ApiTemplate: "DELETE /api/containers/{Id}",
            Parameters: [new("Id", "Container", CommandParamType.ContainerId)],
            BuildWslcArgs: p => WslcCommands.ContainerRemove(GetParam(p, "Id", "<id>")),
            ExecuteAsync: async p => await App.ServiceClient.RemoveContainerAsync(GetParam(p, "Id"))),
        new(
            Name: "Get-ContainerLogs",
            DisplayName: "Container Logs",
            Category: "Containers",
            Description: "Show container logs",
            ApiTemplate: "GET /api/containers/{Id}/logs",
            Parameters:
            [
                new("Id", "Container", CommandParamType.ContainerId),
                new("Tail", "Tail Lines", CommandParamType.Text, Required: false)
            ],
            BuildWslcArgs: p => WslcCommands.ContainerLogs(GetParam(p, "Id", "<id>"), GetTailLines(p, 500)),
            ExecuteAsync: async p => await App.ServiceClient.GetContainerLogsAsync(GetParam(p, "Id"), GetTailLines(p, 500))),
        new(
            Name: "Get-Image",
            DisplayName: "List Images",
            Category: "Images",
            Description: "List pulled images",
            ApiTemplate: "GET /api/images",
            Parameters: [],
            BuildWslcArgs: _ => WslcCommands.ImageLs(),
            ExecuteAsync: async _ => await App.ServiceClient.GetImagesAsync()),
        new(
            Name: "Pull-Image",
            DisplayName: "Pull Image",
            Category: "Images",
            Description: "Download an image from a registry",
            ApiTemplate: "POST /api/images/pull",
            Parameters: [new("Image", "Image", CommandParamType.ImageName)],
            BuildWslcArgs: p => WslcCommands.ImagePull(GetParam(p, "Image", "<image>")),
            ExecuteAsync: async p => await App.ServiceClient.PullImageAsync(GetParam(p, "Image"))),
        new(
            Name: "Remove-Image",
            DisplayName: "Remove Image",
            Category: "Images",
            Description: "Delete an image from local storage",
            ApiTemplate: "DELETE /api/images/{Id}",
            Parameters: [new("Id", "Container", CommandParamType.ContainerId)],
            BuildWslcArgs: p => WslcCommands.ImageRemove(GetParam(p, "Id", "<id>")),
            ExecuteAsync: async p => await App.ServiceClient.RemoveImageAsync(GetParam(p, "Id"))),
        new(
            Name: "Get-Volumes",
            DisplayName: "List Volumes",
            Category: "Volumes",
            Description: "List all volumes",
            ApiTemplate: "GET /api/volumes",
            Parameters: [],
            BuildWslcArgs: _ => WslcCommands.VolumeLs(),
            ExecuteAsync: async _ => await App.ServiceClient.GetVolumesAsync()),
        new(
            Name: "Create-Volume",
            DisplayName: "Create Volume",
            Category: "Volumes",
            Description: "Create a new volume",
            ApiTemplate: "POST /api/volumes",
            Parameters: [new("Name", "Name", CommandParamType.Text)],
            BuildWslcArgs: p => WslcCommands.VolumeCreate(GetParam(p, "Name", "<name>")),
            ExecuteAsync: async p => await App.ServiceClient.CreateVolumeAsync(GetParam(p, "Name"))),
        new(
            Name: "Remove-Volume",
            DisplayName: "Remove Volume",
            Category: "Volumes",
            Description: "Delete a volume",
            ApiTemplate: "DELETE /api/volumes/{Name}",
            Parameters: [new("Name", "Name", CommandParamType.Text)],
            BuildWslcArgs: p => WslcCommands.VolumeRemove(GetParam(p, "Name", "<name>")),
            ExecuteAsync: async p => await App.ServiceClient.RemoveVolumeAsync(GetParam(p, "Name"))),
        new(
            Name: "Get-Networks",
            DisplayName: "List Networks",
            Category: "Networks",
            Description: "List all networks",
            ApiTemplate: "GET /api/networks",
            Parameters: [],
            BuildWslcArgs: _ => WslcCommands.NetworkLs(),
            ExecuteAsync: async _ => await App.ServiceClient.GetNetworksAsync()),
        new(
            Name: "Create-Network",
            DisplayName: "Create Network",
            Category: "Networks",
            Description: "Create a new network",
            ApiTemplate: "POST /api/networks",
            Parameters: [new("Name", "Name", CommandParamType.Text)],
            BuildWslcArgs: p => WslcCommands.NetworkCreate(GetParam(p, "Name", "<name>")),
            ExecuteAsync: async p => await App.ServiceClient.CreateNetworkAsync(GetParam(p, "Name"))),
        new(
            Name: "Remove-Network",
            DisplayName: "Remove Network",
            Category: "Networks",
            Description: "Delete a network",
            ApiTemplate: "DELETE /api/networks/{Name}",
            Parameters: [new("Name", "Name", CommandParamType.Text)],
            BuildWslcArgs: p => WslcCommands.NetworkRemove(GetParam(p, "Name", "<name>")),
            ExecuteAsync: async p => await App.ServiceClient.RemoveNetworkAsync(GetParam(p, "Name")))
    ];

    private static readonly IReadOnlyDictionary<string, TerminalCommandMetadata> CommandDefinitionsByName =
        CommandDefinitions.ToDictionary(cmd => cmd.Name, StringComparer.Ordinal);

    private void BuildCommandPreview()
    {
        if (SelectedCommand is null || !CommandDefinitionsByName.TryGetValue(SelectedCommand.Name, out var commandDef))
        {
            CommandPreview = null;
            WslcCommandPreview = null;
            return;
        }

        var parameters = new Dictionary<string, string>();
        foreach (var pv in ParameterValues)
        {
            if (!string.IsNullOrWhiteSpace(pv.Value))
                parameters[pv.Name] = pv.Value;
        }

        var parametersByName = ParameterValues.ToDictionary(p => p.Name, StringComparer.Ordinal);
        var result = ParamRegex.Replace(commandDef.ApiTemplate, match =>
        {
            var parameterName = match.Groups[1].Value;
            if (parameters.TryGetValue(parameterName, out var value))
                return value;

            return parametersByName.TryGetValue(parameterName, out var parameter) && parameter.Required
                ? $"<{parameter.DisplayName}>"
                : string.Empty;
        });

        CommandPreview = result;
        WslcCommandPreview = BuildWslcCommandPreview(commandDef, parameters);
    }

    private static string? BuildWslcCommandPreview(TerminalCommandMetadata commandDef, IReadOnlyDictionary<string, string> parameters)
    {
        var args = commandDef.BuildWslcArgs(parameters);

        return args is null ? null : $"wslc {args}";
    }

    public async Task InitializeAsync()
    {
        await RefreshDropdownOptionsAsync();
    }

    private async Task RefreshDropdownOptionsAsync()
    {
        var containerIds = await FetchContainerIdsAsync();
        var imageNames = await FetchImageNamesAsync();

        foreach (var param in ParameterValues)
        {
            if (param.Type == CommandParamType.ContainerId)
            {
                param.Options.Clear();
                foreach (var id in containerIds)
                    param.Options.Add(id);
            }
            else if (param.Type == CommandParamType.ImageName)
            {
                param.Options.Clear();
                foreach (var img in imageNames)
                    param.Options.Add(img);
            }
            else if (param.Type == CommandParamType.RestartPolicy)
            {
                param.Options.Clear();
                param.Options.Add("no");
                param.Options.Add("always");
                param.Options.Add("unless-stopped");
            }
            else if (param.Type == CommandParamType.Format)
            {
                param.Options.Clear();
                param.Options.Add("default");
                param.Options.Add("table");
                param.Options.Add("json");
            }
        }
    }

    private void OnCommandChanged()
    {
        ParameterValues.Clear();
        OutputText = null;
        CommandPreview = null;
        WslcCommandPreview = null;

        if (SelectedCommand is null) return;

        foreach (var def in SelectedCommand.Parameters)
        {
            var pv = new CommandParamValue
            {
                Name = def.Name,
                DisplayName = def.DisplayName,
                Type = def.Type,
                Required = def.Required
            };
            pv.PropertyChanged += (_, _) => BuildCommandPreview();
            ParameterValues.Add(pv);
        }

        BuildCommandPreview();
    }

    #endregion

    #region History

    private bool _historyLoaded;
    private const string HistoryFile = "terminal-history.json";

    private static string? GetHistoryPath()
    {
        try
        {
            return System.IO.Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, HistoryFile);
        }
        catch
        {
            var fallback = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WinContainers", "terminal-history.json");
            var dir = System.IO.Path.GetDirectoryName(fallback);
            if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            return fallback;
        }
    }

    public void LoadHistory()
    {
        if (_historyLoaded) return;
        _historyLoaded = true;

        try
        {
            var path = GetHistoryPath();
            if (path is null) return;
            if (!System.IO.File.Exists(path)) return;
            var json = System.IO.File.ReadAllText(path);
            var entries = JsonSerializer.Deserialize<List<TerminalHistoryEntry>>(json);
            if (entries is null) return;
            History.Clear();
            foreach (var e in entries)
                History.Add(e);
        }
        catch (Exception ex) { _output.Write($"LoadHistory failed: {ex.Message}", ServiceLogLevel.Warning); }
    }

    public void SaveHistory()
    {
        try
        {
            var path = GetHistoryPath();
            if (path is null) return;
            var json = JsonSerializer.Serialize(History.ToList());
            System.IO.File.WriteAllText(path, json);
        }
        catch (Exception ex) { _output.Write($"SaveHistory failed: {ex.Message}", ServiceLogLevel.Warning); }
    }

    #endregion

    #region Execution

    [RelayCommand]
    private async Task RunAsync()
    {
        if (SelectedCommand is null || IsRunning) return;

        var missing = ParameterValues.FirstOrDefault(p => p.Required && string.IsNullOrWhiteSpace(p.Value));
        if (missing is not null)
        {
            OutputText = $"Please fill in: {missing.DisplayName}";
            return;
        }

        IsRunning = true;
        OutputText = null;

        try
        {
            var parameters = new Dictionary<string, string>();
            foreach (var pv in ParameterValues)
            {
                if (!string.IsNullOrWhiteSpace(pv.Value))
                    parameters[pv.Name] = pv.Value;
            }

            _output.Write($"Running {SelectedCommand.Name}...");
            var output = await ExecuteCommandAsync(SelectedCommand.Name, parameters);
            _output.Write($"{SelectedCommand.Name}: {output}");

            OutputText = string.IsNullOrWhiteSpace(output) ? "(completed)" : output;

            var entry = new TerminalHistoryEntry
            {
                ScriptName = SelectedCommand.Name,
                Parameters = parameters,
                Timestamp = DateTime.Now,
                Output = output
            };
            History.Insert(0, entry);
            SaveHistory();

            await RefreshDropdownOptionsAsync();
        }
        catch (Exception ex)
        {
            _output.Write($"Run command failed: {ex}", ServiceLogLevel.Warning);
            OutputText = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private async Task ReRunAsync(TerminalHistoryEntry entry)
    {
        if (IsRunning) return;
        IsRunning = true;
        try
        {
            _output.Write($"Running {entry.ScriptName}...");
            var output = await ExecuteCommandAsync(entry.ScriptName, entry.Parameters);
            _output.Write($"{entry.ScriptName}: {output}");
            OutputText = output;
            entry.Output = output;
            entry.Timestamp = DateTime.Now;
            var idx = History.IndexOf(entry);
            if (idx >= 0)
            {
                History.RemoveAt(idx);
                History.Insert(0, entry);
            }
            SaveHistory();
        }
        catch (Exception ex)
        {
            _output.Write($"Re-run command failed: {ex}", ServiceLogLevel.Warning);
            OutputText = $"Error: {ex.GetType().Name}: {ex.Message}";
        }
        finally
        {
            IsRunning = false;
        }
    }

    [RelayCommand]
    private void ToggleFavorite(TerminalHistoryEntry entry)
    {
        entry.IsFavorite = !entry.IsFavorite;
        SaveHistory();
    }

    [RelayCommand]
    private void ClearHistory()
    {
        History.Clear();
        SaveHistory();
    }

    private async Task<string> ExecuteCommandAsync(string scriptName, Dictionary<string, string> parameters)
    {
        if (!CommandDefinitionsByName.TryGetValue(scriptName, out var commandDef))
            throw new NotSupportedException($"Command '{scriptName}' is not supported via WSLC API");

        return await commandDef.ExecuteAsync(parameters);
    }

    private static string GetParam(IReadOnlyDictionary<string, string> parameters, string key, string fallback = "")
        => parameters.TryGetValue(key, out var val) ? val : fallback;

    private static int GetTailLines(IReadOnlyDictionary<string, string> parameters, int fallback)
    {
        return int.TryParse(GetParam(parameters, "Tail"), out var tail) && tail > 0
            ? tail
            : fallback;
    }

    #endregion

    #region Helpers

    private static readonly Regex ParamRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    private void BuildCommandList()
    {
        Categories.Clear();
        foreach (var group in CommandDefinitions.GroupBy(def => def.Category).OrderBy(group => group.Key))
        {
            var cat = new TerminalCategory { Name = group.Key };
            foreach (var commandDef in group)
            {
                var command = new TerminalCommand
                {
                    Name = commandDef.Name,
                    DisplayName = commandDef.DisplayName,
                    Category = commandDef.Category,
                    Description = commandDef.Description,
                    HasOutput = true,
                    Parameters = commandDef.Parameters
                        .Select(parameter => new CommandParamDef
                        {
                            Name = parameter.Name,
                            DisplayName = parameter.DisplayName,
                            Type = parameter.Type,
                            Required = parameter.Required
                        })
                        .ToList()
                };

                cat.Commands.Add(command);
            }

            Categories.Add(cat);
        }
    }

    private async Task<List<string>> FetchContainerIdsAsync()
    {
        try
        {
            var output = await App.ServiceClient.GetContainersAsync();
            var containers = WslcContainerParser.ParseContainers(output ?? "");
            return containers.Select(c => c.Name).ToList();
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to load container IDs for terminal dropdown: {ex.Message}", ServiceLogLevel.Warning);
            return new();
        }
    }

    private async Task<List<string>> FetchImageNamesAsync()
    {
        try
        {
            var output = await App.ServiceClient.GetImagesAsync();
            var images = WslcContainerParser.ParseImages(output ?? "");
            return images.Select(i => string.IsNullOrWhiteSpace(i.Tag) || i.Tag == "(none)"
                ? i.Repository
                : $"{i.Repository}:{i.Tag}").ToList();
        }
        catch (Exception ex)
        {
            _output.Write($"Failed to load image names for terminal dropdown: {ex.Message}", ServiceLogLevel.Warning);
            return new();
        }
    }

    #endregion
}
