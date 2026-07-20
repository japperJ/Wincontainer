namespace WinContainers_App.Services;

public sealed class AppSettings
{
    public string? ApiToken { get; set; }

    public bool ApiLoggingEnabled { get; set; }

    public bool RemoteApiLoggingEnabled { get; set; }
}
