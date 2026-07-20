using WinContainers.Core;
using WinContainers.Core.Models;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly AppSettingsService _settingsService;

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

    public SettingsViewModel(IOutputService output, AppSettingsService settingsService)
    {
        _output = output;
        _settingsService = settingsService;
    }

    public async Task LoadAsync()
    {
        ApiLoggingEnabled = _output.ApiLoggingEnabled;
        RemoteApiLoggingEnabled = _output.RemoteApiLoggingEnabled;
        PortText = Environment.GetEnvironmentVariable("WINCONTAINERS_SERVICE_PORT") ?? "5123";
        TokenText = ServiceEndpointResolver.ResolveToken();
        StatusText = $"Current endpoint: {ServiceEndpointResolver.Resolve()}";

        try
        {
            ServiceHealthy = await App.ServiceClient.IsHealthyAsync();
            var version = await App.ServiceClient.GetVersionAsync();
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
        Environment.SetEnvironmentVariable("WINCONTAINERS_SERVICE_TOKEN", token);

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
        _settingsService.Save(settings);
    }
}
