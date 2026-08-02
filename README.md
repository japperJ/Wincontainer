# WinContainers

![WinContainers](src/WinContainers.App/Assets/Wide310x150Logo.scale-200.png)

A focused Windows desktop manager for containers running through Microsoft's WSL Containers runtime (WSLC). WinContainers gives developers a small, native WinUI 3 interface for viewing and operating containers without Docker Desktop.

## What It Does

- View, start, stop, restart, and remove containers.
- Pull, inspect, and remove container images.
- Manage volumes and networks.
- Open container logs and an interactive terminal.
- Detect WSL2, virtualization, and WSLC prerequisites during onboarding.
- Install or update WSLC from Microsoft's official WSL releases.
- Check for Stable or Beta application updates.
- Run as a portable app or install with the Windows setup executable.

## Requirements

- Windows 11.
- WSL2 and virtualization enabled.
- WSLC (`wslc.exe`) for container operations.
- Administrator approval when installing WSL2 or WSLC.

The onboarding screen checks prerequisites and provides installation actions where possible.

## Download

Download the latest installer or portable ZIP from [GitHub Releases](https://github.com/japperJ/Wincontainer/releases).

The installed application lives under `%LOCALAPPDATA%\WinContainers`. Portable builds can be extracted to any folder.

## Build From Source

```powershell
dotnet build WinContainers.slnx -c Debug --nologo -v q
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q
```

Publish a self-contained Windows build:

```powershell
dotnet publish src/WinContainers.App/WinContainers.App.csproj `
  -c Debug -r win-x64 --self-contained `
  -p:PublishTrimmed=false -o publish/WinContainers --nologo -v q
```

## MCP Server

WinContainers exposes a built-in [Model Context Protocol (MCP)](https://modelcontextprotocol.io) server so that AI coding assistants (GitHub Copilot, Cursor, Claude, etc.) can manage containers, images, volumes, and networks directly from the editor.

### Endpoint

The MCP server runs on `http://localhost:5123/mcp` (HTTP/SSE transport) as part of the in-process Kestrel service that starts with the app.

The port and token can be overridden with environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `WINCONTAINERS_SERVICE_PORT` | `5123` | Port the service listens on |
| `WINCONTAINERS_SERVICE_TOKEN` | *(none)* | ****** required for remote connections |

When no token is set the service only accepts loopback connections. When a token is set it also accepts connections from other hosts and requires `Authorization: ******

### Connecting an AI Client

Add the server to your AI client's MCP configuration. A ready-made `.github/copilot-mcp.json` is included in this repository for GitHub Copilot:

```json
{
  "mcpServers": {
    "wincontainer": {
      "description": "Wincontainer container management — run, stop, inspect containers, images, volumes, and networks via wslc.",
      "url": "http://localhost:5123/mcp",
      "headers": {
        "Authorization": "******"
      }
    }
  }
}
```

Omit the `headers` block when connecting from localhost without a token configured.

### Available Tools

| Tool | Description |
|---|---|
| `ListContainers` | List all containers |
| `RunContainer` | Run a new container from an image |
| `StartContainer` | Start a stopped container |
| `StopContainer` | Stop a running container |
| `RestartContainer` | Restart a container |
| `RenameContainer` | Rename a container |
| `RemoveContainer` | Delete a container |
| `InspectContainer` | Get detailed container configuration and status |
| `ExecCommand` | Execute a command inside a running container |
| `GetContainerLogs` | Retrieve recent container logs |
| `ListImages` | List downloaded images |
| `PullImage` | Pull an image from a registry |
| `RemoveImage` | Delete an image |
| `InspectImage` | Get detailed image metadata |
| `ListVolumes` | List storage volumes |
| `CreateVolume` | Create a volume |
| `RemoveVolume` | Delete a volume |
| `InspectVolume` | Get detailed volume information |
| `ListNetworks` | List container networks |
| `CreateNetwork` | Create a network |
| `RemoveNetwork` | Delete a network |
| `HealthCheck` | Check whether the wslc runtime is available |
| `GetVersion` | Get the wslc runtime version |

## Runtime

WinContainers uses `wslc.exe` as its container runtime. It does not bundle Docker Desktop or use Docker Desktop binaries. Container commands run through the local WSLC runtime and the app communicates with its in-process local service.

## Open Source

WinContainers is free and open source under the [MIT License](LICENSE). It is a focused local desktop utility, not a hosted service or paid SaaS product.

## Author

Created by [Jan Petersen](https://github.com/japperj).
