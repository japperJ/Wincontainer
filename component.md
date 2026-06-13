# WinContainers — Components & Modules

## 1. Solution Layout

| Project | Purpose |
|---|---|
| `WinContainers.App` | WinUI 3 GUI — single self-contained `.exe` |
| `WinContainers.Core` | Models, interfaces (`IRuntimeDriver`, `IContainerStateProvider`, `IAppStore`) |
| `WinContainers.Service` | ASP.NET Core minimal API (in-process Kestrel), PSRunner, StreamHostRegistry |
| `WinContainers.Scripts` | Embedded PowerShell scripts + `ScriptManifest.json` |
| `WinContainers.Tests.Unit` | xUnit unit tests |
| `WinContainers.Tests.Integration` | Integration tests (`[Trait("wslc", "required")]`, skips cleanly) |
| `WinContainers.Tests.Playwright` | WebView2 terminal pane UI tests |

## 2. Runtime & Container Layer

| Module | Role |
|---|---|
| **WSLC** (`wslc.exe`) | Microsoft WSL Containers CLI — primary runtime driver |
| **wslcsession.exe** | Per-user Windows service that hosts the WSLC runtime |
| **WSL2** | Linux kernel hosting layer for WSLC containers |

## 3. PowerShell Scripting Layer

| Component | Execution mode | Description |
|---|---|---|
| `PowerShell.Create()` | One-shot (~50 ms) | Used for list/start/stop/pull/inspect/create calls |
| `pwsh.exe` child process | Streaming (ConPTY) | Used for logs `-f`, exec sessions, and the interactive terminal |
| `ScriptProvider` | Startup | Materializes embedded `.ps1` resources to `%APPDATA%\WinContainers\Scripts\` on first run |
| `StreamHostRegistry` | Runtime | Tracks long-lived `pwsh.exe` processes; supports cancellation |
| `ScriptManifest.json` | Configuration | Maps script name → type (one-shot/stream) → `wslc` command with timeout |

**Scripts managed:**

| Script | Type | `wslc` command | Timeout |
|---|---|---|---|
| `Get-Container` | one-shot | `wslc container ps --format json` | 30 s |
| `Start-Container` | one-shot | `wslc container start {id}` | 30 s |
| `Stop-Container` | one-shot | `wslc container stop {id}` | 30 s |
| `Restart-Container` | one-shot | `wslc container restart {id}` | 30 s |
| `Remove-Container` | one-shot | `wslc container rm {id}` | 30 s |
| `Get-Image` | one-shot | `wslc image ls --format json` | 30 s |
| `Pull-Image` | stream | `wslc image pull {image}` | 30 min |
| `Remove-Image` | one-shot | `wslc image rm {id}` | 30 s |
| `Get-Volume` | one-shot | `wslc volume ls --format json` | 30 s |
| `New-Volume` | one-shot | `wslc volume create {name}` | 30 s |
| `Remove-Volume` | one-shot | `wslc volume rm {name}` | 30 s |
| `Get-Network` | one-shot | `wslc network ls --format json` | 30 s |
| `New-Network` | one-shot | `wslc network create {name}` | 30 s |
| `Remove-Network` | one-shot | `wslc network rm {name}` | 30 s |
| `Get-ContainerLogs` | one-shot | `wslc logs --tail {tail} {id}` | 30 s |
| `Start-ContainerLogsStream` | stream | `wslc logs -f {id}` | — |
| `Exec-Container` | stream | `wslc exec -it {id} pwsh` | — |
| `Login-Registry` | one-shot | `wslc login {host} --username {u} --password-stdin` | 30 s |
| `Verify-Wslc` | one-shot | `wslc --version` | 10 s |

## 4. GUI Layer (WinUI 3)

### Screens / Pages

| Screen | Feature | Description |
|---|---|---|
| **Onboarding wizard** | F1 | 4-card wizard detecting WSL, virtualization, Insider status, WSL Preview |
| **Dashboard** | F2 | Container list with status badges; polls `wslc container ps` every 2 s |
| **Images** | F4 | Image list + pull dialog + delete + inspect |
| **Volumes** | F7 | Volume list + create + inspect + delete |
| **Networks** | F8 | Network list + create + inspect + delete |
| **Settings** | F10 | Theme, terminal font size, registry list, startup behaviour |
| **Terminal tab** | F5, F11 | Real ConPTY-backed `pwsh.exe` via xterm.js in WebView2 |
| **Logs viewer** | F6 | Live tail + 500-line history |
| **Activity panel** | F11 | Real-time stream of every `wslc` call with status |

### WinUI 3 Controls used

| Control | Usage |
|---|---|
| `NavigationView` | Primary app shell navigation |
| `ItemsRepeater` | Virtualized container cards on Dashboard |
| `ContentDialog` | Pull image dialog, confirmations, registry login |
| `WebView2` | xterm.js terminal pane |
| `InfoBar` | Status notifications for actions |
| `ProgressBar` / `ProgressRing` | Loading/pull progress |
| `AutoSuggestBox` | Image name autocomplete in pull dialog |
| `TextBox` | Settings fields |
| `ToggleSwitch` | Settings toggles |
| `CommandBar` | Action buttons on list pages |
| `Grid` / `StackPanel` | General page layout |
| `Border` | Card styling |
| `AcrylicBrush` / `MicaBackdrop` | Fluent Design background |

## 5. Service Layer (In-process ASP.NET Core)

| Component | Description |
|---|---|
| **Kestrel** | HTTP server bound to `127.0.0.1:<random port>` |
| **Bearer token auth** | 128-bit token generated per launch; written to `%LOCALAPPDATA%\WinContainers\service.token` |
| **PSRunner** | Abstracts `PowerShell.Create()` one-shots and `pwsh.exe` streaming processes |
| **StreamHostRegistry** | In-memory registry of active streaming processes; supports `Cancel(streamId)` |
| **ScriptProvider** | Reads `ScriptManifest.json`, resolves embedded `.ps1` resources |
| **Health endpoint** | `GET /api/health` → `{ wslcOk, wslcVersion, appVersion }` |

### REST API endpoints

| Method | Path | Purpose |
|---|---|---|
| `GET` | `/api/containers` | List containers |
| `POST` | `/api/containers/{id}/start` | Start container |
| `POST` | `/api/containers/{id}/stop` | Stop container |
| `POST` | `/api/containers/{id}/restart` | Restart container |
| `DELETE` | `/api/containers/{id}` | Remove container |
| `POST` | `/api/containers/{id}/exec` | Spawn exec session → `{ streamId }` |
| `GET` | `/api/containers/{id}/logs?tail=500` | Get log lines |
| `GET` | `/api/containers/{id}/logs/stream` | WS live log tail |
| `GET` | `/api/images` | List images |
| `POST` | `/api/images/pull` | Pull image → stream |
| `DELETE` | `/api/images/{id}` | Remove image |
| `GET` | `/api/volumes` | List volumes |
| `POST` | `/api/volumes` | Create volume |
| `DELETE` | `/api/volumes/{name}` | Remove volume |
| `GET` | `/api/networks` | List networks |
| `POST` | `/api/networks` | Create network |
| `DELETE` | `/api/networks/{name}` | Remove network |
| `GET` | `/api/registries` | List configured registries |
| `POST` | `/api/registries` | Add registry (secret → Credential Manager) |
| `DELETE` | `/api/registries/{host}` | Remove registry |
| `GET` | `/api/settings` | Get app settings |
| `PUT` | `/api/settings` | Update app settings |
| `GET` | `/api/health` | Health check |

### WebSocket endpoints

| Path | Direction | Messages |
|---|---|---|
| `/ws/events` | server → client | `container.snapshot` every 2 s + `container.event` on state change |
| `/ws/terminal` | bidirectional | `terminal.in`, `terminal.out`, `terminal.resize` |
| `/ws/stream/{streamId}` | server → client | `stream.line`, `stream.closed` |

## 6. Persistence

| Component | Technology | Stores |
|---|---|---|
| **IAppStore** | `System.Text.Json` → `%APPDATA%\WinContainers\*.json` | App settings, registry metadata |
| **Windows Credential Manager** | `CredentialManagement` NuGet | Registry passwords/tokens |
| **Service port/token file** | Plain text (ACL'd) | `%LOCALAPPDATA%\WinContainers\service.{port,token}` |
| **Logs** | Serilog rolling file | `%LOCALAPPDATA%\WinContainers\logs\app-YYYYMMDD.log` |

## 7. NuGet Packages

| Package | Version | Used by |
|---|---|---|
| `Microsoft.WindowsAppSDK` | 1.8+ | App (WinUI 3) |
| `Microsoft.PowerShell.SDK` | 7.4+ | Service (PSRunner) |
| `Pty.Net` | latest | Service (ConPTY) |
| `Serilog.AspNetCore` | latest | Service |
| `Serilog.Sinks.File` | latest | Service |
| `CredentialManagement` | latest | Service |
| `System.Text.Json` | built-in | Core / Service |
| `xunit` | latest | All test projects |
| `FluentAssertions` | latest | Unit + Integration tests |
| `NSubstitute` | latest | Unit tests |
| `Playwright` | latest | Playwright tests |
| `Velopack` | latest | App (auto-update) |

## 8. CI/CD & Distribution

| Component | Role |
|---|---|
| **GitHub Actions** (`ci.yml`) | Build, test (unit + integration + Playwright), `dotnet format --verify-no-changes` |
| **GitHub Actions** (`release.yml`) | Publish → Velopack pack → SignPath sign → `gh release create` → WinGet PR |
| **GitHub Actions** (`codeql.yml`) | Weekly C# CodeQL analysis |
| **Dependabot** (`dependabot.yml`) | Weekly NuGet + Actions updates |
| **SignPath.io** | Free OSS code signing of all released binaries |
| **Velopack** | Auto-update framework (Squirrel.Windows successor) |
| **WinGet** | Community manifest for `winget install WinContainers` |
| **GitHub Releases** | Distribution channel for signed installer + delta + portable `.exe` |

## 9. Architecture Interfaces (Core)

| Interface | Purpose |
|---|---|
| `IRuntimeDriver` | Abstraction over WSLC; swapable for `containerd+nerdctl` in future |
| `IContainerStateProvider` | Abstraction for live container state; upgrade path for future `events`/`stats` API |
| `IAppStore` | Generic JSON persistence; upgrade path for SQLite in v2 |
| `StreamHostRegistry` | Registry + lifecycle for streaming `pwsh.exe` child processes |

## 10. Architecture Patterns

| Pattern | Implementation |
|---|---|
| **MVVM** | WinUI 3 data binding with `x:Bind`, ViewModels decoupled from `Microsoft.UI.Xaml.*` |
| **DI** | `Microsoft.Extensions.DependencyInjection` via ASP.NET Core host builder |
| **In-process hosting** | ASP.NET Core `WebApplication` hosted inside WinUI 3 `App.OnLaunched` |
| **Bearer token auth** | Per-launch 128-bit random token; validated on every API request |
| **Poll + push** | Dashboard polls every 2 s; WebSocket events for real-time terminal I/O |
| **Embedded resources** | `.ps1` scripts compiled as resources; extracted on first run |
