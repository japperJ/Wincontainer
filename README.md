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

## Runtime

WinContainers uses `wslc.exe` as its container runtime. It does not bundle Docker Desktop or use Docker Desktop binaries. Container commands run through the local WSLC runtime and the app communicates with its in-process local service.

## Open Source

WinContainers is free and open source under the [MIT License](LICENSE). It is a focused local desktop utility, not a hosted service or paid SaaS product.

## Author

Created by [Jan Petersen](https://github.com/japperj).
