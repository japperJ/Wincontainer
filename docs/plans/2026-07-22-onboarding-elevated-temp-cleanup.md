# Onboarding Elevated Temp Cleanup Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Ensure elevated onboarding command artifacts are isolated per invocation and deleted on success, timeout, cancellation, process-start failure, and other exceptions.

**Architecture:** Keep the existing elevated PowerShell execution and timeout behavior unchanged. Create a unique child directory below the existing application temp directory for each invocation, put the script, launcher, and log there, and place the whole operation inside `try/finally` so cleanup is independent of the return path. Remove the invocation directory recursively with the existing best-effort logging approach.

**Tech Stack:** C#, .NET 10, WinUI 3, xUnit, FluentAssertions.

---

### Task 1: Add the failing regression contract

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source contract test requiring a unique invocation directory, directory creation, a `finally` block, and invocation-directory cleanup.

**Step 2: Run test to verify it fails**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~OnboardingViewModel_ShouldCleanupElevatedTempFilesOnEveryExitPath`

Expected: FAIL because `RunElevatedCommandAsync` currently uses shared temp files and deletes them only before its normal return.

### Task 2: Isolate and always clean elevated artifacts

**Files:**
- Modify: `src/WinContainers.App/ViewModels/OnboardingViewModel.cs:424-523`

**Step 1: Create a per-invocation directory**

Create `WinContainers/elevated-{runId}` below the system temp directory and derive the script, launcher, and log paths from that directory.

**Step 2: Move the operation into `try/finally`**

Wrap artifact creation, process execution, output collection, timeout handling, and result construction in `try/finally`. Preserve the existing timeout result and process-tree kill behavior.

**Step 3: Add recursive best-effort directory cleanup**

Add `TryDeleteTempDirectory` using `Directory.Delete(path, recursive: true)` and log `IOException` or `UnauthorizedAccessException`. Call it from `finally`; remove the normal-path-only file cleanup calls.

### Task 3: Verify the fix

**Files:**
- No additional files.

**Step 1: Run the focused regression test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~OnboardingViewModel_ShouldCleanupElevatedTempFilesOnEveryExitPath`

Expected: PASS.

**Step 2: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: all tests pass.

**Step 3: Build the app project**

Run: `dotnet build src/WinContainers.App/WinContainers.App.csproj -c Debug --nologo -v q`

Expected: build succeeds with no warnings or errors.

**Step 4: Check the diff**

Run: `git diff --check`

Expected: no whitespace errors.
