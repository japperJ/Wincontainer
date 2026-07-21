# Container Detail Handler Lifecycle Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent `ContainerDetailPage` from accumulating `PropertyChanged` callbacks across navigation cycles.

**Architecture:** Store the inspect-property handler as a page field and use a named handler method. Detach any existing handler before attaching a new one, unsubscribe it in `OnNavigatedFrom`, and clear the field after removal.

**Tech Stack:** .NET 10, WinUI 3, CommunityToolkit.Mvvm, xUnit, FluentAssertions.

---

### Task 1: Add the regression test

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add a source-contract test for `ContainerDetailPage.xaml.cs` that requires a stored `PropertyChangedEventHandler`, named handler registration/removal, and clearing the handler field.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerDetailPage`

Expected: FAIL because the current page attaches an inline lambda and never removes it.

### Task 2: Implement handler lifecycle management

**Files:**
- Modify: `src/WinContainers.App/Pages/ContainerDetailPage.xaml.cs`

**Step 1: Add a handler field**

Add a nullable `PropertyChangedEventHandler` field for the inspect JSON callback.

**Step 2: Detach before reattaching**

Before replacing or reusing the view model in `OnNavigatedTo`, remove the stored handler if present. Attach a named handler and store it in the field after the view model is selected.

**Step 3: Unsubscribe on navigation away**

Remove the stored handler from the view model in `OnNavigatedFrom`, then clear the field before stopping the timer.

**Step 4: Move callback logic to the named handler**

Preserve the existing `InspectJson` behavior in a named async handler that uses the sender view model rather than relying on a stale closure.

**Step 5: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ContainerDetailPage`

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
