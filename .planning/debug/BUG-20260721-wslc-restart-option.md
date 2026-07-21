# Bug Investigation: WSLC Rejects Container Restart Option

## Summary

Creating a container with a non-default restart policy failed before the image
was processed.

## Symptoms Observed

Creating `heimdall97` produced:

`wslc error (1): Argument name was not recognized for the current command: '--restart'`

## Root Cause

`WslcCommands.Run` emitted Docker's `--restart` option for policies other than
`no`. WSLC 2.9.4.0 does not expose that option for `wslc run`.

## Hypothesis Testing

- Hypothesis 1: The installed WSLC executable was stale or incompatible. Rejected: the executable reported `wslc 2.9.4.0`, and its own `run --help` also omits `--restart`.
- Hypothesis 2: The generated command contained an option unsupported by WSLC. Accepted: running the generated command reproduced the exact error; removing `--restart` matches the documented options.

## Fix Applied

Stopped emitting `--restart` from `WslcCommands.Run`. The existing parameter
remains in the service contract, but WSLC currently cannot apply it.

## Verification

- Reproduced the original command and observed exit code 1 with the reported error.
- Added `WslcCommands_Run_ShouldNotEmitUnsupportedRestartOption`.
- Unit test suite run after the change.
