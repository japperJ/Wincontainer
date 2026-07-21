using System.Diagnostics;
using System.Reflection;

const string installedRoot = "WinContainers";
const string payloadResourceName = "WinContainers.VelopackSetup.exe";

try
{
    CloseInstalledApplication();
    StopLegacyKeepAliveProcesses();

    var payloadPath = Path.Combine(
        Path.GetTempPath(),
        $"WinContainers-Setup-{Environment.ProcessId}-{Guid.NewGuid():N}.exe");

    await ExtractPayloadAsync(payloadPath);

    try
    {
        using var installer = new Process
        {
            StartInfo = new ProcessStartInfo(payloadPath)
            {
                UseShellExecute = true
            }
        };

        foreach (var argument in args)
            installer.StartInfo.ArgumentList.Add(argument);

        installer.Start();
        await installer.WaitForExitAsync();
        return installer.ExitCode;
    }
    finally
    {
        TryDelete(payloadPath);
    }
}
catch (Exception ex)
{
    Console.Error.WriteLine($"WinContainers setup bootstrapper failed: {ex.Message}");
    return 1;
}

static void CloseInstalledApplication()
{
    var installDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        installedRoot);

    foreach (var process in Process.GetProcessesByName("WinContainers.App"))
    {
        try
        {
            var executablePath = process.MainModule?.FileName;
            if (string.IsNullOrWhiteSpace(executablePath)
                || !executablePath.StartsWith(installDirectory, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!process.HasExited)
                process.CloseMainWindow();

            if (!process.WaitForExit(TimeSpan.FromSeconds(15)))
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(TimeSpan.FromSeconds(5));
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or UnauthorizedAccessException)
        {
            // Velopack will report a useful access-denied error if a lock remains.
        }
        finally
        {
            process.Dispose();
        }
    }
}

static async Task ExtractPayloadAsync(string destinationPath)
{
    await using var source = Assembly.GetExecutingAssembly()
        .GetManifestResourceStream(payloadResourceName)
        ?? throw new InvalidOperationException("The Velopack setup payload is missing.");
    await using var destination = File.Create(destinationPath);
    await source.CopyToAsync(destination);
}

static void StopLegacyKeepAliveProcesses()
{
    const string command = "Get-CimInstance Win32_Process | "
        + "Where-Object { $_.Name -eq 'wsl.exe' -and $_.CommandLine -like '*-u root*--exec sleep infinity*' } | "
        + "ForEach-Object { Stop-Process -Id $_.ProcessId -Force }";

    try
    {
        using var cleanup = new Process
        {
            StartInfo = new ProcessStartInfo("powershell.exe")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetTempPath()
            }
        };
        cleanup.StartInfo.ArgumentList.Add("-NoProfile");
        cleanup.StartInfo.ArgumentList.Add("-NonInteractive");
        cleanup.StartInfo.ArgumentList.Add("-ExecutionPolicy");
        cleanup.StartInfo.ArgumentList.Add("Bypass");
        cleanup.StartInfo.ArgumentList.Add("-Command");
        cleanup.StartInfo.ArgumentList.Add(command);
        cleanup.Start();
        cleanup.WaitForExit(10_000);
    }
    catch
    {
        // The payload will report the remaining lock if legacy cleanup is unavailable.
    }
}

static void TryDelete(string path)
{
    try
    {
        if (File.Exists(path))
            File.Delete(path);
    }
    catch
    {
        // The temporary payload can be removed by the OS later.
    }
}
