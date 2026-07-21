# Bug Investigation: Containers Missing After Windows Restart

## Summary
Containers were not being deleted by WSLC. The application and direct `wslc.exe` commands were using different WSLC session stores.

## Symptoms Observed
- Direct `wslc container ps --all --format json` returned `heimdall`.
- The application API `GET /api/containers` returned an empty list.
- A container created through the application API appeared in the app but not in direct `wslc` output.
- WSLC stores existed separately at `%LOCALAPPDATA%\wslc\sessions\wslc-cli-jptrs` and `wslc-cli-admin-jptrs`.

## Root Cause
`WslcDriver` started `wsl.exe -u root --exec sleep infinity` as a keep-alive process. This caused the app's WSLC commands to resolve to a different default WSLC session than normal user-launched `wslc.exe` commands. The two sessions have separate `storage.vhdx` files, so each showed a different container inventory after startup/reboot.

## Hypothesis Testing
- Hypothesis 1: WSLC deletes container metadata during WSL shutdown. **Rejected.** A container created through direct `wslc` remained after `wsl --shutdown`.
- Hypothesis 2: The application parser or API refresh discarded stopped containers. **Rejected.** The API returned the app session's container, while direct `wslc` returned a different session's container.
- Hypothesis 3: The app's keep-alive bootstrap changed the default WSLC session. **Accepted.** Removing it made the application API return the same `heimdall` container as direct `wslc`, including after WSL shutdown.

## Fix Applied
- Removed the `wsl.exe` keep-alive process and its restart/cleanup lifecycle from `WslcDriver`.
- WSLC now starts and manages its own runtime session through direct `wslc.exe` calls.
- The installer bootstrapper still cleans legacy keep-alive processes left by older releases.

## Verification
- Unit tests: 33 passed.
- Debug build and publish succeeded.
- App API and direct `wslc` returned the same container inventory.
- Inventory remained visible after `wsl --shutdown`.
- Release `0.0.81` built successfully.
- Packaged `0.0.81` installed with exit code `0`, and the installed app API returned the persisted `heimdall` container.
