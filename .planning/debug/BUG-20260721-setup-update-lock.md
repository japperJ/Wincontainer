# Bug Investigation: Setup Update Fails While App Is Running

## Summary
Velopack setup failed with `Access denied` while renaming `%LOCALAPPDATA%\WinContainers`. The first bootstrapper fix closed `WinContainers.App.exe` but did not release the installed `current` directory.

## Symptoms Observed
- Setup versions `0.0.73` and `0.0.74` failed while the app had been running.
- A Windows restart made setup work.
- The first bootstrapper version still failed in a live test even though `WinContainers.App.exe` had exited.

## Root Cause
`WslcDriver` started persistent `wsl.exe -u root --exec sleep infinity` keep-alive processes without setting `WorkingDirectory`. They inherited the installed app's `current` directory. Some stale keep-alive processes survived previous app exits and retained a directory handle, preventing Velopack from renaming `current` and the installation root.

## Hypothesis Testing
- Hypothesis 1: The user was still testing the old installer. **Rejected.** A live test with the new `0.0.76` payload reproduced the same failure.
- Hypothesis 2: The bootstrapper could not identify the installed app path. **Rejected.** It found and closed `...\WinContainers\current\WinContainers.App.exe`.
- Hypothesis 3: A stale WSLC keep-alive process held the directory. **Accepted.** `wsl.exe -u root --exec sleep infinity` processes were present after the app exited; stopping them made `current` renameable immediately.

## Fix Applied
- Set `WorkingDirectory` to `%TEMP%` for all WSLC and WSL keep-alive processes.
- Updated the installer bootstrapper to stop legacy WinContainers keep-alive processes before invoking Velopack setup.
- Rebuilt the single-file installer as release `0.0.77`.

## Verification
- Unit tests: 33 passed.
- Debug build: passed with 0 warnings/errors.
- Release `0.0.77`: built successfully.
- Live update test: started the installed app, ran the new setup silently, and received exit code `0`.
- Installer log confirmed successful root rename and extraction.
- Post-update `current` directory rename probe succeeded.
- Regression test with the newly installed build: detected 2 keep-alive processes before setup, 0 afterward, setup exit code `0`, and `current` remained renameable.
