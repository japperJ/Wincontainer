# Bug Investigation: WSLC Container Refresh Timeout

## Summary

Container refresh hangs after the WSLC migration and eventually reports:

`The request was canceled due to the configured HttpClient.Timeout of 100 seconds elapsing.`

## Symptoms Observed

- `wslc --version` succeeds.
- The app reports `All prerequisites met!`.
- `GET /api/containers` does not complete.
- Template cache read/write warnings appear, but those exceptions are caught and do not stop the app.

## Root Cause

The WSL VM cannot be created on this machine. Direct WSL diagnostics report:

`Wsl/Service/CreateInstance/CreateVm/HCS/ERROR_FILE_NOT_FOUND`

The Ubuntu WSL distribution remains stopped. Consequently, WSLC container commands such as:

`wslc container list --all --format json`

hang instead of returning container data. The same hang occurs when the command is started through the old `cmd.exe /c wslc ...` path, so changing from the shell alias to the resolved executable is not the underlying cause.

The app's prerequisite check validates only `wslc --version`, which checks that the CLI binary exists but does not check that WSL/WSLC can start its runtime. The API then waits on the driver command (120 seconds), while the UI client gives up at 100 seconds.

## Hypothesis Testing

- Direct executable resolution is selecting the wrong binary: **REJECTED**. `Get-Command wslc` and the resolver both identify `C:\Program Files\WSL\wslc.exe`; `wslc --version` returns `wslc 2.9.4.0`.
- The new direct process invocation changed WSLC behavior: **REJECTED**. The exact container command also hangs through `cmd.exe /c wslc ...`.
- The WSL runtime is unavailable even though the CLI is installed: **ACCEPTED**. `wsl --list --verbose` shows Ubuntu stopped, and starting it reports the HCS `ERROR_FILE_NOT_FOUND` VM creation failure.

## Fix Applied

No runtime repair was applied in source. This is an environment/WSL installation failure, with an application readiness-check defect that allows it to surface as a misleading HTTP timeout.

## Verification

- `wslc --version`: succeeds.
- `wslc container list --all --format json`: times out outside the app.
- `wslc images`: times out outside the app.
- `wsl --list --verbose`: all listed distributions are stopped.
- `wsl.exe -u root --exec sleep infinity`: fails with `CreateVm/HCS/ERROR_FILE_NOT_FOUND`.

## Recommended Remediation

1. Repair or reinstall the WSL/Virtual Machine Platform installation and verify that the Ubuntu distribution starts with `wsl -d Ubuntu`.
2. Verify `wslc container list --all --format json` completes before launching the app.
3. Change onboarding/health checks to execute a bounded WSLC data command, not only `wslc --version`.
4. Align the API/client timeout behavior so runtime failures are returned as a diagnostic error rather than a 100-second cancellation.
