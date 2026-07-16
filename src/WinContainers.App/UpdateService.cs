using Velopack;
using Velopack.Sources;

namespace WinContainers_App;

public static class UpdateService
{
    private const string GitHubRepoUrl = "https://github.com/YOUR_USER/WinContainers";

    public static void CheckForUpdates()
    {
        try
        {
            var updateManager = new UpdateManager(
                new GithubSource(GitHubRepoUrl, null, false));

            var newVersion = updateManager.CheckForUpdates();
            if (newVersion != null)
            {
                updateManager.DownloadUpdates(newVersion);
                updateManager.ApplyUpdatesAndRestart(newVersion);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update check failed: {ex.Message}");
        }
    }
}
