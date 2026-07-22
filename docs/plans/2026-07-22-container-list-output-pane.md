# Container List Output Pane Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Automatically show the output pane when a start, stop, or delete action is launched from the container list.

**Architecture:** `ContainersViewModel` already writes action progress through the shared output service, while `MainWindow` owns the output pane state and exposes `EnsureOutputPaneVisible()`. The list page will invoke that existing UI method at the action-handler boundary before dispatching individual or group start, stop, and remove operations; no output-service or ViewModel coupling is added.

**Tech Stack:** WinUI 3, C#, xUnit, FluentAssertions.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source contract test that reads `ContainersControl.xaml.cs` and verifies the start, stop, and remove list handlers show the output pane before invoking their ViewModel actions. Include the group action handlers because they are the same container-list interaction surface.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerListOutput`

Expected: FAIL because the handlers currently call the ViewModel without `EnsureOutputPaneVisible()`.

### Task 2: Show output for list actions

**Files:**
- Modify: `src/WinContainers.App/Pages/ContainersControl.xaml.cs:49-143`

**Step 1: Implement the minimal fix**

Call `MainWindow.Instance?.EnsureOutputPaneVisible()` immediately before dispatching start, stop, and remove actions for individual containers and groups. For individual remove, call it after any volume confirmation dialog has completed and the removal has actually been confirmed.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerListOutput`

Expected: PASS.

### Task 3: Verify the repository

**Files:**
- No additional files.

**Step 1: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: All unit tests pass.

**Step 2: Build the solution**

Run: `dotnet build WinContainers.slnx -c Debug --nologo -v q`

Expected: Build succeeds with zero warnings and errors.

**Step 3: Check the diff**

Run: `git diff --check`

Expected: No whitespace errors.
