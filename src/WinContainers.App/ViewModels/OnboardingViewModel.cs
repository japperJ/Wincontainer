using System.Diagnostics;
using WinContainers_App.Services;
using LogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.ViewModels;

public partial class OnboardingViewModel : ViewModelBase
{
    private readonly IOutputService _output;

    private bool _wsl2Available;
    public bool Wsl2Available
    {
        get => _wsl2Available;
        set => SetProperty(ref _wsl2Available, value);
    }

    private string _wsl2Status = "Checking...";
    public string Wsl2Status
    {
        get => _wsl2Status;
        set => SetProperty(ref _wsl2Status, value);
    }

    private bool _wslcAvailable;
    public bool WslcAvailable
    {
        get => _wslcAvailable;
        set => SetProperty(ref _wslcAvailable, value);
    }

    private string _wslcStatus = "Checking...";
    public string WslcStatus
    {
        get => _wslcStatus;
        set => SetProperty(ref _wslcStatus, value);
    }

    private bool _virtualizationAvailable;
    public bool VirtualizationAvailable
    {
        get => _virtualizationAvailable;
        set => SetProperty(ref _virtualizationAvailable, value);
    }

    private string _virtualizationStatus = "Checking...";
    public string VirtualizationStatus
    {
        get => _virtualizationStatus;
        set => SetProperty(ref _virtualizationStatus, value);
    }

    private bool _windowsVersionOk = true;
    public bool WindowsVersionOk
    {
        get => _windowsVersionOk;
        set => SetProperty(ref _windowsVersionOk, value);
    }

    private string _windowsVersionStatus = "Windows 11 detected";
    public string WindowsVersionStatus
    {
        get => _windowsVersionStatus;
        set => SetProperty(ref _windowsVersionStatus, value);
    }

    private bool _isChecking;
    public bool IsChecking
    {
        get => _isChecking;
        set => SetProperty(ref _isChecking, value);
    }

    private bool _isInstalling;
    public bool IsInstalling
    {
        get => _isInstalling;
        set => SetProperty(ref _isInstalling, value);
    }

    private string _installProgress = "";
    public string InstallProgress
    {
        get => _installProgress;
        set => SetProperty(ref _installProgress, value);
    }

    public bool AllPrerequisitesMet => Wsl2Available && WslcAvailable;

    public OnboardingViewModel(IOutputService output)
    {
        _output = output;
    }

    public async Task CheckAllAsync()
    {
        IsChecking = true;
        _output.Write("Checking prerequisites...", LogLevel.Info);

        await CheckWsl2Async();
        await CheckWslcAsync();
        await CheckVirtualizationAsync();
        CheckWindowsVersion();

        OnPropertyChanged(nameof(AllPrerequisitesMet));
        IsChecking = false;

        if (AllPrerequisitesMet)
        {
            _output.Write("All prerequisites met!", LogLevel.Info);
        }
        else
        {
            _output.Write("Some prerequisites are missing. Install them to continue.", LogLevel.Warning);
        }
    }

    private async Task CheckWsl2Async()
    {
        try
        {
            var result = await RunPowerShellCommandAsync("wsl --status");
            var output = NormalizeCommandOutput(result.Output);
            Wsl2Available = result.ExitCode == 0 && output.Contains("Default Version: 2", StringComparison.OrdinalIgnoreCase);
            Wsl2Status = Wsl2Available
                ? "WSL2 is installed and configured"
                : $"WSL2 is not installed or not configured as default ({output})";
        }
        catch
        {
            Wsl2Available = false;
            Wsl2Status = "WSL2 is not available";
        }
    }

    private async Task CheckWslcAsync()
    {
        try
        {
            var result = await RunPowerShellCommandAsync("wslc --version");
            var output = NormalizeCommandOutput(result.Output);
            WslcAvailable = result.ExitCode == 0;
            WslcStatus = WslcAvailable ? FormatWslcStatus(output) : $"WSLC is not installed ({output})";
        }
        catch
        {
            WslcAvailable = false;
            WslcStatus = "WSLC is not available";
        }
    }

    private static string FormatWslcStatus(string output)
    {
        // wslc --version currently prints:
        //   wslc compatibility bridge (nerdctl backend)
        //   nerdctl version 2.3.1
        // We prefer a clean "WSLC version X.Y.Z" message.
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("nerdctl version", StringComparison.OrdinalIgnoreCase))
            {
                var version = trimmed["nerdctl version".Length..].Trim();
                return $"WSLC is installed: version {version}";
            }
        }

        return "WSLC is installed";
    }

    private async Task CheckVirtualizationAsync()
    {
        try
        {
            var result = await RunPowerShellCommandAsync("systeminfo");
            var output = NormalizeCommandOutput(result.Output);
            var firmwareDisabled = output.Contains("Virtualization Enabled In Firmware: No", StringComparison.OrdinalIgnoreCase);
            var firmwareEnabled = output.Contains("Virtualization Enabled In Firmware: Yes", StringComparison.OrdinalIgnoreCase);
            var platformReady = output.Contains("A hypervisor has been detected", StringComparison.OrdinalIgnoreCase);

            VirtualizationAvailable = !firmwareDisabled && (firmwareEnabled || platformReady);
            VirtualizationStatus = firmwareDisabled
                ? "Virtualization is disabled in firmware/BIOS"
                : VirtualizationAvailable
                    ? "Virtualization is enabled"
                    : "Virtualization is not available; enable Virtual Machine Platform and BIOS virtualization";
        }
        catch
        {
            VirtualizationAvailable = false;
            VirtualizationStatus = "Unable to check virtualization status";
        }
    }

    private void CheckWindowsVersion()
    {
        var version = Environment.OSVersion.Version;
        WindowsVersionOk = version.Major >= 10 && version.Build >= 22000;
        WindowsVersionStatus = WindowsVersionOk
            ? $"Windows 11 (build {version.Build}) detected"
            : $"Windows {version.Major}.{version.Minor} (build {version.Build}) - Windows 11 required";
    }

    public async Task InstallWsl2Async()
    {
        IsInstalling = true;
        InstallProgress = "Installing WSL2 (this may take several minutes)...";
        _output.Write("Installing WSL2... Downloading from Windows Update if needed.", LogLevel.Info);

        try
        {
            var result = await RunElevatedCommandAsync(
                "Write-Output 'Enabling WSL2 and Virtual Machine Platform features...'; " +
                "$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User'); " +
                "wsl --install --no-distribution; " +
                "Write-Output 'WSL2 install command completed.'", 600);
            _output.Write(result.Output, LogLevel.Info);

            if (result.ExitCode == 0)
            {
                InstallProgress = "WSL2 installed. Checking status...";
                await CheckWsl2Async();
                OnPropertyChanged(nameof(AllPrerequisitesMet));
                _output.Write("WSL2 installation completed. A reboot may be required.", LogLevel.Info);
            }
            else
            {
                InstallProgress = "WSL2 installation failed. Check output for details.";
                _output.Write($"WSL2 installation failed (exit {result.ExitCode}): {result.Output}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            InstallProgress = $"Installation error: {ex.Message}";
            _output.Write($"WSL2 installation error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    public async Task InstallWslcAsync()
    {
        IsInstalling = true;
        InstallProgress = "Installing WSLC (download + install can take several minutes)...";
        _output.Write("Installing WSLC (WSL Containers)...", LogLevel.Info);

        try
        {
            var result = await RunElevatedCommandAsync(
                "$url = 'https://github.com/microsoft/WSL/releases/download/2.9.3/wsl.2.9.3.0.x64.msi'; " +
                "$path = Join-Path $env:TEMP 'wsl.2.9.3.0.x64.msi'; " +
                "$expected = '7281640D2DC64BAE2044A466A336A9460B497F964BFB3E949B270D2F4CFCD48D'; " +
                "if (!(Test-Path $path) -or ((Get-FileHash -Algorithm SHA256 $path).Hash -ne $expected)) { Write-Output 'Downloading WSL 2.9.3 MSI...'; Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $path }; " +
                "$hash = (Get-FileHash -Algorithm SHA256 $path).Hash; " +
                "if ($hash -ne $expected) { throw 'WSL installer hash verification failed.' }; " +
                "Write-Output 'Installing WSL 2.9.3 MSI (this can take several minutes)...'; " +
                "$log = Join-Path $env:LOCALAPPDATA 'WinContainers\\wsl-install.log'; " +
                "$installer = Start-Process msiexec.exe -ArgumentList '/i', $path, '/qn', '/norestart', '/l*v', $log -Wait -PassThru; " +
                "if ($installer.ExitCode -notin @(0, 3010)) { exit $installer.ExitCode }; " +
                "Write-Output ('WSL 2.9.3 installed. MSI exit code: ' + $installer.ExitCode); Write-Output ('MSI log: ' + $log)", 1200);
            _output.Write(result.Output, LogLevel.Info);

            if (result.ExitCode == 0)
            {
                InstallProgress = "WSLC installed. Checking status...";
                await CheckWslcAsync();
                OnPropertyChanged(nameof(AllPrerequisitesMet));
                _output.Write("WSLC installation completed. Restart Windows if wslc is not available yet.", LogLevel.Info);
            }
            else
            {
                InstallProgress = "WSLC installation failed. Check output for details.";
                _output.Write($"WSLC installation failed (exit {result.ExitCode}): {result.Output}", LogLevel.Error);
            }
        }
        catch (Exception ex)
        {
            InstallProgress = $"Installation error: {ex.Message}";
            _output.Write($"WSLC installation error: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            IsInstalling = false;
        }
    }

    public void MarkOnboardingComplete()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var markerPath = Path.Combine(appDataPath, "WinContainers", ".first-run-complete");
        var directory = Path.GetDirectoryName(markerPath);

        if (directory != null && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(markerPath, DateTime.UtcNow.ToString("O"));
        _output.Write("Onboarding marked as complete.", LogLevel.Info);
    }

    public static bool IsFirstRun()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var markerPath = Path.Combine(appDataPath, "WinContainers", ".first-run-complete");
        return !File.Exists(markerPath);
    }

    private static async Task<(int ExitCode, string Output)> RunPowerShellCommandAsync(string command, int timeoutSeconds = 15)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"$env:Path = [Environment]::GetEnvironmentVariable('Path', 'Machine') + ';' + [Environment]::GetEnvironmentVariable('Path', 'User'); {command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch { }

            return (-1, $"Command timed out after {timeoutSeconds} seconds.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        var output = string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}";
        return (process.ExitCode, output.Trim());
    }

    private static async Task<(int ExitCode, string Output)> RunElevatedCommandAsync(string command, int timeoutSeconds)
    {
        var scriptDir = Path.Combine(Path.GetTempPath(), "WinContainers");
        Directory.CreateDirectory(scriptDir);
        var runId = Guid.NewGuid().ToString("N");
        var scriptPath = Path.Combine(scriptDir, $"elevated-cmd-{runId}.ps1");
        var launcherPath = Path.Combine(scriptDir, $"elevated-launcher-{runId}.ps1");
        var logPath = Path.Combine(scriptDir, $"elevated-output-{runId}.log");

        File.WriteAllText(scriptPath, $"{command}\n");
        var escapedScriptPath = EscapePowerShellString(scriptPath);
        var escapedLogPath = EscapePowerShellString(logPath);
        var launcher = "$ErrorActionPreference = 'Stop'\n" +
            "try {\n" +
            $"    & powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File '{escapedScriptPath}' *> '{escapedLogPath}'\n" +
            "    exit $LASTEXITCODE\n" +
            "} catch {\n" +
            $"    $_ | Out-File -FilePath '{escapedLogPath}' -Append\n" +
            "    exit 1\n" +
            "}\n";
        File.WriteAllText(launcherPath, launcher);

        var elevatedCommand = $"try {{ $p = Start-Process -FilePath 'powershell.exe' -ArgumentList @('-NoProfile','-NonInteractive','-ExecutionPolicy','Bypass','-File','{EscapePowerShellString(launcherPath)}') -Verb RunAs -Wait -PassThru; exit $p.ExitCode }} catch {{ $_ | Out-File -FilePath '{EscapePowerShellString(logPath)}' -Append; exit 1223 }}";
        var encodedCommand = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(elevatedCommand));

        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -EncodedCommand {encodedCommand}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
            await Task.WhenAll(stdoutTask, stderrTask);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                    process.Kill(entireProcessTree: true);
            }
            catch { }

            return (-1, $"Command timed out after {timeoutSeconds} seconds.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var logOutput = File.Exists(logPath) ? await File.ReadAllTextAsync(logPath) : "";
        var combined = string.IsNullOrWhiteSpace(logOutput)
            ? (string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}")
            : logOutput;

        try { File.Delete(scriptPath); } catch { }
        try { File.Delete(launcherPath); } catch { }
        try { File.Delete(logPath); } catch { }

        return (process.ExitCode, combined.Trim());
    }

    private static string EscapePowerShellString(string value) =>
        value.Replace("'", "''", StringComparison.Ordinal);

    private static string NormalizeCommandOutput(string output) =>
        output.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();
}
