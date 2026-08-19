# AI Assistant Chat Flow

This document explains what happens when a user sends a chat message to the
WinContainers app. The AI assistant lives in the `WinContainers.AI` project.

The assistant is a **tool-calling agent**. It uses **tools**, not "skills".
There is no skill system inside the app. The `skills-lock.json` file and the
`.agents` / `.claude` folders are for the developers' coding agents. They do
not run when you chat with the in-app assistant.

---

## 1. Parts of the assistant

| Part | File | Role |
|------|------|------|
| `ContainerAgent` | `WinContainers.AI/ContainerAgent.cs` | Runs one chat turn. Calls the model and the tools. |
| `AgentToolRegistry` | `WinContainers.AI/AgentToolRegistry.cs` | Builds the list of tools the model can use. |
| `ContainerSnapshotBuilder` | `WinContainers.AI/ContainerSnapshotBuilder.cs` | Reads live container and image state. |
| `ChatClientFactory` | `WinContainers.AI/ChatClientFactory.cs` | Creates the model client (OpenAI or local Ollama). |
| `AgentTextCleaner` | `WinContainers.AI/AgentTextCleaner.cs` | Cleans model output and recovers tool calls. |
| `AiChatService` | `WinContainers.App/Services/AiChatService.cs` | Builds the agent from settings. Installs Ollama. |
| `AiViewModel` | `WinContainers.App/ViewModels/AiViewModel.cs` | Drives the chat page. Streams text. Shows step cards. |

---

## 2. What happens when you send a chat

1. **You type** a message on the AI Assistant page.
2. **History is loaded.** `AiViewModel.SendAsync` loads the saved chat from disk
   (`%LOCALAPPDATA%\WinContainers\chats`) and adds your message to the screen.
3. **Agent is created.** `AiChatService.CreateAgent` reads your settings and
   builds:
   - a model client (`OpenAiCompatibleChatClientFactory` — works with OpenAI or
     local Ollama),
   - a **tool registry** (the actions the AI can do),
   - a **snapshot builder** (live container/image state).
4. **Snapshot is taken.** The app asks WSLC for the current containers and
   images. This text goes into the system prompt.
5. **One turn runs.** `ContainerAgent.RunTurnAsync`:
   - builds the **system prompt** + history + your message,
   - sends them to the model with the tools,
   - streams the text to the screen,
   - if the model calls a tool, the app runs it, shows a **step card**, and
     sends the result back to the model,
   - repeats up to **10 steps** per turn (`MaxIterations = 10`).
6. **Safety gate.** For destructive tools (remove container/image/volume/
   network), the app shows a **confirm dialog**. You choose **Allow** or
   **Deny**.
7. **Answer shown and saved.** The final text appears in the chat and the
   conversation is saved to disk.

---

## 3. The system prompt

There is **one system prompt**. It is built in `BuildSystemPrompt` inside
`ContainerAgent.cs`. It says:

- "You are WinContainers AI, an assistant built into the WinContainers desktop
  app for Windows."
- "You manage containers, images, volumes, and networks that run through the
  WSLC runtime."
- "You act through the available tools. Never invent tool output; read it from
  the tool results."
- It lists the **current container and image state** (the snapshot).

Rules for the model:

- Use a tool when an action or a lookup is needed. Do not guess container state.
- Never end a reply with a plan, a promise, or a colon. When you say you will
  do something, do it in the same reply by making the tool call.
- A reply that only announces an action (for example "Let me test ...",
  "I'll check ...") is a failure. End every reply either with the tool call you
  announced or with the final answer.
- Finish each reply with a complete sentence and a final answer. Do not leave a
  sentence unfinished.
- After an action, briefly tell the user what you did and why.
- If a tool returns an error, explain it in plain words and suggest a fix.
- When the user wants a multi-service setup, write a docker-compose file with
  the `save_compose_file` tool and tell them the file path.
- Call tools only through standard function calling. Never output DSML or other
  special markup tokens; the app removes them.
- Be concise. Do not use markdown headings. Use short paragraphs or bullet
  lists.

### Hidden nudges (not shown to the user)

The code adds small prompts to keep the model on track:

- `ContinuationPrompt` — used when a reply was cut off:
  "Your previous reply was cut off before you finished. Continue now: make the
  tool call you intended, or give your final answer."
- `NarrationContinuationPrompt` — used when the model only described an action:
  "Your previous reply described an action but made no tool call. Do not describe
  what you are about to do. Make the tool call you intended now, or give your
  final answer if you already have the information you need."

There are also fixed messages:

- `MaxStepsMessage` — shown after 10 steps with no answer.
- `NoUsableReplyMessage` — shown when the model returns no usable text.

---

## 4. The tools (the AI's hands)

The tools come from `ToolImplementations` in `AgentToolRegistry.cs`. Each tool
is a real WSLC action. The method name is the tool name. The descriptions come
from `Description` attributes.

| Tool | Purpose |
|------|---------|
| `list_containers` | List all containers. |
| `inspect_container` | Inspect a container. |
| `get_container_logs` | Get recent logs of a container. |
| `start_container` | Start a stopped container. |
| `stop_container` | Stop a running container. |
| `restart_container` | Restart a container. |
| `rename_container` | Rename a container. |
| `run_container` | Run (create and start) a container from an image. |
| `exec_command` | Run a command inside a container. |
| `pull_image` | Pull an image from a registry. |
| `list_images` | List all images. |
| `inspect_image` | Inspect an image. |
| `remove_image` | Delete an image. |
| `list_volumes` | List all volumes. |
| `create_volume` | Create a volume. |
| `inspect_volume` | Inspect a volume. |
| `remove_volume` | Delete a volume. |
| `list_networks` | List all networks. |
| `create_network` | Create a network. |
| `remove_network` | Delete a network. |
| `remove_container` | Delete a container. |
| `save_compose_file` | Write a docker-compose YAML file to disk. |

The `run_container` tool also saves a config for the container so its settings
can be reused.

### Destructive tools

These tools require user confirmation (`AgentToolRegistry.DestructiveToolNames`):

- `remove_container`
- `remove_image`
- `remove_volume`
- `remove_network`

When `ConfirmDestructiveActions` is on, the app shows a confirm dialog before
running them. If you deny, the tool result tells the model: "The user declined
this action. Explain this to the user."

---

## 5. The turn loop

`RunAttemptAsync` runs a loop of up to `MaxIterations = 10` steps:

1. Send the messages (system + history + your message) to the model with the
   tools. Only one tool call per step is allowed (`AllowMultipleToolCalls = false`).
2. If the model returns **no tool call**:
   - If there is clean text, that is the final answer.
   - If the reply was cut off or only narrated an action, the agent adds a
     continuation nudge and loops again.
   - If the reply is empty or only special tokens, the agent makes one final
     call **without tools** so the model can answer in plain text.
3. If the model returns a **tool call**:
   - Build a step card (`AgentStep`) with a human-readable preview.
   - If the tool is destructive and confirmation is on, ask the user.
   - Run the tool through `IWslcDriver`.
   - Send the tool result back to the model as a `Tool` message.
   - Loop again with the new result.
4. If the loop reaches 10 steps, the agent makes one final call without tools.
   If that gives no text, it shows `MaxStepsMessage`.

Tool output is trimmed to `MaxStepOutputChars = 8000` characters. Longer output
ends with "... (output truncated)".

---

## 6. Retry logic

A whole turn can fail because the model provider is busy or the network drops.
`ContainerAgent.RunTurnAsync` wraps each attempt in a retry loop.

```
_maxAttempts      = 3   (initial call + 2 retries)
_retryDelaySeconds = 10  (wait between retries)
```

How it works:

1. `RunTurnAsync` copies the base history and runs `RunAttemptAsync`.
2. If the attempt throws a **retryable** error (`AgentErrorClassifier.IsRetryable`),
   and this is not the last attempt:
   - the agent calls `OnRetryWaitAsync` (shows a countdown in the chat),
   - then waits `retryDelaySeconds` (default 10 s),
   - then resets the history to the base and tries again.
3. If all attempts fail, the turn throws.

The chat page shows the retry to the user. `AiViewModel.ShowRetryWaitAsync`
removes the partial step cards and streaming text, then shows a message such as:

- "The provider is busy. Waiting 5 seconds before retry (attempt 2 of 3)..."
- "Retrying now (attempt 2 of 3)..."

This way a brief provider outage does not lose the whole conversation.

---

## 7. Streaming text cleaning

Some models do not use standard function calling. They put tool calls inside
special markup tokens. The app cleans this and recovers the tool calls.

The tokens look like this:

```
<｜DSML｜tool_call_start｜>{"name":"start_container","arguments":{"id":"web"}}<｜DSML｜tool_call_end｜>
```

`AgentTextCleaner` handles this:

- **`StripSpecialTokens`** — removes all DSML tokens from a finished reply. Used
  before the final text is shown.
- **`SanitizeStreaming`** — removes only *complete* DSML blocks while the text is
  still streaming. The token disappears as soon as its closing tag arrives, even
  if it spanned several streamed pieces.
- **`ExtractToolCalls`** — reads DSML blocks and turns them into tool calls.
  Blocks that cannot be parsed are dropped (counted as `droppedBlocks`) so the
  agent can detect a model that tried but failed.
- **`HasUnclosedToolCallMarker`** — true when a reply has a start marker but no
  end marker. This means the reply was cut off mid-tool-call.
- **`IsNarrationOnlyIncomplete`** — true when a reply has no tool call but only
  announces an action (for example "Let me test ..."). This is not a final
  answer.

### How the agent uses this

In `GetAssistantTurnAsync`, the agent:

1. Streams the model text and tool calls.
2. Recovers any DSML tool calls from the text so the turn can continue.
3. Sets an `interrupted` flag when:
   - the stream was truncated (`Length` or `ContentFilter` finish reason),
   - a DSML tool call was left unclosed (`HasUnclosedToolCallMarker`), or
   - the reply was narration only (`IsNarrationOnlyIncomplete`).

When the reply is interrupted, the agent adds a continuation nudge and loops
again. This stops the model from ending the turn with "Let me test ..." and no
action.

In the UI, `AiViewModel.AppendStreamingDelta` uses `SanitizeStreaming` on every
delta so the user never sees raw DSML tokens. `FinishTurn` uses
`StripSpecialTokens` on the final text.

---

## 8. Model and privacy

- Default local model: `qwen2.5:3b` on Ollama (runs in a container, no cloud).
- You can also use any OpenAI-compatible endpoint.
- On first setup the app can pull `ollama/ollama` and the default model into a
  container for you (`AiChatService.InstallOllamaAsync`).
- Conversations are stored only on your machine under
  `%LOCALAPPDATA%\WinContainers\chats`.

---

## 9. Skills

**There are no skills inside the app.** The assistant works only with the tools
in section 4 and the one system prompt in section 3. The `skills-lock.json` at
the repo root lists skills for **coding help** (for example `winui3-full-skill`,
`grilling`). Those help the developers write code. They do not run when you chat
with the in-app assistant.
