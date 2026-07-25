using Velopack;
using Velopack.Sources;
using System.Reflection;

namespace WinContainers_App;

public static class UpdateService
{
    public const string GitHubRepoUrl = "https://github.com/japperJ/Wincontainer";
    public const string StableChannel = "stable";
    public const string BetaChannel = "beta";

    public static string CurrentVersion =>
        Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version
        ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
        ?? "0.0.0";

    public static bool IsPortable => new UpdateManager(
        new GithubSource(GitHubRepoUrl, null, false)).IsPortable;

    public static async Task<UpdateInfo?> CheckForUpdatesAsync(string channel = StableChannel)
    {
        var updateManager = new UpdateManager(
            new GithubSource(GitHubRepoUrl, null, channel.Equals(BetaChannel, StringComparison.OrdinalIgnoreCase)));

        return await updateManager.CheckForUpdatesAsync();
    }

    public static async Task DownloadAndApplyAsync(UpdateInfo update, string channel)
    {
        var updateManager = new UpdateManager(
            new GithubSource(GitHubRepoUrl, null, channel.Equals(BetaChannel, StringComparison.OrdinalIgnoreCase)));

        await updateManager.DownloadUpdatesAsync(update);
        // Velopack waits for this process to exit before replacing the running release.
        updateManager.WaitExitThenApplyUpdates(update);
    }
}
