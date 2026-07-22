# WSLC Timeout Output Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ensure `WslcDriver` observes redirected stdout and stderr tasks after an internal process timeout without changing caller-cancellation behavior.

**Architecture:** Keep the existing timeout and caller cancellation distinction in `RunAsync`. On the internal timeout branch, kill the process and pass both output tasks to a private bounded drain helper. The helper awaits both tasks when possible and attaches a fault observer if the cleanup bound expires, preventing late stream exceptions from becoming unobserved.

**Tech Stack:** C#, .NET 10, xUnit, FluentAssertions.

---

### Task 1: Add the failing regression contract

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a test that reads `WslcDriver.cs` and requires the timeout branch to await a dedicated output cleanup helper, and requires that helper to use a bounded wait and fault observation.

**Step 2: Run the test to verify it fails**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~WslcDriver_ShouldBoundOutputCleanupAfterTimeout`

Expected: FAIL because the current timeout branch returns immediately after `TryKill(process)` and has no cleanup helper.

### Task 2: Implement bounded output cleanup

**Files:**
- Modify: `src/WinContainers.Runtime/WslcDriver.cs`

**Step 1: Add the minimal cleanup implementation**

Add a private cleanup timeout constant and a helper that awaits `Task.WhenAll(stdoutTask, stderrTask)` with `WaitAsync`. If the bounded wait expires, attach a continuation that observes a later fault from the combined task. Log cleanup timeout or cleanup exceptions with `Trace`.

**Step 2: Use the helper only on the internal timeout path**

After `TryKill(process)` in the existing timeout catch, await the helper before returning the timeout result. Leave the caller cancellation path outside this catch so caller cancellation still propagates.

### Task 3: Verify the fix

**Files:**
- No additional files.

**Step 1: Run the focused regression test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~WslcDriver_ShouldBoundOutputCleanupAfterTimeout`

Expected: PASS.

**Step 2: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: all tests pass.

**Step 3: Build the runtime project**

Run: `dotnet build src/WinContainers.Runtime/WinContainers.Runtime.csproj -c Debug --nologo -v q`

Expected: build succeeds with no warnings or errors.

**Step 4: Check the diff**

Run: `git diff --check`

Expected: no whitespace errors.
