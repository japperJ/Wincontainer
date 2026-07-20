using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using WinContainers.Core;

namespace WinContainers.Runtime;

public sealed class WslcDriver : IDisposable
{
    private const int DefaultTimeoutMs = 30000;
    private const int SlowTimeoutMs = 120000;
    private const int MaxKeepAliveFailures = 5;
    private static readonly TimeSpan KeepAliveFailureWindow = TimeSpan.FromMinutes(5);

    private readonly object _keepAliveLock = new();
    private Process? _keepAliveProcess;
    private bool _disposed;
    private int _keepAliveFailureCount;
    private DateTime _keepAliveStartTime;

    public WslcDriver()
    {
        AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;

        try
        {
            StartKeepAliveProcess();
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Keep-alive process failed to start: {ex}");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct)
    {
        try
        {
            var result = await RunAsync("--version", DefaultTimeoutMs, ct);
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

    public Task<string> RunContainerAsync(string image, string? name = null, IEnumerable<string>? ports = null, IEnumerable<string>? volumes = null, IEnumerable<string>? env = null, string? restart = null, CancellationToken ct = default) =>
        RunAndCaptureAsync(WslcCommands.Run(image, name, ports, volumes, env, restart), DefaultTimeoutMs, ct);

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
            return new RunResult(-1, string.Empty, $"Command timed out after {timeoutMs}ms.");
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

    public void Dispose()
    {
        lock (_keepAliveLock)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            StopKeepAliveProcessCore();
        }

        GC.SuppressFinalize(this);
    }

    private void OnProcessExit(object? sender, EventArgs e) => Dispose();

    private void OnDomainUnload(object? sender, EventArgs e) => Dispose();

    private void StartKeepAliveProcess()
    {
        lock (_keepAliveLock)
        {
            StartKeepAliveProcessLocked();
        }
    }

    private void StartKeepAliveProcessLocked()
    {
        if (_disposed)
        {
            return;
        }

        StopKeepAliveProcessCore();

        var process = new Process
        {
            StartInfo = new ProcessStartInfo("wsl.exe", "-u root --exec sleep infinity")
            {
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        process.Exited += OnKeepAliveExited;
        _keepAliveStartTime = DateTime.UtcNow;
        process.Start();
        _keepAliveProcess = process;
        Trace.WriteLine($"[WslcDriver] Keep-alive process started (pid {process.Id}).");
    }

    private void OnKeepAliveExited(object? sender, EventArgs e)
    {
        try
        {
            var process = sender as Process;
            if (process is not null)
            {
                process.Exited -= OnKeepAliveExited;
                process.Dispose();
            }

            TimeSpan delay;
            bool shouldRestart;
            lock (_keepAliveLock)
            {
                if (_keepAliveProcess != process || _disposed)
                {
                    return;
                }

                _keepAliveProcess = null;

                var lifetime = DateTime.UtcNow - _keepAliveStartTime;
                if (lifetime > TimeSpan.FromSeconds(30))
                {
                    _keepAliveFailureCount = 0;
                }
                else
                {
                    _keepAliveFailureCount++;
                }

                if (_keepAliveFailureCount > MaxKeepAliveFailures)
                {
                    Trace.WriteLine("[WslcDriver] Keep-alive process failed repeatedly; stopping restart.");
                    return;
                }

                var seconds = Math.Min(30, 2 * Math.Pow(2, _keepAliveFailureCount));
                delay = TimeSpan.FromSeconds(seconds);
                shouldRestart = true;
            }

            if (shouldRestart)
            {
                _ = Task.Run(async () =>
                {
                    await Task.Delay(delay);
                    lock (_keepAliveLock)
                    {
                        if (!_disposed)
                        {
                            StartKeepAliveProcessLocked();
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Keep-alive restart failed: {ex}");
        }
    }

    private void StopKeepAliveProcess()
    {
        lock (_keepAliveLock)
        {
            StopKeepAliveProcessCore();
        }
    }

    private void StopKeepAliveProcessCore()
    {
        var process = _keepAliveProcess;
        _keepAliveProcess = null;

        if (process is null)
        {
            return;
        }

        try
        {
            process.Exited -= OnKeepAliveExited;
            if (!process.HasExited)
            {
                TryKill(process);
            }
        }
        catch (Exception ex)
        {
            Trace.WriteLine($"[WslcDriver] Stop keep-alive failed: {ex}");
        }
        finally
        {
            process.Dispose();
        }
    }

    private static ProcessStartInfo BuildStartInfo(string arguments)
    {
        return new ProcessStartInfo("cmd.exe", $"/c wslc {arguments}")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
    }

    private readonly record struct RunResult(int ExitCode, string Stdout, string Stderr);
}
