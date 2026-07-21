using Velopack;
using Velopack.Sources;

namespace WinContainers_App;

public static class UpdateService
{
    private const string GitHubRepoUrl = "https://github.com/japperJ/Wincontainer";

    public static async Task CheckForUpdatesAsync()
    {
        try
        {
            var updateManager = new UpdateManager(
                new GithubSource(GitHubRepoUrl, null, false));

            var newVersion = await updateManager.CheckForUpdatesAsync();
            if (newVersion != null)
            {
                await updateManager.DownloadUpdatesAsync(newVersion);
                // The app hosts Kestrel and a tray thread. Let Velopack coordinate
                // process shutdown before replacing files from the running release.
                updateManager.WaitExitThenApplyUpdates(newVersion);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
