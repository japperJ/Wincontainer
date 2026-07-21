# Repository Guide

## Build and Run

- The solution targets `net10.0`; nullable reference types and warnings-as-errors are enabled in `Directory.Build.props`.
- Normal debug build: `dotnet build WinContainers.slnx -c Debug --nologo -v q`.
- Unit tests: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`.
- Integration tests: `dotnet test tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj -c Debug --nologo -v q`.
- The App project loads a custom MSBuild task from `src/BuildTasks/bin/<Configuration>/netstandard2.0`; build `src/BuildTasks/BuildTasks.csproj` first if a clean app build cannot find `WinContainers.Build.FixCulture`.
- After changing code, publish before launching the UI:
  `dotnet publish src/WinContainers.App/WinContainers.App.csproj -c Debug -r win-x64 --self-contained -p:PublishTrimmed=false -o publish/WinContainers --nologo -v q`.
- A running `publish/WinContainers/WinContainers.App.exe` locks the publish output. Kill that process before rebuilding; launch with `Start-Process -FilePath publish/WinContainers/WinContainers.App.exe`.
- Release packaging is `pwsh tools/build-release.ps1 -Version <version>`; it requires `vpk`, and ISO creation additionally requires Windows ADK `oscdimg.exe`. Run `pwsh tools/generate-cert.ps1` only when a local signing certificate is needed; `*.pfx` is ignored.

## Architecture

- `src/WinContainers.App` is the only executable. It starts `WinContainers.Service.Host.ServiceHost` in-process and hosts the WinUI UI and Kestrel API in the same process.
- `src/WinContainers.Core` contains shared commands/models; `src/WinContainers.Runtime` owns WSLC execution, parsing, and runtime models; `src/WinContainers.Service` owns the REST endpoint definitions.
- Container operations are WSLC-only and run `wslc.exe` through `WinContainers.Runtime.WslcDriver`; do not add PowerShell, nerdctl, LXC, or a second runtime abstraction.
- `WslcDriver` resolves `wslc` from PATH and starts a WSL keep-alive process. Runtime changes should preserve its temp working directory and process cleanup behavior.

## WinUI Conventions

- Commands inside `DataTemplate`s must use code-behind `Click` handlers that read the button's `DataContext` and call the ViewModel. ElementName/ordinary `Command` bindings do not reach the page ViewModel from nested template data contexts.
- Use `x:Bind` with `x:DataType` for property-only template bindings.
- `ContainersViewModel.ContainerItems` is an `ObservableCollection<object>` whose reference is used by the view; update it with `Clear()` and `Add()` rather than replacing the collection.
- `ContainerCardData` is observable so status changes can update derived UI state such as `CanRemove`; keep that notification behavior when changing models.
- Container polling is intentionally every 10 seconds (`BackgroundPollIntervalMs`); change it only with an explicit product reason.

## Tests and Changes

- Test projects are under `tests/`: Unit, Integration, Playwright, and Ui. The currently implemented focused tests are in Unit and Integration; target a project rather than running the whole solution when iterating.
- Do not overwrite unrelated working-tree changes. Generated outputs under `bin/`, `obj/`, `publish/`, and `release/` are ignored.
