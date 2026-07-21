using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using WinContainers.Core;

namespace WinContainers_App.Services;

public sealed record WslcUpdateInfo(string Version, string DownloadUrl, string Sha256);

public sealed class WslcUpdateService
{
    private const string ReleasesUrl = "https://api.github.com/repos/microsoft/WSL/releases?per_page=20";
    private readonly HttpClient _http = new();

    public WslcUpdateService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("WinContainers", "1.0"));
    }

    public async Task<WslcUpdateInfo?> CheckForUpdateAsync(string installedVersion, CancellationToken cancellationToken = default)
    {
        var installed = ParseVersion(installedVersion);
        if (installed is null)
        {
            return null;
        }

        using var response = await _http.GetAsync(ReleasesUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        WslcUpdateInfo? newest = null;
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if (!release.TryGetProperty("tag_name", out var tagElement))
            {
                continue;
            }

            var version = ParseVersion(tagElement.GetString());
            if (version is null || version <= installed)
            {
                continue;
            }

            foreach (var asset in release.GetProperty("assets").EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? string.Empty;
                if (!name.StartsWith($"wsl.{version}", StringComparison.OrdinalIgnoreCase) ||
                    !name.EndsWith(".x64.msi", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var digest = asset.GetProperty("digest").GetString() ?? string.Empty;
                if (!digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var update = new WslcUpdateInfo(
                    version.ToString(),
                    asset.GetProperty("browser_download_url").GetString() ?? string.Empty,
                    digest["sha256:".Length..]);

                if (newest is null || ParseVersion(newest.Version) < version)
                {
                    newest = update;
                }
            }
        }

        return newest;
    }

    public async Task InstallAsync(WslcUpdateInfo update, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetTempPath(), $"wsl.{update.Version}.x64.msi");
        try
        {
            await using (var source = await _http.GetStreamAsync(update.DownloadUrl, cancellationToken))
            await using (var destination = File.Create(path))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            await using var file = File.OpenRead(path);
            var hash = Convert.ToHexString(await SHA256.HashDataAsync(file, cancellationToken));
            if (!hash.Equals(update.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("WSLC installer hash verification failed.");
            }

            using var installer = Process.Start(new ProcessStartInfo
            {
                FileName = "msiexec.exe",
                UseShellExecute = true,
                Verb = "runas",
                Arguments = $"/i \"{path}\" /qn /norestart"
            }) ?? throw new InvalidOperationException("Could not start the WSLC installer.");

            await installer.WaitForExitAsync(cancellationToken);
            if (installer.ExitCode is not (0 or 3010))
            {
                throw new InvalidOperationException($"WSLC installation failed with exit code {installer.ExitCode}.");
            }
        }
        finally
        {
            try { File.Delete(path); } catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static Version? ParseVersion(string? value)
    {
        var formatted = value is null ? string.Empty : WslcVersionFormatter.Format(value);
        return Version.TryParse(formatted, out var version) ? version : null;
    }
}
