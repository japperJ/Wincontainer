using System.Collections.ObjectModel;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.Json;
using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Runtime.Models;
using WinContainers_App.Models;
using WinContainers_App.Services;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public partial class ContainerDetailViewModel : ViewModelBase
{

    #region Constructor and Fields

    private readonly IOutputService _output;
    private readonly IDialogService _dialog;
    private readonly INavigationService _navigation;
    private readonly IWslcServiceClient _serviceClient;
    private readonly Func<IEnumerable<IPAddress>> _addressProvider;
    private int _accessOperationVersion;
    private ContainerRunConfig? _accessConfig;
    #endregion

    #region Observable Properties

    private string? _containerName;
    public string? ContainerName
    {
        get => _containerName;
        set => SetProperty(ref _containerName, value);
    }

    private string? _containerStatus;
    public string? ContainerStatus
    {
        get => _containerStatus;
        set => SetProperty(ref _containerStatus, value);
    }

    private string? _containerInfo;
    public string? ContainerInfo
    {
        get => _containerInfo;
        set => SetProperty(ref _containerInfo, value);
    }

    private string? _containerId;
    public string? ContainerId
    {
        get => _containerId;
        set => SetProperty(ref _containerId, value);
    }

    private string? _containerImage;
    public string? ContainerImage
    {
        get => _containerImage;
        set => SetProperty(ref _containerImage, value);
    }

    private string? _containerPorts;
    public string? ContainerPorts
    {
        get => _containerPorts;
        set => SetProperty(ref _containerPorts, value);
    }

    private string? _containerCreatedAt;
    public string? ContainerCreatedAt
    {
        get => _containerCreatedAt;
        set => SetProperty(ref _containerCreatedAt, value);
    }

    private bool _isStartEnabled;
    public bool IsStartEnabled
    {
        get => _isStartEnabled;
        set => SetProperty(ref _isStartEnabled, value);
    }

    private bool _isStopEnabled;
    public bool IsStopEnabled
    {
        get => _isStopEnabled;
        set => SetProperty(ref _isStopEnabled, value);
    }

    private bool _isRestartEnabled;
    public bool IsRestartEnabled
    {
        get => _isRestartEnabled;
        set => SetProperty(ref _isRestartEnabled, value);
    }

    private bool _isDeleteEnabled;
    public bool IsDeleteEnabled
    {
        get => _isDeleteEnabled;
        set => SetProperty(ref _isDeleteEnabled, value);
    }

    private string _actionError = "";
    public string ActionError
    {
        get => _actionError;
        set => SetProperty(ref _actionError, value);
    }

    private bool _hasActionError;
    public bool HasActionError
    {
        get => _hasActionError;
        set => SetProperty(ref _hasActionError, value);
    }

    public void DismissActionError()
    {
        ActionError = "";
        HasActionError = false;
    }

    private bool _allowLocalNetworkAccess;
    public bool AllowLocalNetworkAccess
    {
        get => _allowLocalNetworkAccess;
        set => SetProperty(ref _allowLocalNetworkAccess, value);
    }

    private bool _isAccessChangeRunning;
    public bool IsAccessChangeRunning
    {
        get => _isAccessChangeRunning;
        set
        {
            if (SetProperty(ref _isAccessChangeRunning, value))
                OnPropertyChanged(nameof(CanChangeAccess));
        }
    }

    private bool _canChangeAccess;
    public bool CanChangeAccess
    {
        get => _canChangeAccess && !IsAccessChangeRunning;
        private set => SetProperty(ref _canChangeAccess, value);
    }

    private string _accessStatusText = "";
    public string AccessStatusText
    {
        get => _accessStatusText;
        private set => SetProperty(ref _accessStatusText, value);
    }

    public ObservableCollection<string> AccessEndpoints { get; } = [];

    // Logs
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

    // Inspect
    private string? _inspectStatusText;
    public string? InspectStatusText
    {
        get => _inspectStatusText;
        set => SetProperty(ref _inspectStatusText, value);
    }

    private string? _inspectJson;
    public string? InspectJson
    {
        get => _inspectJson;
        set => SetProperty(ref _inspectJson, value);
    }

    // Shell
    private string? _shellOutput;
    public string? ShellOutput
    {
        get => _shellOutput;
        set => SetProperty(ref _shellOutput, value);
    }

    private string? _shellCommand;
    public string? ShellCommand
    {
        get => _shellCommand;
        set => SetProperty(ref _shellCommand, value);
    }

    private int _shellSelectorIndex;
    public int ShellSelectorIndex
    {
        get => _shellSelectorIndex;
        set => SetProperty(ref _shellSelectorIndex, value);
    }

    private bool _isShellRunning;
    public bool IsShellRunning
    {
        get => _isShellRunning;
        set => SetProperty(ref _isShellRunning, value);
    }

    // Files
    private ObservableCollection<FileEntryData>? _fileEntries;
    public ObservableCollection<FileEntryData>? FileEntries
    {
        get => _fileEntries;
        set => SetProperty(ref _fileEntries, value);
    }

    private string? _currentFilePath;
    public string? CurrentFilePath
    {
        get => _currentFilePath;
        set
        {
            if (SetProperty(ref _currentFilePath, value))
            {
                OnPropertyChanged(nameof(BreadcrumbSegments));
            }
        }
    }

    public IEnumerable<string> BreadcrumbSegments
    {
        get
        {
            var path = CurrentFilePath?.Trim('/') ?? "";
            if (string.IsNullOrEmpty(path))
            {
                return ["/"];
            }
            var segments = path.Split('/');
            return segments.Prepend("/");
        }
    }

    private bool _isFilesLoading;
    public bool IsFilesLoading
    {
        get => _isFilesLoading;
        set => SetProperty(ref _isFilesLoading, value);
    }

    private string? _filesLoadingText;
    public string? FilesLoadingText
    {
        get => _filesLoadingText;
        set => SetProperty(ref _filesLoadingText, value);
    }

    private bool _showFileList;
    public bool ShowFileList
    {
        get => _showFileList;
        set => SetProperty(ref _showFileList, value);
    }

    private bool _showFileContent;
    public bool ShowFileContent
    {
        get => _showFileContent;
        set => SetProperty(ref _showFileContent, value);
    }

    private string? _viewingFilePath;
    public string? ViewingFilePath
    {
        get => _viewingFilePath;
        set => SetProperty(ref _viewingFilePath, value);
    }

    private string? _fileContent;
    public string? FileContent
    {
        get => _fileContent;
        set => SetProperty(ref _fileContent, value);
    }

    private string? _fileEditContent;
    public string? FileEditContent
    {
        get => _fileEditContent;
        set => SetProperty(ref _fileEditContent, value);
    }

    private bool _isEditing;
    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    private bool _isFileTextViewable;
    public bool IsFileTextViewable
    {
        get => _isFileTextViewable;
        set => SetProperty(ref _isFileTextViewable, value);
    }

    private bool _isFileBinary;
    public bool IsFileBinary
    {
        get => _isFileBinary;
        set => SetProperty(ref _isFileBinary, value);
    }

    private bool _isImportEnabled;
    public bool IsImportEnabled
    {
        get => _isImportEnabled;
        set => SetProperty(ref _isImportEnabled, value);
    }

    private bool _isRefreshFilesEnabled;
    public bool IsRefreshFilesEnabled
    {
        get => _isRefreshFilesEnabled;
        set => SetProperty(ref _isRefreshFilesEnabled, value);
    }

    private string? _breadcrumbHtml;
    public string? BreadcrumbHtml
    {
        get => _breadcrumbHtml;
        set => SetProperty(ref _breadcrumbHtml, value);
    }

    #endregion

    #region Container State

    public ContainerDetailViewModel(
        IOutputService output,
        IDialogService dialog,
        INavigationService navigation,
        IWslcServiceClient serviceClient,
        Func<IEnumerable<IPAddress>>? addressProvider = null)
    {
        _output = output;
        _dialog = dialog;
        _navigation = navigation;
        _serviceClient = serviceClient;
        _addressProvider = addressProvider ?? GetUsableIpv4Addresses;
    }

    public void LoadContainer(ContainerViewModel data)
    {
        _accessConfig = null;
        _accessOperationVersion++;
        IsAccessChangeRunning = false;
        ContainerId = data.Id;
        ContainerName = data.Name;
        ContainerStatus = data.Status;
        ContainerImage = data.Image;
        ContainerPorts = data.Ports;
        ContainerCreatedAt = data.CreatedAt;

        ShellOutput = $"--- Shell ready (use /bin/bash)\n--- Type a command and press Run or Enter\n";
        DismissActionError();
        UpdateHeaderState();
        UpdateContainerInfo();
        InitializeAccessState();
    }

    public void InitializeAccessState(ContainerRunConfig? config = null)
    {
        config ??= string.IsNullOrWhiteSpace(ContainerName)
            ? _accessConfig
            : ContainerConfigStore.LoadConfig(ContainerName) ?? _accessConfig;
        _accessConfig = config;

        var hasPorts = config?.Ports.Count > 0;
        var hasConfig = config is not null;
        AllowLocalNetworkAccess = config?.AllowLocalNetworkAccess ?? false;
        AccessEndpoints.Clear();
        if (hasPorts)
        {
            foreach (var endpoint in BuildAccessEndpoints(config!.Ports, _addressProvider()))
                AccessEndpoints.Add(endpoint);
        }

        CanChangeAccess = hasConfig && hasPorts;
        AccessStatusText = !hasConfig
            ? "Saved container configuration unavailable."
            : !hasPorts
                ? "No published ports"
                : AllowLocalNetworkAccess
                    ? "Local-network access enabled"
                    : "Local-only access";
    }

    public async Task<bool> SetAccessAsync(bool allowLocalNetworkAccess, Func<Task<bool>>? confirmEnable = null)
    {
        if (!CanChangeAccess || IsAccessChangeRunning || allowLocalNetworkAccess == AllowLocalNetworkAccess)
            return false;

        if (allowLocalNetworkAccess && (confirmEnable is null || !await confirmEnable()))
            return false;

        var version = ++_accessOperationVersion;
        var previous = AllowLocalNetworkAccess;
        DismissActionError();
        AllowLocalNetworkAccess = allowLocalNetworkAccess;
        IsAccessChangeRunning = true;
        AccessStatusText = "Recreating container...";

        try
        {
            var result = await _serviceClient.SetContainerAccessAsync(
                ContainerId ?? "",
                allowLocalNetworkAccess,
                ContainerName);
            if (version != _accessOperationVersion)
                return false;

            if (!result.Success)
            {
                AllowLocalNetworkAccess = previous;
                AccessStatusText = result.Message;
                ActionError = result.Message;
                HasActionError = true;
                return false;
            }

            AccessStatusText = result.Message;
            if (_accessConfig is not null)
            {
                _accessConfig = _accessConfig with
                {
                    Ports = result.Ports.ToList(),
                    AllowLocalNetworkAccess = result.AllowLocalNetworkAccess
                };
            }
            await RefreshContainerStateAsync();
            InitializeAccessState();
            return true;
        }
        catch (Exception ex)
        {
            if (version == _accessOperationVersion)
            {
                AllowLocalNetworkAccess = previous;
                AccessStatusText = $"Access change failed: {ex.Message}";
                ActionError = AccessStatusText;
                HasActionError = true;
            }
            return false;
        }
        finally
        {
            if (version == _accessOperationVersion)
            {
                IsAccessChangeRunning = false;
            }
        }
    }

    public void CancelPendingAccessChange()
    {
        ++_accessOperationVersion;
        IsAccessChangeRunning = false;
    }

    public static IReadOnlyList<IPAddress> GetUsableIpv4Addresses()
    {
        var addresses = new List<IPAddress>();
        try
        {
            foreach (var networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var address in networkInterface.GetIPProperties().UnicastAddresses.Select(a => a.Address))
                {
                    var bytes = address.GetAddressBytes();
                    if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                        || IPAddress.IsLoopback(address)
                        || bytes is [169, 254, ..])
                        continue;

                    if (!addresses.Contains(address))
                        addresses.Add(address);
                }
            }
        }
        catch (NetworkInformationException)
        {
        }

        return addresses;
    }

    public static IReadOnlyList<string> BuildAccessEndpoints(
        IEnumerable<string> bindings,
        IEnumerable<IPAddress> addresses)
    {
        var endpoints = new List<string>();
        foreach (var binding in bindings)
        {
            if (!PortBindingConverter.TryParse(binding, out var hostPort, out _, out var protocol, out _))
                continue;

            foreach (var address in addresses)
            {
                var bytes = address.GetAddressBytes();
                if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork
                    || IPAddress.IsLoopback(address)
                    || bytes is [169, 254, ..])
                    continue;

                var prefix = string.Equals(protocol, "http", StringComparison.OrdinalIgnoreCase)
                    ? "http://"
                    : "";
                endpoints.Add($"{prefix}{address}:{hostPort}");
            }
        }

        return endpoints.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private void UpdateContainerInfo()
    {
        var created = string.IsNullOrWhiteSpace(ContainerCreatedAt) ? "" : $"Created: {ContainerCreatedAt}";
        var ports = string.IsNullOrWhiteSpace(ContainerPorts) || ContainerPorts == "No ports" ? "" : $"Ports: {ContainerPorts}";
        var parts = new[] { created, $"Image: {ContainerImage}", ports }.Where(p => !string.IsNullOrWhiteSpace(p));
        ContainerInfo = string.Join("  |  ", parts);
    }

    private void UpdateHeaderState()
    {
        IsStartEnabled = WslcContainerParser.IsExitedStatus(ContainerStatus) || ContainerStatus == "Created";
        IsStopEnabled = WslcContainerParser.IsRunningStatus(ContainerStatus);
        IsRestartEnabled = IsStartEnabled || IsStopEnabled;
        IsDeleteEnabled = true;
    }

    public async Task RefreshContainerStateAsync()
    {
        try
        {
            var output = await _serviceClient.GetContainersAsync();
            var entries = WslcContainerParser.ParseContainers(output ?? "");
            var match = entries.Find(c => c.Id == ContainerId || c.Name == ContainerName);

            if (match is not null)
            {
                ContainerStatus = match.Status;
                ContainerName = match.Name;
                ContainerImage = match.Image;
                ContainerPorts = match.Ports;
                ContainerCreatedAt = match.CreatedAt;
                UpdateHeaderState();
                UpdateContainerInfo();
            }
            else
            {
                ContainerName = $"{ContainerName} (removed)";
                ContainerStatus = "Removed";
                IsStartEnabled = false;
                IsStopEnabled = false;
                IsRestartEnabled = false;
                IsDeleteEnabled = false;
            }
        }
        catch (Exception ex) { _output.Write($"RefreshContainerStateAsync failed: {ex.Message}", ServiceLogLevel.Warning); }
    }

    public async Task RunActionAsync(string action)
    {
        DismissActionError();
        _output.Write($"Running {action} for {ContainerId}...");

        try
        {
            var output = action switch
            {
                "Start" => await _serviceClient.StartContainerAsync(ContainerId),
                "Stop" => await _serviceClient.StopContainerAsync(ContainerId),
                "Restart" => await _serviceClient.RestartContainerAsync(ContainerId),
                "Delete" => await _serviceClient.RemoveContainerAsync(ContainerId),
                _ => null
            };

            if (output is null) return;
            _output.Write($"{action} {ContainerId}: {output}");

            if (!string.IsNullOrWhiteSpace(output) && IsErrorOutput(output))
            {
                ActionError = output.Trim();
                HasActionError = true;
            }

            await RefreshContainerStateAsync();

            if (action == "Start" && WslcContainerParser.IsExitedStatus(ContainerStatus))
                await ShowContainerExitErrorAsync();
        }
        catch (Exception ex)
        {
            ActionError = ex.Message;
            HasActionError = true;
        }
    }

    private async Task ShowContainerExitErrorAsync()
    {
        try
        {
            var logs = await _serviceClient.GetContainerLogsAsync(ContainerId, 500);
            var msg = $"Container '{ContainerName}' exited after start.";

            if (!string.IsNullOrWhiteSpace(logs))
                msg += $"\n\nContainer logs:\n{logs.Trim()}";

            ActionError = msg;
            HasActionError = true;
        }
        catch (Exception ex)
        {
            ActionError = $"Failed to get exit details: {ex.Message}";
            HasActionError = true;
        }
    }

    private static bool IsErrorOutput(string output)
    {
        var trimmed = output.Trim();
        if (trimmed.Length == 0) return false;
        if (trimmed.StartsWith("Error:", StringComparison.OrdinalIgnoreCase)) return true;
        if (trimmed.StartsWith("Error response", StringComparison.OrdinalIgnoreCase)) return true;
        if (trimmed.Contains("cannot start", StringComparison.OrdinalIgnoreCase)) return true;
        if (trimmed.Contains("failed to", StringComparison.OrdinalIgnoreCase)) return true;
        if (trimmed.Contains("no such container", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    #endregion

    #region Logs and Inspect

    public async Task LoadLogsAsync()
    {
        try
        {
            var output = await _serviceClient.GetContainerLogsAsync(ContainerId, 500);
            LogsContent = string.IsNullOrWhiteSpace(output) ? "(no logs)" : output;
            LogsInfoText = $"Auto-refreshing every 3s — {(output?.Length ?? 0)} chars";
        }
        catch (Exception ex)
        {
            LogsContent = $"Failed to load logs: {ex.Message}";
        }
    }

    public async Task LoadInspectAsync()
    {
        try
        {
            InspectStatusText = "Loading...";
            var output = await _serviceClient.GetContainersAsync();
            var entries = WslcContainerParser.ParseContainers(output ?? "");
            var match = entries.Find(c => c.Id == ContainerId || c.Name == ContainerName);
            InspectJson = match is not null
                ? JsonSerializer.Serialize(match, new JsonSerializerOptions { WriteIndented = true })
                : "{}";
            InspectStatusText = "Ready";
        }
        catch (Exception ex)
        {
            InspectStatusText = $"Failed: {ex.Message}";
        }
    }

    #endregion

    #region Shell

    private static readonly string[] ShellOptions = ["/bin/bash", "/bin/sh", "pwsh", "cmd.exe"];

    public async Task RunShellCommandAsync()
    {
        var command = ShellCommand?.Trim();
        if (string.IsNullOrWhiteSpace(command))
            return;

        var shell = ShellSelectorIndex >= 0 && ShellSelectorIndex < ShellOptions.Length
            ? ShellOptions[ShellSelectorIndex] : "/bin/sh";

        AppendShellOutput($"> [{shell}] {command}\n");
        ShellCommand = "";
        IsShellRunning = true;

        _output.Write($"Shell exec: id={ContainerId}, shell={shell}, cmd={command}");

        try
        {
            var output = await _serviceClient.ExecContainerAsync(ContainerId, command, useShell: true, shell: shell);
            _output.Write($"Shell result: len={output?.Length ?? 0}");
            AppendShellOutput(string.IsNullOrWhiteSpace(output) ? "(no output)\n" : output + "\n");
        }
        catch (Exception ex)
        {
            _output.Write($"Shell error: {ex.GetType().Name}: {ex.Message}", ServiceLogLevel.Warning);
            AppendShellOutput($"Error: {ex.GetType().Name}: {ex.Message}\n");
        }
        finally
        {
            IsShellRunning = false;
        }
    }

    private void AppendShellOutput(string text)
    {
        ShellOutput += text;
    }

    #endregion

    #region File Management

    public async Task LoadFileListAsync(string path)
    {
        CurrentFilePath = path;
        IsFilesLoading = true;
        ShowFileContent = false;
        ShowFileList = true;
        IsImportEnabled = false;
        IsRefreshFilesEnabled = false;
        FileEntries = null;

        try
        {
            var quotedPath = WslcCommands.ShellQuote(path);
            var listingCommand =
                $"for entry in {quotedPath}/* {quotedPath}/.[!.]* {quotedPath}/..?*; do " +
                "[ -e \"$entry\" ] || [ -L \"$entry\" ] || continue; " +
                "if [ -d \"$entry\" ]; then printf 'd\\t%s\\0' \"${entry##*/}\"; " +
                "else printf 'f\\t%s\\0' \"${entry##*/}\"; fi; done";
            var output = await _serviceClient.ExecContainerAsync(ContainerId, listingCommand, true, "/bin/sh");
            var entries = new ObservableCollection<FileEntryData>();

            // Add parent directory entry ("..") if not at root
            if (!string.IsNullOrEmpty(path) && path != "/")
            {
                entries.Add(new FileEntryData
                {
                    Name = "..",
                    Type = "dir",
                    Icon = "\uE72A",  // up arrow / parent folder icon
                    Permissions = "d../.."
                });
            }

            if (!string.IsNullOrWhiteSpace(output) && !output.StartsWith("error"))
            {
                foreach (var entry in WslcFileParser.ParseFileEntries(output))
                {
                    var isDir = entry.Type == "dir";
                    entries.Add(new FileEntryData
                    {
                        Name = entry.Name,
                        Type = isDir ? "dir" : "file",
                        Icon = isDir ? "\uE838" : "\uE996",
                        Permissions = isDir ? "d" : "-"
                    });
                }
            }

            FileEntries = entries;
            FilesLoadingText = $"{entries.Count} item(s)";
            IsFilesLoading = false;
            IsImportEnabled = true;
            IsRefreshFilesEnabled = true;
        }
        catch (Exception ex)
        {
            FilesLoadingText = $"Failed: {ex.Message}";
            IsFilesLoading = false;
            IsImportEnabled = true;
            IsRefreshFilesEnabled = true;
        }
    }

    public async Task OpenFileViewerAsync(FileEntryData entry)
    {
        var filePath = CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
        ViewingFilePath = filePath;

        var isText = IsTextViewableFile(entry.Name);
        IsFileTextViewable = isText;
        IsFileBinary = !isText;
        IsEditing = false;

        ShowFileList = false;
        ShowFileContent = true;

        if (!isText) return;

        try
        {
            var output = await _serviceClient.ExecContainerAsync(ContainerId, $"cat {WslcCommands.ShellQuote(filePath)}");
            FileContent = output ?? "(empty or error)";
            FileEditContent = FileContent;
        }
        catch (Exception ex)
        {
            FileContent = $"Failed to read file: {ex.Message}";
        }
    }

    public async Task SaveFileAsync()
    {
        if (string.IsNullOrEmpty(ViewingFilePath)) return;
        try
        {
            await WriteFileViaStdin(ViewingFilePath, FileEditContent);
            FileContent = FileEditContent;
            IsEditing = false;
        }
        catch (Exception ex)
        {
            await _dialog.ShowMessageAsync("Error", $"Failed to save file: {ex.Message}");
        }
    }

    public async Task<string> ReadFileAsync(string filePath)
    {
        try
        {
            var output = await _serviceClient.ExecContainerAsync(ContainerId, $"cat {WslcCommands.ShellQuote(filePath)}", false);
            if (output.StartsWith("error"))
                throw new InvalidOperationException(output);
            return output;
        }
        catch (Exception ex)
        {
            _output.Write($"Read file error: {ex.Message}", ServiceLogLevel.Warning);
            throw;
        }
    }

    private async Task WriteFileViaStdin(string filePath, string content)
    {
        var encodedContent = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(content ?? string.Empty));
        var script = $"printf '%s' '{encodedContent}' | base64 -d > {WslcCommands.ShellQuote(filePath)}";
        var output = await _serviceClient.ExecContainerAsync(ContainerId, script, true, "/bin/sh");
        if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("error", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(output);
    }

    public async Task ChangePermissionsAsync(FileEntryData entry, string mode)
    {
        var filePath = CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
        try
        {
            var output = await _serviceClient.ExecContainerAsync(ContainerId, $"chmod {mode} {WslcCommands.ShellQuote(filePath)}");
            if (!string.IsNullOrWhiteSpace(output) && output.StartsWith("error", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(output);

            await LoadFileListAsync(CurrentFilePath);
            _output.Write($"Changed permissions on {filePath} to {mode}");
        }
        catch (Exception ex)
        {
            _output.Write($"Permission change failed: {ex.Message}", ServiceLogLevel.Error);
            await _dialog.ShowMessageAsync("Error", $"Failed to change permissions: {ex.Message}");
        }
    }

    public static string ConvertPermissionsToNumeric(string? symbolic)
    {
        if (string.IsNullOrWhiteSpace(symbolic)) return "?";

        var trimmed = symbolic.Trim();
        if (trimmed.StartsWith("d") || trimmed.StartsWith("-") || trimmed.StartsWith("l") || trimmed.StartsWith("c") || trimmed.StartsWith("b") || trimmed.StartsWith("s") || trimmed.StartsWith("p"))
        {
            var part = trimmed.Length >= 10 ? trimmed[..10] : trimmed;
            var owner = part[1..4];
            var group = part[4..7];
            var other = part[7..10];

            int ToValue(string segment) => segment switch
            {
                "rwx" => 7,
                "rw-" => 6,
                "r-x" => 5,
                "r--" => 4,
                "-wx" => 3,
                "-w-" => 2,
                "--x" => 1,
                "---" => 0,
                _ => 0
            };

            return $"{ToValue(owner)}{ToValue(group)}{ToValue(other)}";
        }

        return trimmed;
    }

    public async Task DeleteFileAsync(FileEntryData entry)
    {
        var filePath = CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
        _output.Write($"Delete not available via WSLC API (path: {filePath})", ServiceLogLevel.Warning);
        await _dialog.ShowMessageAsync("Error", "File delete not available via WSLC API");
    }

    public async Task<string?> DoImportFileAsync(string localPath, string fileName)
    {
        try
        {
            var content = await System.IO.File.ReadAllTextAsync(localPath);
            var destPath = CurrentFilePath.TrimEnd('/') + "/" + fileName;
            await WriteFileViaStdin(destPath, content);
            await LoadFileListAsync(CurrentFilePath);
            return null;
        }
        catch (Exception ex)
        {
            _output.Write($"Import error: {ex.Message}", ServiceLogLevel.Error);
            return $"Import failed: {ex.Message}";
        }
    }

    public void StartEditing()
    {
        FileEditContent = FileContent;
        IsEditing = true;
    }

    public void CancelEditing()
    {
        FileEditContent = FileContent;
        IsEditing = false;
    }

    #endregion

    #region Helpers

    private static readonly HashSet<string> TextViewableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt", ".conf", ".json", ".xml", ".yaml", ".yml", ".md", ".sh", ".env",
        ".log", ".cfg", ".ini", ".py", ".js", ".ts", ".html", ".css", ".go", ".cs",
        ".java", ".rs", ".rb", ".php", ".sql", ".lua", ".toml", ".lock", ".gradle",
        ".properties", ".makefile", ".dockerfile", ".bashrc", ".profile", ".gitignore",
        ".gitattributes", ".editorconfig", ".dockerignore", ".hcl", ".tf", ".proto",
        ".vim", ".ps1", ".psm1", ".psd1", ".bat", ".cmd"
    };

    private static readonly HashSet<string> TextViewableFilenames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Dockerfile", "Makefile", "README", "LICENSE", "CHANGELOG",
        ".dockerignore", ".editorconfig", ".gitignore", ".gitattributes",
        ".env", ".bashrc", ".profile"
    };

    public static bool IsTextViewableFile(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return false;
        var ext = Path.GetExtension(name);
        if (!string.IsNullOrEmpty(ext) && TextViewableExtensions.Contains(ext)) return true;
        return TextViewableFilenames.Contains(name);
    }

    public void NavigateBack()
    {
        MainWindow.ReturnToPivotIndex = 1; // Containers tab
        _navigation.GoBack();
    }

    public async Task ShowErrorAsync(string message)
    {
        await _dialog.ShowMessageAsync("Error", message);
    }

    #endregion
}
