# Bug Investigation: Windows Restart Changes Container Inventory and Ports

## Summary

After Windows restart, the app could show a different container name and no
ports. Starting a stopped container can temporarily show no ports because WSLC
does not report bindings while the container is stopped.

## Symptoms Observed

- `wslc container ps --all --format json` returned containers with `State: 3`
  and `Ports: []`.
- Repeated repair/update activity appeared to restore the expected inventory.
- Containers created in another WSLC session were not the same containers shown
  by the app.

## Root Cause

The previous `WslcDriver` started a persistent `wsl.exe -u root --exec sleep
infinity` process. That changed WSLC's default session selection. WSLC stores
container metadata per session, so the app and a normal user-launched `wslc.exe`
could list different containers. Repair/update restarted the runtime and made
the discrepancy appear fixed.

WSLC 2.9.4.0 also reports `Ports: []` while a container is stopped. After a
successful `container start`, the port binding is reported again. This is
runtime output, not a parser loss.

The issue remained reproducible because the running installed app had a full
administrator token. Its API returned the `wslc-cli-admin-jptrs` session while
the normal user session was `wslc-cli-jptrs`. A Windows restart or elevated
repair changed which session was active/selected, making the inventory appear
to change.

## Hypothesis Testing

- Hypothesis 1: WSLC deletes or renames containers during Windows restart.
  **Rejected.** Direct `wslc.exe` retained the container and its name.
- Hypothesis 2: The parser drops port mappings after restart. **Rejected.** A
  direct stop/start test returned `Ports: []` while stopped and the structured
  port mapping again after start; the parser already handles that structure.
- Hypothesis 3: The app and direct CLI use different WSLC sessions.
  **Accepted.** The old keep-alive bootstrap created separate session stores,
  and the installed app was still running elevated during the reproduction.

## Fix Applied

- Removed the persistent WSL keep-alive process from `WslcDriver`.
- Declared the WinContainers app manifest as `asInvoker` so normal app launches
  do not create or select an administrator WSLC session.
- WSLC commands now use the direct resolved `wslc.exe` process without creating
  a second session.
- Kept structured port parsing and numeric state parsing unchanged.

## Verification

- Direct WSLC stop/start reproduction confirmed port bindings disappear only
  while stopped and return after start.
- Unit tests: 34 passed.
- `git diff --check`: no whitespace errors.
- The temporary verification container was removed after testing.
