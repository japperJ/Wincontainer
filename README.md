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

When running WinContainers inside a Hyper-V virtual machine, enable nested virtualization on the Hyper-V host:

```powershell
Set-VMProcessor -VMName "yourVM" -ExposeVirtualizationExtensions $true
```

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

The MCP server runs on `http://localhost:5123/mcp` (stateful Streamable HTTP transport) as part of the in-process Kestrel service that starts with the app.

The port and token can be overridden with environment variables:

| Variable | Default | Purpose |
|---|---|---|
| `WINCONTAINERS_SERVICE_PORT` | `5123` | Port the service listens on |
| `WINCONTAINERS_SERVICE_TOKEN` | *(none)* | ****** required for remote connections |
| `WINCONTAINERS_MCP_ENABLED` | *(unset)* | Set to `0` or `false` to start with the MCP server disabled |
| `WINCONTAINERS_MCP_DESTRUCTIVE_CONFIRMATION_ENABLED` | *(unset)* | Set to `0` or `false` only for explicit automation bypass of human approval |
| `WINCONTAINERS_ALLOW_REMOTE_API` | *(unset)* | Set to `0` or `false` to start with remote API access blocked |

When no token is set the service only accepts loopback connections. When a token is set it also accepts connections from other hosts and requires `Authorization: ******

### On/Off Controls

The app Settings page provides live toggles (persisted across restarts):

| Setting | Effect |
|---|---|
| **MCP server** | When off, `/mcp` returns `404` for all clients. The in-app AI chat is not affected. |
| **MCP request logging** | When on, MCP activity (methods, tool calls, and their results) is written to the Output window. |
| **Destructive confirmation** | When on, destructive MCP tools request an in-request human Allow/Deny elicitation. Default is `true`. |
| **Allow remote API access** | When off, non-loopback `/api` requests return `403`. Localhost requests always work. |

The environment variables above apply only at startup and are useful for tests or automation; the Settings toggles are the live source of truth. `/api/health` reports the current state in the `mcpEnabled` and `apiRemoteAccessEnabled` fields.

### Destructive tool confirmation

When `McpDestructiveConfirmationEnabled` is on (default), each destructive tool requests an in-request MCP elicitation with `Allow`/`Deny` choices before it executes. The action proceeds only when the client returns `Action: "accept"` and `Content["Allow"] == "allow"`. Clients that do not support elicitation, cancellations, transport errors, and any malformed response fail closed. Approval prompts contain only safe action, target, and session information; environment values, tar data, and full mount details are never shown. Disabling the setting preserves the explicit automation bypass, but DB-special `confirmDestructive` guard rails remain enforced.

### Connecting an AI Client

Add the server to your AI client's MCP configuration. A ready-made `.github/copilot-mcp.json` is included in this repository for GitHub Copilot:

```json
{
  "mcpServers": {
    "wincontainer": {
      "description": "Wincontainer container management via wslc. Destructive tools require in-request MCP elicitation with a real human Allow/Deny decision; unsupported clients and failed elicitation fail closed.",
      "url": "http://localhost:5123/mcp",
      "headers": {
        "Authorization": "******"
      }
    }
  }
}
```

Omit the `headers` block when connecting from localhost without a token configured.

### Deployment skill

This repository includes a reusable Copilot skill for Wincontainer deployment:
[`deploying-to-wincontainer`](.github/skills/deploying-to-wincontainer/SKILL.md).
It covers the elevated WSLC session, image build and export, MCP tar loading, the
chunked upload workflow, limits, and verification. AI clients that support repository
skills can use it when working in a Wincontainer project.

### Available Tools

| Tool | Description |
|---|---|
| `ListContainers` | List all containers |
| `RunContainer` | Run a new container from an image, optionally attached to a named network |
| `StartContainer` | Start a stopped container |
| `StopContainer` | Stop a running container |
| `RestartContainer` | Restart a container |
| `RenameContainer` | Rename a container |
| `RemoveContainer` | Delete a container — requires in-request human Allow/Deny elicitation |
| `InspectContainer` | Get detailed container configuration and status |
| `ExecCommand` | Execute a command inside a running container |
| `GetContainerLogs` | Retrieve recent container logs |
| `ListImages` | List downloaded images |
| `PullImage` | Pull an image from a registry |
| `RemoveImage` | Delete an image — requires in-request human Allow/Deny elicitation |
| `InspectImage` | Get detailed image metadata |
| `ListVolumes` | List storage volumes |
| `CreateVolume` | Create a volume |
| `RemoveVolume` | Delete a volume — requires in-request human Allow/Deny elicitation |
| `InspectVolume` | Get detailed volume information |
| `ListNetworks` | List container networks |
| `CreateNetwork` | Create a network |
| `RemoveNetwork` | Delete a network — requires in-request human Allow/Deny elicitation |
| `RedeployWebOnly` | Redeploy the web container (stops, removes, and re-creates it) — requires in-request human Allow/Deny elicitation |
| `HealthCheck` | Check whether the wslc runtime is available |
| `GetVersion` | Get the wslc runtime version |
| `LoadImage` | Load a container image from a .tar file or base64-encoded tar data. Examples: `load_image(tarPath="C:\\images\\app.tar")` or `load_image(tarData="<base64 tar data>")`. Exactly one of `tarPath` or `tarData` is required. Only paths ending with `.tar` are accepted. When using `tarPath`, the path is read by the Wincontainer host (not the MCP client machine). Base64 `tarData` is limited to 512 MB after decoding. |
| `StartImageUpload` | Start a chunked image upload and return the upload ID |
| `UploadImageChunk` | Append a chunk to a chunked image upload |
| `FinishImageUpload` | Finish a chunked image upload and load it into WSLC |


Chunked image upload workflow (new):

1. start_image_upload() -> returns JSON metadata containing the `uploadId` property
2. upload_image_chunk(uploadId, sequence, base64Chunk) — pass the `uploadId` from that JSON response; send ordered, zero-based sequence chunks; each chunk MUST decode to at most 3 KB
3. finish_image_upload(uploadId) — pass the same `uploadId` to finalize and assemble the uploaded chunks; the resulting decoded image data is subject to the same 512 MB total limit

Notes:

- Chunk size limit: each decoded chunk must be 3 KB or smaller.
- Total upload limit: the sum of decoded chunks must not exceed 512 MB.
- Empty uploads are rejected before WSLC is called.
- Sequence numbering: sequences are zero-based and must be uploaded in increasing order; the server will assemble chunks by sequence.
- Expiry: an upload that is inactive for more than 15 minutes is expired and its state is discarded.
- Process-local state: upload state is kept only in the service process and is not persisted across restarts; do not rely on uploads surviving a process restart.

MCP tool names are snake_case: `start_image_upload`, `upload_image_chunk`, `finish_image_upload`. Keep upload payloads as base64-encoded chunk strings when calling the MCP tools.

## AI Assistant

WinContainers includes a built-in AI assistant that manages containers, images, volumes, and networks in natural language. It uses the same in-process runtime layer as the MCP tools, so no extra services are needed.

> **Alpha:** The AI Assistant is an early-access feature. Review each proposed action before allowing it, especially actions that remove containers, images, volumes, or networks.

### What It Does

- Answer questions about your containers, images, volumes, and networks.
- Start, stop, restart, rename, and remove containers on request.
- Pull and remove images, create and remove volumes and networks.
- Run commands inside a running container.
- Generate `docker-compose` files for multi-service setups and save them under `Documents\WinContainers\compose`.
- Show each tool action as a step card with the exact command it ran.
- Ask for confirmation before destructive actions (removing containers, images, volumes, or networks).
- Use the same WSLC runtime as the main application and MCP server.

### Providers

- **OpenAI-compatible endpoint** — works with OpenAI, Azure OpenAI, and any compatible gateway. Your API key is protected with DPAPI and stored locally.
- **Local Ollama** — fully offline. The app detects a running Ollama server or installs it as a container (`ollama/ollama`, persistent volume, port `11434`) and pulls a default model (`qwen2.5:3b`) automatically.

### First Run

Open the **AI Assistant** page from the left navigation. On first use, a setup dialog asks you to pick a provider. Choose OpenAI-compatible to enter an endpoint, model, and API key, or choose Local Ollama to detect or install it with one click. You can change these settings later on the **Settings** page.

### Privacy

- Conversations are stored only on your machine under `%LOCALAPPDATA%\WinContainers\chats`.
- Your API key never leaves the machine except when sent to the provider endpoint you configured.
- With Local Ollama, nothing leaves your machine.

### Demo Script

A ready-to-run demo is in `docs/demo-ai-assistant.md`.

## Runtime

WinContainers uses `wslc.exe` as its container runtime. It does not bundle Docker Desktop or use Docker Desktop binaries. Container commands run through the local WSLC runtime and the app communicates with its in-process local service.

## Open Source

WinContainers is free and open source under the [MIT License](LICENSE). It is a focused local desktop utility, not a hosted service or paid SaaS product.

## Author

Created by [Jan Petersen](https://github.com/japperj).
