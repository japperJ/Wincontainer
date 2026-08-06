# AI Assistant Demo Script

This script walks through the WinContainers AI assistant feature. It is designed
for a live demo on a clean machine. Each step shows the prompt to type and what
to expect.

## Before You Start

1. Build and run the app:
   ```powershell
   dotnet build WinContainers.slnx -c Debug --nologo -v q
   dotnet publish src/WinContainers.App/WinContainers.App.csproj `
     -c Debug -r win-x64 --self-contained `
     -p:PublishTrimmed=false -o publish/WinContainers --nologo -v q
   Start-Process -FilePath publish/WinContainers/WinContainers.App.exe
   ```
2. Open the **AI Assistant** page from the left navigation.
3. In the setup dialog choose **Local Ollama**, then click **Install Ollama container**.
   Wait for the status text: *"Ollama installed. You can now chat with the local model."*
   The first install pulls `ollama/ollama` and `qwen2.5:3b`, which can take a few minutes.

> Tip: If Ollama is already installed on the machine, click **Detect local Ollama**
> instead — it will find the server and skip installation.

---

## Prompt 1 — Ollama Setup (if not done above)

**Prompt:**
```
What model are you running and is the server healthy?
```

**Expected:**
- The assistant lists the local model (`qwen2.5:3b`) from the container state snapshot.
- No tool step card appears (it answers from the snapshot in the system prompt).

---

## Prompt 2 — Start a Web Stack

**Prompt:**
```
Run an nginx container named web with port 8080 mapped to 80, then tell me its status.
```

**Expected:**
- A step card shows: *"Run container from image 'nginx:latest'"*.
- A second step card appears when it checks the container list.
- The reply confirms the container is running and explains how to reach it:
  `http://localhost:8080`.

**Follow-up (optional, shows compose generation):**
```
Write a docker-compose file for nginx + redis and save it.
```

**Expected:**
- A step card shows: *"Save compose file 'web-stack'"*.
- The reply includes the file path under `Documents\WinContainers\compose`.

---

## Prompt 3 — Diagnose a Crash

First, make the container look unhealthy:

```powershell
wslc container exec web -- sh -c "kill 1"
```

Wait a few seconds, then ask:

**Prompt:**
```
Why did my web container stop? Check its logs and status, then start it again.
```

**Expected:**
- Step cards appear for getting logs, listing containers, and starting the container.
- The reply explains the cause (process killed) and confirms the container is running again.

---

## Prompt 4 — Destructive Confirmation (Safety Gate)

**Prompt:**
```
Remove the nginx container named web.
```

**Expected:**
- A confirmation dialog appears: *"The AI assistant wants to run this action. It cannot be undone."*
  showing *"Remove container 'web'"*.
- Choose **Deny** the first time. The assistant says it will not remove it and the
  container still exists.
- Ask again and choose **Allow**. The container is removed.

---

## Prompt 5 — Offline Privacy (Optional)

Show the settings page and point out:

- The provider shows **Ollama (local)** — no cloud service involved.
- API key field is empty for Ollama.
- Conversations are stored only under `%LOCALAPPDATA%\WinContainers\chats`.

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| "No local Ollama server found" | Click **Install Ollama container**. WSL/WSLC must be available. |
| Installation fails on `pull` | The container may still be starting. Click **Detect local Ollama** again after a few seconds. |
| Model never loads on first ask | The first reply can be slow on a CPU-only machine while `qwen2.5:3b` loads. Wait a few seconds. |
| Assistant refuses an action | It is the safety policy. Rephrase or click **Allow** in the confirmation dialog. |
