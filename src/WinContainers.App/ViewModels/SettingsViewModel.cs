using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers_App.Services;
using LogLevel = WinContainers_App.Services.LogLevel;
using Velopack;

namespace WinContainers_App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly AppSettingsService _settingsService;
    private readonly WslcUpdateService _wslcUpdateService;
    private readonly IWslcServiceClient _serviceClient;

    private bool _isCheckingWslcUpdate;
    public bool IsCheckingWslcUpdate
    {
        get => _isCheckingWslcUpdate;
        private set => SetProperty(ref _isCheckingWslcUpdate, value);
    }

    private bool _wslcUpdateAvailable;
    public bool WslcUpdateAvailable
    {
        get => _wslcUpdateAvailable;
        private set => SetProperty(ref _wslcUpdateAvailable, value);
    }

    private string _wslcUpdateStatus = "Use Check for updates to look for a newer WSLC release.";
    public string WslcUpdateStatus
    {
        get => _wslcUpdateStatus;
        private set => SetProperty(ref _wslcUpdateStatus, value);
    }

    private WslcUpdateInfo? _availableWslcUpdate;

    private string? _portText;
    public string? PortText
    {
        get => _portText;
        set => SetProperty(ref _portText, value);
    }

    private string? _tokenText;
    public string? TokenText
    {
        get => _tokenText;
        set => SetProperty(ref _tokenText, value);
    }

    private string? _statusText;
    public string? StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    private string? _serviceStatusText;
    public string? ServiceStatusText
    {
        get => _serviceStatusText;
        set => SetProperty(ref _serviceStatusText, value);
    }

    private bool _serviceHealthy;
    public bool ServiceHealthy
    {
        get => _serviceHealthy;
        set => SetProperty(ref _serviceHealthy, value);
    }

    private string? _versionText;
    public string? VersionText
    {
        get => _versionText;
        set => SetProperty(ref _versionText, value);
    }

    private bool _apiLoggingEnabled;
    public bool ApiLoggingEnabled
    {
        get => _apiLoggingEnabled;
        set
        {
            if (SetProperty(ref _apiLoggingEnabled, value))
            {
                _output.ApiLoggingEnabled = value;
            }
        }
    }

    private bool _remoteApiLoggingEnabled;
    public bool RemoteApiLoggingEnabled
    {
        get => _remoteApiLoggingEnabled;
        set
        {
            if (SetProperty(ref _remoteApiLoggingEnabled, value))
            {
                _output.RemoteApiLoggingEnabled = value;
            }
        }
    }

    private bool _mcpEnabled;
    public bool McpEnabled
    {
        get => _mcpEnabled;
        set
        {
            if (SetProperty(ref _mcpEnabled, value))
            {
                _output.McpEnabled = value;
                if (!_isLoading)
                {
                    _output.Write(value ? "MCP server enabled" : "MCP server disabled", LogLevel.Info);
                }
            }
        }
    }

    private bool _mcpLoggingEnabled;
    public bool McpLoggingEnabled
    {
        get => _mcpLoggingEnabled;
        set
        {
            if (SetProperty(ref _mcpLoggingEnabled, value))
            {
                _output.McpLoggingEnabled = value;
            }
        }
    }

    private bool _allowRemoteApiAccess;
    public bool AllowRemoteApiAccess
    {
        get => _allowRemoteApiAccess;
        set
        {
            if (SetProperty(ref _allowRemoteApiAccess, value))
            {
                _output.AllowRemoteApiAccess = value;
                if (!_isLoading)
                {
                    _output.Write(value ? "Remote API access enabled" : "Remote API access blocked", LogLevel.Info);
                }
            }
        }
    }

    private string _updateChannel = UpdateService.StableChannel;
    public string UpdateChannel
    {
        get => _updateChannel;
        set
        {
            if (SetProperty(ref _updateChannel, value))
            {
                var settings = _settingsService.Load();
                settings.UpdateChannel = value;
                _settingsService.Save(settings);
            }
        }
    }

    public string AppVersion => UpdateService.CurrentVersion;
    public bool IsPortable => UpdateService.IsPortable;

    private bool _isCheckingAppUpdate;
    public bool IsCheckingAppUpdate
    {
        get => _isCheckingAppUpdate;
        private set => SetProperty(ref _isCheckingAppUpdate, value);
    }

    private bool _appUpdateAvailable;
    public bool AppUpdateAvailable
    {
        get => _appUpdateAvailable;
        private set => SetProperty(ref _appUpdateAvailable, value);
    }

    private string _appUpdateStatus = "Use Check for updates to look for a newer WinContainers release.";
    public string AppUpdateStatus
    {
        get => _appUpdateStatus;
        private set => SetProperty(ref _appUpdateStatus, value);
    }

    private UpdateInfo? _availableAppUpdate;

    private string _aiProviderKind = "OpenAiCompatible";
    public string AiProviderKind
    {
        get => _aiProviderKind;
        set => SetProperty(ref _aiProviderKind, value);
    }

    private string _aiEndpoint = "https://api.openai.com/v1";
    public string AiEndpoint
    {
        get => _aiEndpoint;
        set => SetProperty(ref _aiEndpoint, value);
    }

    private string _aiModel = "gpt-4o-mini";
    public string AiModel
    {
        get => _aiModel;
        set => SetProperty(ref _aiModel, value);
    }

    private string? _aiApiKey;
    public string? AiApiKey
    {
        get => _aiApiKey;
        set => SetProperty(ref _aiApiKey, value);
    }

    private bool _aiConfirmDestructiveActions = true;
    public bool AiConfirmDestructiveActions
    {
        get => _aiConfirmDestructiveActions;
        set => SetProperty(ref _aiConfirmDestructiveActions, value);
    }

    private bool _mcpDestructiveConfirmationEnabled = true;
    public bool McpDestructiveConfirmationEnabled
    {
        get => _mcpDestructiveConfirmationEnabled;
        set => SetProperty(ref _mcpDestructiveConfirmationEnabled, value);
    }

    private string _aiStatusText = "AI assistant settings are stored locally and never leave this machine.";
    public string AiStatusText
    {
        get => _aiStatusText;
        set => SetProperty(ref _aiStatusText, value);
    }

    public SettingsViewModel(IOutputService output, AppSettingsService settingsService, WslcUpdateService wslcUpdateService, IWslcServiceClient serviceClient)
    {
        _output = output;
        _settingsService = settingsService;
        _wslcUpdateService = wslcUpdateService;
        _serviceClient = serviceClient;
    }

    private bool _isLoading;
    public async Task LoadAsync()
    {
        _isLoading = true;
        try
        {
            ApiLoggingEnabled = _output.ApiLoggingEnabled;
            RemoteApiLoggingEnabled = _output.RemoteApiLoggingEnabled;
            McpEnabled = _output.McpEnabled;
            McpLoggingEnabled = _output.McpLoggingEnabled;
            AllowRemoteApiAccess = _output.AllowRemoteApiAccess;
            var settings = _settingsService.Load();
            UpdateChannel = string.Equals(settings.UpdateChannel, UpdateService.BetaChannel, StringComparison.OrdinalIgnoreCase)
                ? UpdateService.BetaChannel
                : UpdateService.StableChannel;
            PortText = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
            TokenText = ServiceEndpointResolver.ResolveToken();
            StatusText = $"Current endpoint: {ServiceEndpointResolver.Resolve()}";

            AiProviderKind = string.Equals(settings.AiProviderKind, "Ollama", StringComparison.OrdinalIgnoreCase)
                ? "Ollama"
                : "OpenAiCompatible";
            AiEndpoint = string.IsNullOrWhiteSpace(settings.AiEndpoint) ? "https://api.openai.com/v1" : settings.AiEndpoint;
            AiModel = string.IsNullOrWhiteSpace(settings.AiModel) ? "gpt-4o-mini" : settings.AiModel;
            AiApiKey = settings.AiApiKey;
            AiConfirmDestructiveActions = settings.AiConfirmDestructiveActions;
            McpDestructiveConfirmationEnabled = settings.McpDestructiveConfirmationEnabled;

            try
            {
                ServiceHealthy = await _serviceClient.IsHealthyAsync();
                var version = await _serviceClient.GetVersionAsync();
                VersionText = ServiceHealthy ? $"WSLC version: {WslcVersionFormatter.Format(version)}" : "Service unavailable";
                ServiceStatusText = ServiceHealthy ? "WSLC service is running" : "WSLC service is not responding";
            }
            catch
            {
                ServiceHealthy = false;
                VersionText = "WSLC: unavailable";
                ServiceStatusText = "Failed to connect to WSLC service";
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    public void ApplyPort()
    {
        var port = string.IsNullOrWhiteSpace(PortText) ? "5123" : PortText.Trim();
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT", port);
        StatusText = $"Updated endpoint: {ServiceEndpointResolver.Resolve()}";
    }

    public void ApplyToken()
    {
        var token = TokenText ?? string.Empty;
        ServiceEndpointResolver.SetToken(token);

        var settings = _settingsService.Load();
        settings.ApiToken = token;
        _settingsService.Save(settings);

        StatusText = $"Updated endpoint: {ServiceEndpointResolver.Resolve()}";
    }

    public void SaveLoggingSettings()
    {
        var settings = _settingsService.Load();
        settings.ApiLoggingEnabled = _output.ApiLoggingEnabled;
        settings.RemoteApiLoggingEnabled = _output.RemoteApiLoggingEnabled;
        settings.McpEnabled = _output.McpEnabled;
        settings.McpLoggingEnabled = _output.McpLoggingEnabled;
        settings.AllowRemoteApiAccess = _output.AllowRemoteApiAccess;
        settings.McpDestructiveConfirmationEnabled = McpDestructiveConfirmationEnabled;
        WinContainers.Service.Mcp.McpDestructiveConfirmationPolicy.SetEnabled(McpDestructiveConfirmationEnabled);
        _settingsService.Save(settings);
    }

    public void SaveAiSettings()
    {
        var settings = _settingsService.Load();
        settings.AiProviderKind = string.Equals(AiProviderKind, "Ollama", StringComparison.OrdinalIgnoreCase)
            ? "Ollama"
            : "OpenAiCompatible";
        settings.AiEndpoint = string.IsNullOrWhiteSpace(AiEndpoint) ? "https://api.openai.com/v1" : AiEndpoint.Trim();
        settings.AiModel = string.IsNullOrWhiteSpace(AiModel) ? "gpt-4o-mini" : AiModel.Trim();
        settings.AiApiKey = AiApiKey;
        settings.AiConfirmDestructiveActions = AiConfirmDestructiveActions;
        settings.McpDestructiveConfirmationEnabled = McpDestructiveConfirmationEnabled;
        _settingsService.Save(settings);
        AiStatusText = "AI assistant settings saved.";
    }

    public async Task CheckAppUpdateAsync()
    {
        IsCheckingAppUpdate = true;
        AppUpdateAvailable = false;
        _availableAppUpdate = null;
        try
        {
            var settings = _settingsService.Load();
            var update = await UpdateService.CheckForUpdatesAsync(UpdateChannel);
            settings.LastUpdateCheckUtc = DateTimeOffset.UtcNow;
            _settingsService.Save(settings);
            _availableAppUpdate = update;
            AppUpdateAvailable = update is not null && !string.Equals(
                settings.DeferredUpdateVersion, update.TargetFullRelease.Version.ToString(), StringComparison.OrdinalIgnoreCase);
            AppUpdateStatus = AppUpdateAvailable
                ? $"WinContainers {update!.TargetFullRelease.Version} is available."
                : $"WinContainers is up to date ({AppVersion}).";
        }
        catch (Exception ex)
        {
            AppUpdateStatus = $"Update check failed: {ex.Message}";
            _output.Write(AppUpdateStatus, LogLevel.Warning);
        }
        finally
        {
            IsCheckingAppUpdate = false;
        }
    }

    public async Task InstallAppUpdateAsync()
    {
        if (_availableAppUpdate is null)
        {
            return;
        }

        IsCheckingAppUpdate = true;
        try
        {
            AppUpdateStatus = IsPortable
                ? "Downloading the portable update. Close WinContainers and replace the portable folder when prompted."
                : $"Downloading WinContainers {_availableAppUpdate.TargetFullRelease.Version}...";
            await UpdateService.DownloadAndApplyAsync(_availableAppUpdate, UpdateChannel);
            AppUpdateAvailable = false;
            _availableAppUpdate = null;
            AppUpdateStatus = IsPortable
                ? "Portable update downloaded. Restart from the new folder to finish the update."
                : "Update downloaded. WinContainers will restart to apply it.";
        }
        catch (Exception ex)
        {
            AppUpdateStatus = $"Update install failed: {ex.Message}";
            _output.Write(AppUpdateStatus, LogLevel.Error);
        }
        finally
        {
            IsCheckingAppUpdate = false;
        }
    }

    public void DeferAppUpdate()
    {
        if (_availableAppUpdate is null)
        {
            return;
        }

        var settings = _settingsService.Load();
        settings.DeferredUpdateVersion = _availableAppUpdate.TargetFullRelease.Version.ToString();
        _settingsService.Save(settings);
        AppUpdateAvailable = false;
        AppUpdateStatus = "Update deferred. Use Check for updates to review it again.";
    }

    public async Task CheckWslcUpdateAsync()
    {
        IsCheckingWslcUpdate = true;
        WslcUpdateAvailable = false;
        _availableWslcUpdate = null;
        try
        {
            var installedVersion = await _serviceClient.GetVersionAsync();
            _availableWslcUpdate = await _wslcUpdateService.CheckForUpdateAsync(installedVersion);
            WslcUpdateAvailable = _availableWslcUpdate is not null;
            WslcUpdateStatus = WslcUpdateAvailable
                ? $"WSLC {_availableWslcUpdate!.Version} is available."
                : $"WSLC is up to date ({WslcVersionFormatter.Format(installedVersion)}).";
        }
        catch (Exception ex)
        {
            WslcUpdateStatus = $"Update check failed: {ex.Message}";
            _output.Write(WslcUpdateStatus, LogLevel.Warning);
        }
        finally
        {
            IsCheckingWslcUpdate = false;
        }
    }

    public async Task UpdateWslcAsync()
    {
        if (_availableWslcUpdate is null)
        {
            return;
        }

        IsCheckingWslcUpdate = true;
        try
        {
            WslcUpdateStatus = $"Downloading WSLC {_availableWslcUpdate.Version}...";
            await _wslcUpdateService.InstallAsync(_availableWslcUpdate);
            WslcUpdateStatus = "WSLC updated. Restart Windows if wslc is not available yet.";
            WslcUpdateAvailable = false;
            _availableWslcUpdate = null;
            await LoadAsync();
        }
        catch (Exception ex)
        {
            WslcUpdateStatus = $"WSLC update failed: {ex.Message}";
            _output.Write(WslcUpdateStatus, LogLevel.Error);
        }
        finally
        {
            IsCheckingWslcUpdate = false;
        }
    }
}
