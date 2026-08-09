using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using WinContainers.Core;

namespace WinContainers.Runtime;

public sealed class WslcDriver : IWslcDriver
{
    private const int DefaultTimeoutMs = 30000;
    private const int RuntimeProbeTimeoutMs = 15000;
    private const int SlowTimeoutMs = 120000;
    private const int OutputCleanupTimeoutMs = 5000;
    private const long MaxImageTarBytes = 512L * 1024 * 1024;
    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            // --version only proves that the CLI is installed. Probe the runtime
            // itself so a broken WSL/WSLC installation is reported as unavailable.
            var result = await RunAsync(WslcCommands.ContainerPs(), RuntimeProbeTimeoutMs, ct);
            return result.ExitCode == 0;
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] IsAvailableAsync failed: {ex}");
            return false;
        }
    }

    public Task<string> GetVersionAsync(CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.Version(), DefaultTimeoutMs, ct);

    public Task<string> GetContainersAsync(CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerPs(), SlowTimeoutMs, ct);

    public Task<string> StartContainerAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerStart(id), DefaultTimeoutMs, ct);

    public Task<string> StopContainerAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerStop(id), DefaultTimeoutMs, ct);

    public Task<string> RestartContainerAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerRestart(id), DefaultTimeoutMs, ct);

    public Task<string> RenameContainerAsync(string id, string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerRename(id, name), DefaultTimeoutMs, ct);

    public Task<string> RemoveContainerAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerRemove(id), DefaultTimeoutMs, ct);

    public Task<string> InspectContainerAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerInspect(id), DefaultTimeoutMs, ct);

    public Task<string> GetContainerLogsAsync(string id, int tail, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ContainerLogs(id, tail), SlowTimeoutMs, ct);

    public Task<string> GetImagesAsync(CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ImageLs(), SlowTimeoutMs, ct);

    public Task<string> PullImageAsync(string image, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ImagePull(image), 1800000, ct);

    public async Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct)
    {
        var hasTarPath = !string.IsNullOrWhiteSpace(tarPath);
        var hasTarData = !string.IsNullOrWhiteSpace(tarData);

        if (hasTarPath == hasTarData)
        {
            return "Validation error: provide exactly one of tarPath or tarData.";
        }

        if (hasTarPath)
        {
            if (!IsValidTarPath(tarPath!))
            {
                return "Validation error: tarPath must point to an existing .tar file.";
            }

            return await RunAndCaptureAsync(WslcCommands.ImageLoad(tarPath!), 1800000, ct);
        }

        var base64 = tarData!;
        if (TryGetMaximumDecodedBytes(base64, out var maxDecodedBytes) && maxDecodedBytes > MaxImageTarBytes)
        {
            return "Validation error: tarData exceeds 512 MB after decoding.";
        }

        byte[] decodedBytes;
        try
        {
            decodedBytes = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            return "Validation error: tarData is not valid base64.";
        }

        if (decodedBytes.LongLength > MaxImageTarBytes)
        {
            return "Validation error: tarData exceeds 512 MB after decoding.";
        }

        var tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tar");
        try
        {
            await File.WriteAllBytesAsync(tempPath, decodedBytes, ct);
            return await RunAndCaptureAsync(WslcCommands.ImageLoad(tempPath), 1800000, ct);
        }
        finally
        {
            TryDeleteFile(tempPath);
        }
    }

    public Task<string> RemoveImageAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ImageRemove(id), DefaultTimeoutMs, ct);

    public Task<string> InspectImageAsync(string id, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.ImageInspect(id), DefaultTimeoutMs, ct);

    public Task<string> GetVolumesAsync(CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.VolumeLs(), DefaultTimeoutMs, ct);

    public Task<string> CreateVolumeAsync(string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.VolumeCreate(name), DefaultTimeoutMs, ct);

    public Task<string> RemoveVolumeAsync(string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.VolumeRemove(name), DefaultTimeoutMs, ct);

    public Task<string> InspectVolumeAsync(string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.VolumeInspect(name), DefaultTimeoutMs, ct);

    public Task<string> GetNetworksAsync(CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.NetworkLs(), DefaultTimeoutMs, ct);

    public Task<string> CreateNetworkAsync(string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.NetworkCreate(name), DefaultTimeoutMs, ct);

    public Task<string> RemoveNetworkAsync(string name, CancellationToken ct) =>
        RunAndCaptureAsync(WslcCommands.NetworkRemove(name), DefaultTimeoutMs, ct);

    public Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, CancellationToken ct = default) =>
        RunAndCaptureAsync(WslcCommands.Run(image, name, ports, volumes, env), DefaultTimeoutMs, ct);

    public Task<string> ExecCommandAsync(string id, string command, CancellationToken ct = default)
    {
        return RunAndCaptureAsync(WslcCommands.ContainerExecCommand(id, command), DefaultTimeoutMs, ct);
    }

    public Task<string> ExecShellAsync(string id, string shellCommand, string? shell = null, CancellationToken ct = default)
    {
        shell ??= "sh";
        return RunAndCaptureAsync(WslcCommands.ContainerExecShell(id, shellCommand, shell), DefaultTimeoutMs, ct);
    }

    public Process StartStreamingProcess(string arguments)
    {
        var process = new Process
        {
            StartInfo = BuildStartInfo(arguments),
            EnableRaisingEvents = true
        };
        process.Start();
        return process;
    }

    private static async Task<string> RunAndCaptureAsync(string arguments, int timeoutMs, CancellationToken ct)
    {
        var result = await RunAsync(arguments, timeoutMs, ct);
        if (result.ExitCode != 0)
        {
            var error = string.IsNullOrWhiteSpace(result.Stderr) ? result.Stdout : result.Stderr;
            return $"wslc error ({result.ExitCode}): {error.Trim()}";
        }
        return result.Stdout;
    }

    private static async Task<RunResult> RunAsync(string arguments, int timeoutMs, CancellationToken ct)
    {
        using var process = new Process
        {
            StartInfo = BuildStartInfo(arguments),
            EnableRaisingEvents = true
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        using var timeoutCts = new CancellationTokenSource(timeoutMs);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);
            return new RunResult(
                process.ExitCode,
                (await stdoutTask).Trim(),
                (await stderrTask).Trim());
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !ct.IsCancellationRequested)
        {
            TryKill(process);
            await DrainOutputAsync(stdoutTask, stderrTask);
            return new RunResult(-1, string.Empty, $"Command timed out after {timeoutMs}ms.");
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested && !timeoutCts.IsCancellationRequested)
        {
            TryKill(process);
            await DrainOutputAsync(stdoutTask, stderrTask);
            throw;
        }
    }

    private static bool IsValidTarPath(string tarPath) =>
        File.Exists(tarPath) && string.Equals(Path.GetExtension(tarPath), ".tar", StringComparison.OrdinalIgnoreCase);

    private static bool TryGetMaximumDecodedBytes(string base64, out long maxDecodedBytes)
    {
        var contentLength = 0;
        for (var i = 0; i < base64.Length; i++)
        {
            if (!char.IsWhiteSpace(base64[i]))
            {
                contentLength++;
            }
        }

        return TryGetMaximumDecodedBytes(contentLength, CountBase64PaddingChars(base64), out maxDecodedBytes);
    }

    private static int CountBase64PaddingChars(string base64)
    {
        var padding = 0;
        for (var i = base64.Length - 1; i >= 0; i--)
        {
            var c = base64[i];
            if (char.IsWhiteSpace(c))
            {
                continue;
            }

            if (c != '=')
            {
                break;
            }

            padding++;
            if (padding == 2)
            {
                break;
            }
        }

        return padding;
    }

    private static bool TryGetMaximumDecodedBytes(int base64ContentLength, int paddingChars, out long maxDecodedBytes)
    {
        var fullGroups = base64ContentLength / 4;
        var remainder = base64ContentLength % 4;
        maxDecodedBytes = remainder switch
        {
            0 => fullGroups * 3L - Math.Min(paddingChars, 2),
            2 => fullGroups * 3L + 1,
            3 => fullGroups * 3L + 2,
            _ => 0
        };

        return remainder is 0 or 2 or 3;
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Temp file cleanup failed: {ex}");
        }
    }

    private static async Task DrainOutputAsync(Task<string> stdoutTask, Task<string> stderrTask)
    {
        var outputTask = Task.WhenAll(stdoutTask, stderrTask);

        try
        {
            await outputTask.WaitAsync(TimeSpan.FromMilliseconds(OutputCleanupTimeoutMs));
        }
        catch (TimeoutException)
        {
            Trace.WriteLine($"[WslcDriver] Output cleanup timed out after {OutputCleanupTimeoutMs}ms.");
            _ = outputTask.ContinueWith(
                task => _ = task.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Output cleanup failed: {ex}");
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException ex)
        {
            Trace.WriteLine($"[WslcDriver] Process kill skipped: {ex.Message}");
        }
        catch (Win32Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Process kill failed: {ex.Message}");
        }
    }

    private static ProcessStartInfo BuildStartInfo(string arguments)
    {
        var wslcPath = RuntimeTools.ResolveExecutablePath("wslc");
        if (string.IsNullOrEmpty(wslcPath))
        {
            throw new FileNotFoundException(
                "wslc.exe could not be found. Install WSLC from Microsoft and ensure it is on PATH.",
                "wslc.exe");
        }

        return new ProcessStartInfo(wslcPath, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetTempPath()
        };
    }

    private readonly record struct RunResult(int ExitCode, string Stdout, string Stderr);
}
