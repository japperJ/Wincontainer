# Port Link Click Routing Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent clicking a container port link from also opening the container detail/log view.

**Architecture:** The port link is handled by `ContainersControl.PortLink_Click`, while the containing row listens for the bubbling `Tapped` event to open container details. A dedicated `Tapped` handler on the link marks the `TappedRoutedEventArgs` handled, preserving browser launch behavior and preventing the parent row action without changing navigation or layout behavior.

**Tech Stack:** WinUI 3, C#, xUnit, FluentAssertions.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source contract test that reads the container control XAML and code-behind and requires the port link to have a `Tapped` handler that marks its routed event handled.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~PortLink`

Expected: FAIL because `PortLink_Click` does not currently set `e.Handled`.

### Task 2: Stop the port event from bubbling

**Files:**
- Modify: `src/WinContainers.App/Pages/ContainersControl.xaml.cs:214-221`

**Step 1: Implement the minimal fix**

Add `Tapped="PortLink_Tapped"` to the port link and implement `PortLink_Tapped` with `TappedRoutedEventArgs`, setting `e.Handled = true`. Leave URL construction and browser launch in `PortLink_Click` unchanged.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~PortLink`

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
