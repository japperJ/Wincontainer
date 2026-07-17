# Build & Launch

- **Kill before rebuild**: `taskkill /f /pid <PID>`. The published EXE (`WinContainers.App`) is locked while running.
- **Rebuild after any code change**:
  ```
  dotnet build -c Debug --nologo -v q
  dotnet publish "src/WinContainers.App/WinContainers.App.csproj" -c Debug -r win-x64 --self-contained -p:PublishTrimmed=false -o "publish/WinContainers" --nologo -v q
  ```
  Running stale publish yields stale UI.
- **Launch**:
  ```
  Start-Process -FilePath "publish/WinContainers/WinContainers.App.exe"
  ```

# Architecture

- **Single .exe**: `WinContainers.App.exe` runs in-process Kestrel on a background thread. No separate `WinContainers.Service.exe`.
- **WSLC-only**: All container operations call `wslc.exe` directly via `WslcDriver` (CLI `Process.Start`, not C#/WinRT SDK).
- **PowerShell layer removed** — no `ScriptManifest.json`, `.ps1` files, `ScriptProvider`, or `RuntimeDriver`.
- **Nerdctl/LXC removed** — no `RuntimeType` enum, `IRuntimeDriver`, dual-runtime branching.
- **Projects**: `Core` (shared commands/models) → `Runtime` (`WslcDriver`, `WslcContainerParser`, models) → `Service` (Kestrel REST API) → `App` (WinUI UI + in-process Kestrel host).

# DataTemplate Command Bindings

- **Do NOT use `{Binding ElementName=..., Path=DataContext.Command}` or `Command="{Binding Command}"` in DataTemplates** — the binding source doesn't reach the ViewModel from within nested DataContexts.
- **Use `Click` handlers in code-behind** instead: cast `sender` to `Button`, read `DataContext` for the model, then call `_viewModel.MethodAsync(...)`. See `StartContainer_Click` / `StopGroup_Click` in `ContainersControl.xaml.cs`.
- Property-only bindings (text, visibility, enabled) can safely use `{x:Bind ModelProperty}` with `x:DataType` on the template.

# Project Conventions

- `ContainerCardData` extends `ObservableObject` for reactive `Status` → `CanRemove` notifications.
- `ContainerItems` is a reference-preserving `ObservableCollection<object>` — use `Clear()`+`Add()` instead of reassignment to preserve scroll position.
- Poll interval is `BackgroundPollIntervalMs = 10000` in `ContainersViewModel`.

# Build Notes

## Model Location
- All model classes (`ContainerCardData`, `ImageEntryData`, `FileEntryData`, `TerminalCommand`, etc.) live in **`WinContainers.Runtime\Models`** at namespace `WinContainers.Runtime.Models`.
- Both `WinContainers.App` and `WinContainers.Service` reference `WinContainers.Runtime` and share these models.
- `WinContainers.Runtime` has a package reference to `CommunityToolkit.Mvvm` (same centrally-managed version as App) to support `ObservableObject` models.

## Pre-existing Build Issues
- XAML compiler WMC9999 error (`Could not find any resources appropriate...`) is a WinUI SDK tooling issue in `Microsoft.WindowsAppSDK.WinUI 2.1.0` — not related to code changes.

# WSLC Migration Summary

- **New**: `WinContainers.Runtime` project — `WslcDriver.cs`, `WslcContainerParser.cs`, `RuntimeTools.cs`, models moved here from App.
- **New**: `WinContainers.Core/WslcCommands.cs` — all `wslc.exe` CLI command definitions as static methods.
- **Removed**: `WinContainers.Scripts/` directory (25+ .ps1 files, ScriptManifest.json, ScriptProvider.cs, LXC/).
- **Removed**: `IRuntimeDriver.cs`, `RuntimeType.cs`, `ServiceHostStarter.cs`, `NerdctlContainerParser.cs`, `LxcContainerParser.cs`, `IContainerParser.cs`, `RuntimeConverters.cs`, `ImageListFormatter.cs` (moved to Core).
- **Rewritten**: `App/App.xaml.cs` — in-process Kestrel startup, DI for `WslcDriver` + `WslcServiceClient`.
- **Rewritten**: `Service/Host/ServiceHost.cs` — clean REST endpoints, no nerdctl/LXC branching.
- **Rewritten**: All ViewModels — use `App.ServiceClient.XXXAsync()` instead of `ServiceHostStarter.RunScriptAsync()`.
- **Updated**: `.slnx` — replaced Scripts with Runtime reference.
- **Updated**: `Directory.Packages.props` — removed `Microsoft.PowerShell.SDK` package version.

# Release Build

- **One-time setup**: Generate self-signed cert: `pwsh tools/generate-cert.ps1`
- **Build installer**: `pwsh tools/build-release.ps1 -Version 1.0.0`
- **Output**: `publish\win-x64\` (self-contained folder) + `release\WinContainers-stable-Setup.exe` (Velopack installer)
- **Install location**: `%LOCALAPPDATA%\Velopack\` (per-user)
- **Auto-update**: Checks GitHub Releases on startup via Velopack
- **Prerequisites**: Onboarding wizard checks WSL2 + WSLC, provides one-click install buttons
- **First-run marker**: `%LOCALAPPDATA%\WinContainers\.first-run-complete`

# Installer Architecture

- **Velopack** handles: install, update, uninstall, Start Menu shortcuts
- **Self-contained**: No .NET runtime needed on target machine; Velopack packages the complete publish folder
- **Untrimmed**: WinUI 3 + Windows App SDK break under trimming
- **Self-signed**: Certificate generated with `New-SelfSignedCertificate`, stored locally (not in git)
- **Prerequisite scripts**: `tools/prereqs/*.ps1` embedded as resources, extracted on first run
