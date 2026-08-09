# Load Local Image Tar Through MCP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add one MCP tool that loads an existing local `.tar` image archive or base64-encoded tar data into the WSLC image store.

**Architecture:** Extend the existing `IWslcDriver` and `WslcCommands` layers with WSLC `image load --input`. Keep MCP argument validation in `WincontainerTools`; keep temporary-file creation, base64 decoding, WSLC execution, and cleanup in `WslcDriver`. Use the current MCP authorization, command execution, and error-result behavior.

**Tech Stack:** C# / .NET 10, WSLC CLI, ModelContextProtocol.Server, xUnit, FluentAssertions, ASP.NET Core integration tests.

---

## File map

- Modify `src/WinContainers.Core/WslcCommands.cs` to generate the quoted
  `image load --input` command.
- Modify `src/WinContainers.Runtime/IWslcDriver.cs` to expose image loading.
- Modify `src/WinContainers.Runtime/WslcDriver.cs` to validate archive input,
  materialize base64 data in a temporary file, execute WSLC, and clean up.
- Modify `src/WinContainers.Service/Mcp/WincontainerTools.cs` to expose and
  validate the `load_image` MCP tool.
- Modify `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs` for command,
  interface, input, and cleanup tests.
- Modify `tests/WinContainers.Tests.Unit/Ai/Fakes.cs` so existing test doubles
  implement the expanded driver interface.
- Modify `tests/WinContainers.Tests.Integration/UnitTest1.cs` to verify MCP
  tool discovery.
- Modify `README.md` to document the new MCP tool and its two input modes.

### Task 1: Add the WSLC image-load command and driver contract

**Files:**
- Modify: `src/WinContainers.Core/WslcCommands.cs`
- Modify: `src/WinContainers.Runtime/IWslcDriver.cs`
- Test: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

- [ ] **Step 1: Write failing command and contract tests**

Add assertions beside the existing image command tests:

```csharp
WslcCommands.ImageLoad(@"C:\images\app.tar")
    .Should().Be(@"image load --input C:\images\app.tar");
WslcCommands.ImageLoad(@"C:\Users\me\my image.tar")
    .Should().Be(@"image load --input ""C:\Users\me\my image.tar""");
```

Add `nameof(IWslcDriver.LoadImageAsync)` to the interface method contract
assertions.

- [ ] **Step 2: Run the focused unit tests and verify they fail**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: compilation or assertion failures because `ImageLoad` and
`LoadImageAsync` do not exist.

- [ ] **Step 3: Implement the command and interface**

Add this command next to `ImagePull`:

```csharp
public static string ImageLoad(string path) => $"image load --input {Quote(path)}";
```

Add this interface method next to `PullImageAsync`:

```csharp
Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct);
```

- [ ] **Step 4: Run the focused unit tests**

Run the same filtered command. Expected: PASS for command and contract
assertions; existing fake-driver compilation may still fail until Task 2.

- [ ] **Step 5: Commit the command contract**

```powershell
git add src/WinContainers.Core/WslcCommands.cs src/WinContainers.Runtime/IWslcDriver.cs tests/WinContainers.Tests.Unit/RuntimeContractTests.cs
git commit -m "feat: add WSLC image load contract" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 2: Implement safe archive loading in `WslcDriver`

**Files:**
- Modify: `src/WinContainers.Runtime/WslcDriver.cs`
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`
- Modify: `tests/WinContainers.Tests.Unit/Ai/Fakes.cs`

- [ ] **Step 1: Add failing driver behavior tests**

Call the concrete driver for validation-only cases, which must complete before
the process starts:

```csharp
var missing = await driver.LoadImageAsync(null, null, CancellationToken.None);
missing.Should().Be("Validation error: provide exactly one of tarPath or tarData.");

var wrongExtension = await driver.LoadImageAsync(
    Path.Combine(tempDirectory, "image.txt"), null, CancellationToken.None);
wrongExtension.Should().Be("Validation error: tarPath must point to an existing .tar file.");

var missingFile = await driver.LoadImageAsync(
    Path.Combine(tempDirectory, "missing.tar"), null, CancellationToken.None);
missingFile.Should().Be("Validation error: tarPath must point to an existing .tar file.");

var invalidBase64 = await driver.LoadImageAsync(null, "not-base64", CancellationToken.None);
invalidBase64.Should().Be("Validation error: tarData is not valid base64.");
```

Add a base64 cleanup test using `Convert.ToBase64String(Encoding.UTF8.GetBytes("tar"))`.
Record the existing `*.tar` files in `Path.GetTempPath()`, call
`LoadImageAsync`, then compare the directory contents after the call. If WSLC
is unavailable, allow the expected `FileNotFoundException`; the assertion is
that no new temporary archive remains. Add the encoded-length boundary test
with a string longer than the 512 MB decoded equivalent and assert:

```csharp
result.Should().Be("Validation error: tarData exceeds 512 MB after decoding.");
```

- [ ] **Step 2: Run the focused tests and verify they fail**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: failures for the missing driver method and input validation.

- [ ] **Step 3: Implement path and base64 handling**

Use a constant decoded limit:

```csharp
private const long MaxImageTarBytes = 512L * 1024 * 1024;
```

Implement `LoadImageAsync` with these rules:

1. Require exactly one non-empty argument.
2. For `tarPath`, require `File.Exists(tarPath)` and
   `Path.GetExtension(tarPath).Equals(".tar", StringComparison.OrdinalIgnoreCase)`.
3. For `tarData`, reject invalid base64 and decoded data larger than
   `MaxImageTarBytes`.
4. For base64 data, create a unique file with
   `Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.tar")`, write the
   decoded bytes, run `RunAndCaptureAsync(WslcCommands.ImageLoad(tempPath),
   1800000, ct)`, and delete the file in `finally`.
5. For path data, call `RunAndCaptureAsync(WslcCommands.ImageLoad(tarPath),
   1800000, ct)` directly.
6. Return these exact validation strings:
   `Validation error: provide exactly one of tarPath or tarData.`,
   `Validation error: tarPath must point to an existing .tar file.`,
   `Validation error: tarData is not valid base64.`, and
   `Validation error: tarData exceeds 512 MB after decoding.`. Do not catch
   WSLC execution exceptions or cancellation.

Use `Convert.FromBase64String` only after checking the encoded length against
the equivalent 512 MB decoded bound, then check the decoded length before
writing. This prevents accepting oversized payloads while keeping the
implementation bounded by the MCP request limit.

- [ ] **Step 4: Update test doubles**

Add this implementation to `tests/WinContainers.Tests.Unit/Ai/Fakes.cs`:

```csharp
public Task<string> LoadImageAsync(string? tarPath, string? tarData, CancellationToken ct) =>
    Task.FromResult(string.Empty);
```

Use the fake in MCP tests so no real WSLC process is started.

- [ ] **Step 5: Run unit tests**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests|FullyQualifiedName~ContainerAgentTests"
```

Expected: PASS, including validation and cleanup coverage.

- [ ] **Step 6: Commit runtime loading**

```powershell
git add src/WinContainers.Runtime/WslcDriver.cs tests/WinContainers.Tests.Unit/RuntimeContractTests.cs tests/WinContainers.Tests.Unit/Ai/Fakes.cs
git commit -m "feat: load image archives through WSLC" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 3: Expose the `load_image` MCP tool

**Files:**
- Modify: `src/WinContainers.Service/Mcp/WincontainerTools.cs`
- Test: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

- [ ] **Step 1: Add failing MCP validation tests**

Call the static tool method with a fake driver and assert that invalid
arguments return validation text and do not call the fake:

```csharp
await WincontainerTools.LoadImage(null, null, fakeDriver, CancellationToken.None);
await WincontainerTools.LoadImage("a.tar", "base64", fakeDriver, CancellationToken.None);
```

Add valid path and valid base64 cases that delegate the exact values to
`LoadImageAsync`.

- [ ] **Step 2: Run the focused unit tests and verify they fail**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~RuntimeContractTests"
```

Expected: compilation failure because `WincontainerTools.LoadImage` does not
exist.

- [ ] **Step 3: Add the MCP tool**

Add it in the Images section:

```csharp
[McpServerTool, Description("Load a local .tar container image archive into the WSLC image store.")]
public static async Task<string> LoadImage(
    [Description("Existing local .tar path on the Wincontainer host; provide this or tarData, not both.")] string? tarPath = null,
    [Description("Base64-encoded .tar archive, maximum 512 MB decoded; provide this or tarPath, not both.")] string? tarData = null,
    IWslcDriver driver = null!,
    CancellationToken ct = default)
{
    var hasPath = !string.IsNullOrWhiteSpace(tarPath);
    var hasData = !string.IsNullOrWhiteSpace(tarData);
    if (hasPath == hasData)
        return "Validation error: provide exactly one of tarPath or tarData.";

    return await driver.LoadImageAsync(tarPath, tarData, ct);
}
```

Keep path, extension, base64, and size validation in the driver so all
callers receive the same behavior.

- [ ] **Step 4: Run unit tests**

Run the focused RuntimeContractTests command. Expected: PASS for valid
delegation and exclusive-input validation.

- [ ] **Step 5: Commit the MCP tool**

```powershell
git add src/WinContainers.Service/Mcp/WincontainerTools.cs tests/WinContainers.Tests.Unit/RuntimeContractTests.cs
git commit -m "feat: expose image tar loading through MCP" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 4: Verify MCP discovery and document usage

**Files:**
- Modify: `tests/WinContainers.Tests.Integration/UnitTest1.cs`
- Modify: `README.md`

- [ ] **Step 1: Extend the MCP discovery assertion**

In `ServiceHost_ShouldExposeMcpToolsForAuthorizedRequests`, require the
returned tool names to contain `load_image` in addition to `health_check`.

- [ ] **Step 2: Document the tool**

Add `LoadImage` to the README MCP tools table. Document both forms:

```text
load_image(tarPath="C:\\images\\app.tar")
load_image(tarData="<base64 tar data>")
```

State that exactly one input is required, only `.tar` paths are accepted, and
base64 data is limited to 512 MB after decoding. State that the path is read
by the Wincontainer host, not by the MCP client machine.

- [ ] **Step 3: Run integration tests**

Run:

```powershell
dotnet test tests/WinContainers.Tests.Integration/WinContainers.Tests.Integration.csproj -c Debug --nologo -v q --filter "FullyQualifiedName~ServiceHost_ShouldExposeMcpToolsForAuthorizedRequests"
```

Expected: PASS and the MCP tool list includes `load_image`.

- [ ] **Step 4: Commit integration coverage and docs**

```powershell
git add tests/WinContainers.Tests.Integration/UnitTest1.cs README.md
git commit -m "docs: describe MCP image tar loading" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```

### Task 5: Run final validation

**Files:**
- No new files.

- [ ] **Step 1: Run the unit test project**

```powershell
dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q
```

Expected: all unit tests pass.

- [ ] **Step 2: Run the integration test project**

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
git --no-pager log --oneline -6
```

Expected: only the approved design, runtime/MCP implementation, tests, and
README changes are present, with no generated files staged.
