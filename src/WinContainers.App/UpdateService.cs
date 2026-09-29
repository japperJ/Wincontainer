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

        try
        {
            await updateManager.DownloadUpdatesAsync(update);
        }
        catch (Exception ex) when (IsMissingAssetFailure(ex))
        {
            throw new UpdatePackageMissingException(
                $"The GitHub release for this version does not contain its update package. " +
                $"Download the installer from {GitHubRepoUrl}/releases.", ex);
        }

        // Velopack waits for this process to exit before replacing the running release.
        updateManager.WaitExitThenApplyUpdates(update);
    }

    private static bool IsMissingAssetFailure(Exception ex)
    {
        for (var current = ex; current is not null; current = current.InnerException)
        {
            if (current.Message.Contains("Could not find asset", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("Could not find release", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public sealed class UpdatePackageMissingException : Exception
{
    public UpdatePackageMissingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
