---
name: deploying-to-wincontainer
description: Use when deploying websites, apps, or static sites as containers on Wincontainer through wslc or Wincontainer MCP, especially when containers are not visible, image tar files must be imported, or the volume limit is reached.
---

# Deploying to Wincontainer

## Overview

Wincontainer runs Windows Subsystem for Linux containers through `wslc`. The Wincontainer
MCP and UI use the elevated admin session. A normal shell can create a separate session
with a different image and container store.

**Core rule:** containers must run in the elevated admin session to appear in the
Wincontainer UI.

## Sessions

Session names are generated from the local Windows user and runtime state. Do not
hard-code a name from another installation.

| Invocation | Session name | Visible in Wincontainer |
|---|---|---|
| Elevated admin shell or MCP | The admin session name shown by WSLC | Yes |
| Non-elevated shell | The non-elevated session name shown by WSLC | No |

Check sessions:

```powershell
& 'C:\Program Files\WSL\wslc.exe' system session list
```

Use the session list from the current installation. The session used by the elevated
admin shell or Wincontainer MCP is the one whose containers appear in the UI.

## Deployment workflow

1. Build the image from a normal shell:

   ```powershell
   & 'C:\Program Files\WSL\wslc.exe' build -t my-site:latest 'C:\path\to\site'
   ```

2. Export it:

   ```powershell
   & 'C:\Program Files\WSL\wslc.exe' save my-site:latest -o "$env:TEMP\my-site.tar"
   ```

3. Load the tar and run the container in the admin session.

`wslc load` and `wslc run` do not count against the volume-mount limit. An elevated
PowerShell command can load and run the image:

```powershell
$script = "$env:TEMP\wslc-deploy.ps1"
@'
& 'C:\Program Files\WSL\wslc.exe' load -i 'C:\Users\me\AppData\Local\Temp\my-site.tar'
& 'C:\Program Files\WSL\wslc.exe' run --detach --name my-site -p 5230:80 my-site:latest
'@ | Set-Content $script
Start-Process powershell.exe -ArgumentList '-ExecutionPolicy','Bypass','-File',"`"$script`"" -Verb RunAs -Wait
```

After the image is loaded in the admin session, the MCP `run_container` tool can run it.
Pass `network="my-network"` when the container must communicate with other containers
on a named WSLC network. Create or list the network first with the network MCP tools.

## MCP image import

Use `load_image` for a tar file on the Wincontainer host:

```text
load_image(tarPath="C:\\Users\\me\\AppData\\Local\\Temp\\my-site.tar")
```

The path is read by the Wincontainer host, not the MCP client. It must exist and end
with `.tar`. `tarData` is also supported, but large base64 arguments can exceed the
Copilot tool-call JSON limit.

For large archives, use the chunked workflow:

```text
1. start_image_upload()
   -> JSON metadata containing uploadId, maxChunkBytes, and maxUploadBytes
2. upload_image_chunk(uploadId, sequence, base64Chunk)
   -> ordered, zero-based chunks; each chunk decodes to 3 KB or less
3. finish_image_upload(uploadId)
   -> assemble the tar and load it into WSLC
```

Read the `uploadId` property from the JSON returned by `start_image_upload`. Do not use
the complete JSON response as the ID.

Limits and lifecycle:

- Maximum decoded chunk: 3 KB.
- Maximum decoded upload: 512 MB.
- Chunks must use increasing zero-based sequence numbers.
- Uploads are process-local and expire after 15 minutes of inactivity.
- State is removed after completion, failure, cancellation, or expiry.
- Empty uploads are rejected before WSLC.

## Verification

Check the admin session:

```text
Wincontainer-list_containers
```

or:

```powershell
& 'C:\Program Files\WSL\wslc.exe' container list
```

Then check the published port:

```powershell
Invoke-WebRequest -Uri 'http://localhost:5230' -UseBasicParsing
```

## Common mistakes

| Problem | Fix |
|---|---|
| Site works locally but Wincontainer is empty | The container is in the non-elevated session. Load and run it in the admin session. |
| `Too many volumes have been mounted (limit: 15)` | Build in a normal shell, then use `save`, elevated `load`, and `run`. |
| MCP reports `Image not found` | Load the image into the admin session first, or use `pull_image` for a public image. |
| Host bind mount consumes the limit | Bake site files into the image with a Dockerfile. |
| Upload ID is rejected | Extract the `uploadId` property from the start response. |
