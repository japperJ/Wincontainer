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

    private static readonly Dictionary<string, string> CommandTemplates = new()
    {
        ["Get-Container"] = "GET /api/containers",
        ["Start-Container"] = "POST /api/containers/{Id}/start",
        ["Stop-Container"] = "POST /api/containers/{Id}/stop",
        ["Restart-Container"] = "POST /api/containers/{Id}/restart",
        ["Remove-Container"] = "DELETE /api/containers/{Id}",
        ["Get-ContainerLogs"] = "GET /api/containers/{Id}/logs",
        ["Get-Image"] = "GET /api/images",
        ["Pull-Image"] = "POST /api/images/pull",
        ["Remove-Image"] = "DELETE /api/images/{Id}",
        ["Get-Volumes"] = "GET /api/volumes",
        ["Create-Volume"] = "POST /api/volumes",
        ["Remove-Volume"] = "DELETE /api/volumes/{Name}",
        ["Get-Networks"] = "GET /api/networks",
        ["Create-Network"] = "POST /api/networks",
        ["Remove-Network"] = "DELETE /api/networks/{Name}",
        ["Get-Version"] = "GET /api/runtime/version",
        ["Get-Health"] = "GET /api/health",
    };

    private void BuildCommandPreview()
    {
        if (SelectedCommand is null || !CommandTemplates.TryGetValue(SelectedCommand.Name, out var template))
        {
            CommandPreview = null;
            WslcCommandPreview = null;
            return;
        }

        var result = template;
        var parameters = new Dictionary<string, string>();
        foreach (var pv in ParameterValues)
        {
            if (!string.IsNullOrWhiteSpace(pv.Value))
            {
                result = result.Replace("{" + pv.Name + "}", pv.Value);
                parameters[pv.Name] = pv.Value;
            }
            else if (pv.Required)
            {
                result = result.Replace("{" + pv.Name + "}", $"<{pv.DisplayName}>");
            }
            else
            {
                result = result.Replace("{" + pv.Name + "}", "");
            }
        }
        CommandPreview = result;
        WslcCommandPreview = BuildWslcCommandPreview(SelectedCommand.Name, parameters);
    }

    private static string? BuildWslcCommandPreview(string scriptName, Dictionary<string, string> parameters)
    {
        var id = parameters.GetValueOrDefault("Id", "<id>");
        var name = parameters.GetValueOrDefault("Name", "<name>");
        var image = parameters.GetValueOrDefault("Image", "<image>");
        var tail = parameters.GetValueOrDefault("Tail", "500");

        var args = scriptName switch
        {
            "Get-Container" => WslcCommands.ContainerPs(),
            "Start-Container" => WslcCommands.ContainerStart(id),
            "Stop-Container" => WslcCommands.ContainerStop(id),
            "Restart-Container" => WslcCommands.ContainerRestart(id),
            "Remove-Container" => WslcCommands.ContainerRemove(id),
            "Get-ContainerLogs" => int.TryParse(tail, out var tailValue)
                ? WslcCommands.ContainerLogs(id, tailValue)
                : WslcCommands.ContainerLogs(id),
            "Get-Image" => WslcCommands.ImageLs(),
            "Pull-Image" => WslcCommands.ImagePull(image),
            "Remove-Image" => WslcCommands.ImageRemove(id),
            "Get-Volumes" => WslcCommands.VolumeLs(),
            "Create-Volume" => WslcCommands.VolumeCreate(name),
            "Remove-Volume" => WslcCommands.VolumeRemove(name),
            "Get-Networks" => WslcCommands.NetworkLs(),
            "Create-Network" => WslcCommands.NetworkCreate(name),
            "Remove-Network" => WslcCommands.NetworkRemove(name),
            "Get-Version" => WslcCommands.Version(),
            "Get-Health" => WslcCommands.Version(),
            _ => null
        };

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
        return scriptName switch
        {
            "Get-Container" => await App.ServiceClient.GetContainersAsync(),
            "Start-Container" => await App.ServiceClient.StartContainerAsync(GetParam(parameters, "Id")),
            "Stop-Container" => await App.ServiceClient.StopContainerAsync(GetParam(parameters, "Id")),
            "Restart-Container" => await App.ServiceClient.RestartContainerAsync(GetParam(parameters, "Id")),
            "Remove-Container" => await App.ServiceClient.RemoveContainerAsync(GetParam(parameters, "Id")),
            "Get-ContainerLogs" => await App.ServiceClient.GetContainerLogsAsync(GetParam(parameters, "Id"), 500),
            "Get-Image" => await App.ServiceClient.GetImagesAsync(),
            "Pull-Image" => await App.ServiceClient.PullImageAsync(GetParam(parameters, "Image")),
            "Remove-Image" => await App.ServiceClient.RemoveImageAsync(GetParam(parameters, "Id")),
            "Get-Volumes" => await App.ServiceClient.GetVolumesAsync(),
            "Create-Volume" => await App.ServiceClient.CreateVolumeAsync(GetParam(parameters, "Name")),
            "Remove-Volume" => await App.ServiceClient.RemoveVolumeAsync(GetParam(parameters, "Name")),
            "Get-Networks" => await App.ServiceClient.GetNetworksAsync(),
            "Create-Network" => await App.ServiceClient.CreateNetworkAsync(GetParam(parameters, "Name")),
            "Remove-Network" => await App.ServiceClient.RemoveNetworkAsync(GetParam(parameters, "Name")),
            "Get-Version" => await App.ServiceClient.GetVersionAsync(),
            "Get-Health" => await App.ServiceClient.IsHealthyAsync().ContinueWith(t => t.Result ? "Healthy" : "Unhealthy"),
            _ => throw new NotSupportedException($"Command '{scriptName}' is not supported via WSLC API")
        };
    }

    private static string GetParam(Dictionary<string, string> parameters, string key)
        => parameters.TryGetValue(key, out var val) ? val : "";

    #endregion

    #region Helpers

    private static readonly Regex ParamRegex = new(@"\{(\w+)\}", RegexOptions.Compiled);

    private void BuildCommandList()
    {
        var categories = new Dictionary<string, TerminalCategory>
        {
            ["System"] = new() { Name = "System" },
            ["Containers"] = new() { Name = "Containers" },
            ["Images"] = new() { Name = "Images" },
            ["Volumes"] = new() { Name = "Volumes" },
            ["Networks"] = new() { Name = "Networks" }
        };

        var byCategory = new Dictionary<string, List<(string ScriptName, string DisplayName, string Desc)>>
        {
            ["System"] = new()
            {
                ("Get-Health", "Check Health", "Check if WSLC service is healthy"),
                ("Get-Version", "Get Version", "Show WSLC runtime version")
            },
            ["Containers"] = new()
            {
                ("Get-Container", "List Containers", "List all containers"),
                ("Start-Container", "Start Container", "Start a stopped container"),
                ("Stop-Container", "Stop Container", "Stop a running container"),
                ("Restart-Container", "Restart Container", "Restart a container"),
                ("Remove-Container", "Remove Container", "Delete a container"),
                ("Get-ContainerLogs", "Container Logs", "Show container logs")
            },
            ["Images"] = new()
            {
                ("Get-Image", "List Images", "List pulled images"),
                ("Pull-Image", "Pull Image", "Download an image from a registry"),
                ("Remove-Image", "Remove Image", "Delete an image from local storage")
            },
            ["Volumes"] = new()
            {
                ("Get-Volumes", "List Volumes", "List all volumes"),
                ("Create-Volume", "Create Volume", "Create a new volume"),
                ("Remove-Volume", "Remove Volume", "Delete a volume")
            },
            ["Networks"] = new()
            {
                ("Get-Networks", "List Networks", "List all networks"),
                ("Create-Network", "Create Network", "Create a new network"),
                ("Remove-Network", "Remove Network", "Delete a network")
            }
        };

        var psParamNames = new Dictionary<string, string[]>
        {
            ["Get-Container"] = new[] { "Format" },
            ["Start-Container"] = new[] { "Id" },
            ["Stop-Container"] = new[] { "Id" },
            ["Restart-Container"] = new[] { "Id" },
            ["Remove-Container"] = new[] { "Id" },
            ["Get-ContainerLogs"] = new[] { "Id", "Tail" },
            ["Pull-Image"] = new[] { "Image" },
            ["Remove-Image"] = new[] { "Id" },
            ["Create-Volume"] = new[] { "Name" },
            ["Remove-Volume"] = new[] { "Name" },
            ["Create-Network"] = new[] { "Name" },
            ["Remove-Network"] = new[] { "Name" }
        };

        var paramTypes = new Dictionary<string, CommandParamType>
        {
            ["Id"] = CommandParamType.ContainerId,
            ["Image"] = CommandParamType.ImageName,
            ["Format"] = CommandParamType.Format,
            ["Name"] = CommandParamType.Text,
            ["Tail"] = CommandParamType.Text
        };

        var displayNames = new Dictionary<string, string>
        {
            ["Format"] = "Output Format",
            ["Id"] = "Container",
            ["Image"] = "Image",
            ["Name"] = "Name",
            ["Tail"] = "Tail Lines"
        };

        foreach (var (catName, entries) in byCategory)
        {
            var category = categories[catName];

            foreach (var (scriptName, displayName, desc) in entries)
            {
                var cmd = new TerminalCommand
                {
                    Name = scriptName,
                    DisplayName = displayName,
                    Category = catName,
                    Description = desc,
                    HasOutput = true
                };

                if (psParamNames.TryGetValue(scriptName, out var paramNames))
                {
                    foreach (var pn in paramNames)
                    {
                        cmd.Parameters.Add(new CommandParamDef
                        {
                            Name = pn,
                            DisplayName = displayNames.GetValueOrDefault(pn, pn),
                            Type = paramTypes.GetValueOrDefault(pn, CommandParamType.Text),
                            Required = pn != "Format" && pn != "Tail"
                        });
                    }
                }

                category.Commands.Add(cmd);
            }
        }

        Categories.Clear();
        foreach (var (_, cat) in categories.OrderBy(kv => kv.Key))
            Categories.Add(cat);
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
