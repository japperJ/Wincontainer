# Chunked MCP Image Upload Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a chunked MCP upload lifecycle so Copilot can load image tar archives larger than its JSON tool-argument limit.

**Architecture:** Add a process-local `ImageUploadStore` that writes ordered base64 chunks directly to a temporary `.tar` file. Add `start_image_upload`, `upload_image_chunk`, and `finish_image_upload` MCP tools; the finish operation passes the completed path to the existing `IWslcDriver.LoadImageAsync` path mode and always removes upload state and files.

**Tech Stack:** C# / .NET 10, ASP.NET Core, ModelContextProtocol.Server, WSLC, xUnit, FluentAssertions.

---

## File map

- Create `src/WinContainers.Runtime/ImageUploadStore.cs` for upload state,
  ordered chunk validation, disk streaming, expiration, and cleanup.
- Modify `src/WinContainers.Service/Host/ServiceHost.cs` to register the
  process-local store.
- Modify `src/WinContainers.Service/Mcp/WincontainerTools.cs` to expose the
  three upload lifecycle tools.
- Modify `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs` to test store
  behavior and MCP delegation.
- Modify `tests/WinContainers.Tests.Integration/UnitTest1.cs` to verify the
  three tools are discoverable.
- Modify `README.md` to document chunked uploads and the 3 KB chunk limit.

### Task 1: Build the upload store

**Files:**
- Create: `src/WinContainers.Runtime/ImageUploadStore.cs`
- Test: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

- [ ] **Step 1: Write failing store tests**

Add tests for the public store contract:

```csharp
[Fact]
public async Task ImageUploadStore_ShouldAppendOrderedChunksAndReturnCompletedPath()
{
    var store = new ImageUploadStore(TimeProvider.System);
    var upload = store.Start();

    (await store.AppendChunkAsync(upload.UploadId, 0, ToBase64("abc"), CancellationToken.None))
        .Should().Be("Upload chunk accepted.");
    string? observedPath = null;
    var path = await store.CompleteAsync(
        upload.UploadId,
        (archivePath, ct) =>
        {
            observedPath = archivePath;
            File.ReadAllText(archivePath).Should().Be("abc");
            return Task.FromResult(archivePath);
        },
        CancellationToken.None);

    path.Should().Be(observedPath);
    File.Exists(observedPath).Should().BeFalse();
}
```

Also add tests that assert the exact validation results:

```csharp
(await store.AppendChunkAsync("missing", 0, ToBase64("x"), ct))
    .Should().Be("Validation error: upload ID was not found.");
(await store.AppendChunkAsync(uploadId, 1, ToBase64("x"), ct))
    .Should().Be("Validation error: expected chunk sequence 0.");
(await store.AppendChunkAsync(uploadId, 0, "not-base64", ct))
    .Should().Be("Validation error: chunk is not valid base64.");
```

Cover a decoded chunk larger than `3 * 1024`, a total larger than
`512L * 1024 * 1024`, expired uploads, and cleanup after a load callback
throws. Use an injected `TimeProvider` test double to advance expiration
without sleeping.

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: compilation failure because `ImageUploadStore` does not exist.

- [ ] **Step 3: Implement the store**

Use these constants and public signatures:

```csharp
public sealed class ImageUploadStore
{
    public const int MaxChunkBytes = 3 * 1024;
    public const long MaxUploadBytes = 512L * 1024 * 1024;
    private static readonly TimeSpan UploadLifetime = TimeSpan.FromMinutes(15);

    public ImageUploadStore(TimeProvider? timeProvider = null);
    public ImageUploadInfo Start();
    public Task<string> AppendChunkAsync(
        string uploadId, int sequence, string base64Chunk, CancellationToken ct);
    public Task<string> CompleteAsync(
        string uploadId,
        Func<string, CancellationToken, Task<string>> loadAsync,
        CancellationToken ct);
}

public sealed record ImageUploadInfo(string UploadId, int MaxChunkBytes, long MaxUploadBytes);
```

For `Start`, create a random GUID upload ID and an exclusive temporary file
named `<uploadId>.tar`. Keep a private dictionary keyed by upload ID. The
state must include the file path, next sequence, byte count, last activity,
and an async lock so concurrent calls for one upload cannot interleave.

For `AppendChunkAsync`, run expiration cleanup first, require the exact next
sequence, reject empty or invalid base64, reject decoded chunks over
`MaxChunkBytes`, reject totals over `MaxUploadBytes`, append with
`FileMode.Append`, update state, and return `Upload chunk accepted.`. Use
`ConfigureAwait(false)` for file operations and honor cancellation.

For `CompleteAsync`, close the file before invoking `loadAsync`, remove the
state and file in a `finally` block, and return the load result. If the ID is
invalid or the upload has expired, return the corresponding validation string
without invoking the callback. Run expiration cleanup on every public
operation and remove files for uploads inactive for 15 minutes.

- [ ] **Step 4: Run the focused unit tests**

Run the same filtered command. Expected: all upload-store tests pass.

- [ ] **Step 5: Commit the store**

```powershell
git add src/WinContainers.Runtime/ImageUploadStore.cs tests/WinContainers.Tests.Unit/RuntimeContractTests.cs
git commit -m "feat: add chunked image upload store" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 2: Register the store and add MCP lifecycle tools

**Files:**
- Modify: `src/WinContainers.Service/Host/ServiceHost.cs`
- Modify: `src/WinContainers.Service/Mcp/WincontainerTools.cs`
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

- [ ] **Step 1: Write failing MCP delegation tests**

Add tests using a fake `ImageUploadStore` or a small test double around the
store that assert:

```csharp
var start = await WincontainerTools.StartImageUpload(store, ct);
start.Should().Contain("uploadId");

var accepted = await WincontainerTools.UploadImageChunk(
    uploadId, 0, ToBase64("abc"), store, ct);
accepted.Should().Be("Upload chunk accepted.");
```

Test that `FinishImageUpload` passes the completed path to
`IWslcDriver.LoadImageAsync(path, null, ct)` and returns its result.

- [ ] **Step 2: Run tests and verify they fail**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: compilation failure because the three MCP methods do not exist.

- [ ] **Step 3: Register the singleton**

After the existing driver registration in `ServiceHost.Build`, add:

```csharp
builder.Services.AddSingleton<ImageUploadStore>();
```

- [ ] **Step 4: Add the three MCP tools**

Add these tools in the Images section:

```csharp
[McpServerTool, Description("Start a chunked image tar upload. Returns an upload ID.")]
public static string StartImageUpload(ImageUploadStore store) =>
    JsonSerializer.Serialize(store.Start());

[McpServerTool, Description("Append the next base64 chunk to an image tar upload. Chunks must be ordered and no larger than 3 KB decoded.")]
public static Task<string> UploadImageChunk(
    [Description("Upload ID returned by start_image_upload")] string uploadId,
    [Description("Zero-based chunk sequence number")] int sequence,
    [Description("Base64 data for one tar chunk, maximum 3 KB decoded")] string base64Chunk,
    ImageUploadStore store,
    CancellationToken ct) =>
    store.AppendChunkAsync(uploadId, sequence, base64Chunk, ct);

[McpServerTool, Description("Finish a chunked image tar upload and load it into WSLC.")]
public static Task<string> FinishImageUpload(
    [Description("Upload ID returned by start_image_upload")] string uploadId,
    ImageUploadStore store,
    IWslcDriver driver,
    CancellationToken ct) =>
    store.CompleteAsync(uploadId, (path, token) => driver.LoadImageAsync(path, null, token), ct);
```

Keep injected services undescribed so they remain runtime parameters, matching
the existing MCP tool pattern.

- [ ] **Step 5: Run unit tests**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: all store and MCP delegation tests pass.

- [ ] **Step 6: Commit the MCP lifecycle**

```powershell
git add src/WinContainers.Service/Host/ServiceHost.cs src/WinContainers.Service/Mcp/WincontainerTools.cs tests/WinContainers.Tests.Unit/RuntimeContractTests.cs
git commit -m "feat: expose chunked image uploads through MCP" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 3: Add discovery coverage and documentation

**Files:**
- Modify: `tests/WinContainers.Tests.Integration/UnitTest1.cs`
- Modify: `README.md`

- [ ] **Step 1: Extend MCP discovery assertions**

Require the tools/list result to contain:

```csharp
"start_image_upload", "upload_image_chunk", "finish_image_upload"
```

- [ ] **Step 2: Document the chunked workflow**

Add a short workflow after the existing `load_image` documentation:

```text
1. start_image_upload() -> uploadId
2. upload_image_chunk(uploadId, sequence, base64Chunk) for each ordered chunk
3. finish_image_upload(uploadId)
```

Document the 3 KB decoded chunk limit, 512 MB total limit, zero-based sequence,
15-minute inactivity expiry, and the fact that the upload is process-local.

- [ ] **Step 3: Run the integration test**

```powershell
dotnet test tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~ServiceHost_ShouldExposeMcpToolsForAuthorizedRequests"
```

Expected: PASS with all three upload tools discoverable.

- [ ] **Step 4: Commit documentation**

```powershell
git add tests/WinContainers.Tests.Integration/UnitTest1.cs README.md
git commit -m "docs: document chunked MCP image uploads" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 4: Run final validation

**Files:**
- No new files.

- [ ] **Step 1: Run all unit tests**

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q
```

Expected: all unit tests pass.

- [ ] **Step 2: Run all integration tests**

```powershell
dotnet test tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj -c Debug --nologo -v q
```

Expected: all integration tests pass.

- [ ] **Step 3: Build the solution**

```powershell
dotnet build WinContainers.slnx -c Debug --nologo -v q
```

Expected: build succeeds with no warnings treated as errors.

- [ ] **Step 4: Inspect the final diff**

```powershell
git --no-pager status --short
git --no-pager log --oneline -8
```

Expected: only the chunked upload specification, implementation, tests, and
documentation changes are present.
