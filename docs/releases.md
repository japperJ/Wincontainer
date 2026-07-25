# WinContainers Releases

## Versioning

Release versions come from SemVer Git tags. Use `v1.2.3` for Stable and
`v1.2.3-beta.1` for Beta. The tag is passed to .NET assembly metadata and
Velopack, so no separate application version should be edited.

## Local Release

Run from the repository root in PowerShell:

```powershell
.\scripts\publish-release.ps1 -Tag v1.2.3
```

The command builds the existing Velopack installer, portable ZIP, full and
delta packages. ISO files are intentionally not part of normal builds or
GitHub Releases. It creates a draft
GitHub Release by default. Add `-Publish` only after reviewing the draft.
Use `-Force` to remove a failed local output directory before rebuilding.

Beta tags create GitHub prereleases and use the Beta update channel.
Unsigned releases are supported. A local PFX may be supplied to
`tools/build-release.ps1`; signing credentials must never be committed.

## CI

Pull requests and pushes to `main` run build and unit-test CI. A `v*.*.*` tag
runs the Windows release build and uploads the generated artifacts for review;
it does not publish a GitHub Release automatically.

## Update Policy

`update-policy.json` is a public contract consumed by release tooling. It uses
schema version 1 and currently has no mandatory-update floor beyond `0.0.0`.
The app checks the selected channel silently at most once every 24 hours.
Users explicitly confirm downloads and can defer optional updates.
