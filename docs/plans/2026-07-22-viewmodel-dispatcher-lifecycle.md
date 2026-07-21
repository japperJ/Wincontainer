# ViewModel Dispatcher Lifecycle Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Make view-model property notifications safe before app launch and during dispatcher teardown.

**Architecture:** `ViewModelBase` captures the current optional `DispatcherQueue` when it is constructed instead of dereferencing the global application queue for every notification. Notifications run synchronously when no dispatcher is available, run directly when already on the dispatcher thread, and are safely dropped when enqueueing fails during teardown.

**Tech Stack:** .NET 10, WinUI 3 `DispatcherQueue`, CommunityToolkit.Mvvm, xUnit, FluentAssertions.

---

### Task 1: Add the regression test

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source-contract test that verifies `ViewModelBase` captures an optional dispatcher and handles unavailable dispatching without unconditional global dereferences.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ViewModelBase`

Expected: FAIL because the current implementation directly dereferences `App.DispatcherQueue` and has no lifecycle-safe fallback.

### Task 2: Implement lifecycle-safe notification dispatch

**Files:**
- Modify: `src/WinContainers.App/ViewModels/ViewModelBase.cs`
- Modify: `src/WinContainers.App/App.xaml.cs`

**Step 1: Make the application dispatcher nullable**

Declare `App.DispatcherQueue` as nullable because it is unavailable before `OnLaunched` and may be unavailable during teardown.

**Step 2: Capture and handle the dispatcher in `ViewModelBase`**

Capture `DispatcherQueue.GetForCurrentThread()` in the base constructor. If no queue exists, invoke the base notification synchronously. If the queue has thread access, invoke synchronously. Otherwise call `TryEnqueue`; if it returns false, do not throw because the queue is shutting down.

**Step 3: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ViewModelBase`

Expected: PASS.

### Task 3: Verify the complete change

**Files:**
- No additional files.

**Step 1: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: all unit tests pass.

**Step 2: Build the application**

Run: `dotnet build src/WinContainers.App/WinContainers.App.csproj -c Debug --nologo -v q`

Expected: build succeeds with zero warnings and errors.

**Step 3: Check the diff**

Run: `git diff --check`

Expected: no whitespace errors.
