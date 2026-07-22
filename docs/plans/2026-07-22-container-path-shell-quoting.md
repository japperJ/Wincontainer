# Container Path Shell Quoting Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent container file-management paths from being interpreted as shell syntax when commands are executed inside a container.

**Architecture:** Add one POSIX shell-quoting helper to `WslcCommands` and use it for every path assembled by `ContainerDetailViewModel`. The helper wraps every value in single quotes and escapes embedded apostrophes with the standard POSIX shell sequence, while the existing free-form shell command feature remains unchanged by design.

**Tech Stack:** C#, .NET 10, xUnit, FluentAssertions, POSIX shell command syntax.

---

### Task 1: Add the failing regression contract

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source contract test requiring a centralized `WslcCommands.ShellQuote` helper and requiring `ContainerDetailViewModel` to use it for file paths instead of the incomplete space-only `EscapePath` implementation.

**Step 2: Run the test to verify it fails**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerFilePaths_ShouldUseCentralizedShellQuoting`

Expected: FAIL because the helper does not exist and file paths still use `EscapePath`.

### Task 2: Implement centralized shell quoting

**Files:**
- Modify: `src/WinContainers.Core/WslcCommands.cs`
- Modify: `src/WinContainers.App/ViewModels/ContainerDetailViewModel.cs`

**Step 1: Add the quote helper**

Add `WslcCommands.ShellQuote(string value)` that returns a single-quoted POSIX shell argument and safely encodes embedded apostrophes by closing the quote, escaping the apostrophe, and reopening the quote.

**Step 2: Route file paths through the helper**

Replace `EscapePath` uses in directory listing, file reads, and permission changes with `WslcCommands.ShellQuote`. Use the same helper for file writes and remove the duplicate local `ShellQuote` and `EscapePath` helpers.

### Task 3: Verify the fix

**Files:**
- No additional files.

**Step 1: Run the focused regression test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerFilePaths_ShouldUseCentralizedShellQuoting`

Expected: PASS.

**Step 2: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: all tests pass.

**Step 3: Build the affected projects**

Run: `dotnet build src/WinContainers.App/WinContainers.App.csproj -c Debug --nologo -v q`

Expected: build succeeds with no warnings or errors.

**Step 4: Check the diff**

Run: `git diff --check`

Expected: no whitespace errors.
