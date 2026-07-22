# Bound Output History Retention Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Bound `OutputService` history so long-running sessions cannot retain an unbounded number of output messages.

**Architecture:** Keep the existing list-backed `IReadOnlyList` API and enforce a fixed 1,000-entry cap at the single write point. When full, remove the oldest entry before appending the newest one, preserving chronological order and the current latest-output behavior.

**Tech Stack:** C#/.NET 10, WinUI 3, xUnit, FluentAssertions.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source-contract test for `OutputService` that requires a documented retention constant, oldest-entry eviction before append, and the existing `IReadOnlyList` history contract.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~OutputService`

Expected: FAIL because `OutputService` currently has no retention bound or eviction logic.

### Task 2: Enforce the history limit

**Files:**
- Modify: `src/WinContainers.App/Services/OutputService.cs`

**Step 1: Add the documented limit**

Define a named constant with a 1,000-message limit and document that history is an in-memory diagnostic buffer, not an archival log.

**Step 2: Evict the oldest message before appending**

In `Write`, remove index zero when the list has reached the limit, then append the new `(level, text)` entry. Keep `LastOutput` and `OutputWritten` behavior unchanged.

**Step 3: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~OutputService`

Expected: PASS.

### Task 3: Verify the complete change

**Files:**
- No additional files.

**Step 1: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: PASS with no warnings or errors.

**Step 2: Build the application**

Run: `dotnet build WinContainers.slnx -c Debug --nologo -v q`

Expected: Build succeeds with zero warnings and zero errors.

**Step 3: Check the diff**

Run: `git diff --check`

Expected: No whitespace errors.
