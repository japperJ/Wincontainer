using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace WinContainers_App.Services;

public sealed class AppSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly string _settingsPath;
    private readonly object _lock = new();

    public AppSettingsService()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var directory = Path.Combine(appDataPath, "WinContainers");
        Directory.CreateDirectory(directory);
        _settingsPath = Path.Combine(directory, "settings.json");
    }

    public AppSettings Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_settingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                settings.ApiToken = UnprotectToken(settings.ApiToken);
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Load failed: {ex}");
                return new AppSettings();
            }
        }
    }

    public void Save(AppSettings settings)
    {
        lock (_lock)
        {
            try
            {
                var originalToken = settings.ApiToken;
                settings.ApiToken = ProtectToken(originalToken);
                try
                {
                    var json = JsonSerializer.Serialize(settings, JsonOptions);
                    File.WriteAllText(_settingsPath, json);
                }
                finally
                {
                    settings.ApiToken = originalToken;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AppSettingsService] Save failed: {ex}");
            }
        }
    }

    private static string ProtectToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return token ?? string.Empty;
        }

        var bytes = Encoding.UTF8.GetBytes(token);
        var protectedBytes = ProtectedData.Protect(bytes, null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(protectedBytes);
    }

    private static string UnprotectToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return token ?? string.Empty;
        }

        try
        {
            var bytes = Convert.FromBase64String(token);
            var unprotected = ProtectedData.Unprotect(bytes, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(unprotected);
        }
        catch
        {
            return token;
        }
    }
}
