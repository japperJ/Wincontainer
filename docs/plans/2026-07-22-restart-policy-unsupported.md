# Remove Unsupported Restart Policy Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Prevent Quick Actions and imported Docker/Compose configurations from implying that WSLC supports container restart policies when the run command cannot apply them.

**Architecture:** Remove restart policy from the interactive create request and its service/runtime plumbing. Keep import parsing only long enough to detect a requested Docker/Compose policy, then emit a warning and omit it from the WSLC run command. This makes the unsupported behavior explicit without adding a fake WSLC equivalent.

**Tech Stack:** C#/.NET 10, WinUI 3, ASP.NET Minimal API, xUnit, FluentAssertions.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing tests**

Add source-contract assertions that the Quick Actions XAML no longer exposes `RestartPolicyCombo`, the code-behind no longer wires it, and the run request no longer carries a restart argument.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~RestartPolicy`

Expected: FAIL because the current Quick Actions UI and request path still contain restart-policy plumbing.

### Task 2: Remove restart policy from the run path

**Files:**
- Modify: `src/WinContainers.Core/WslcCommands.cs`
- Modify: `src/WinContainers.Runtime/WslcDriver.cs`
- Modify: `src/WinContainers.Service/Host/ServiceHost.cs`
- Modify: `src/WinContainers.App/Services/WslcServiceClient.cs`
- Modify: `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs`

**Step 1: Remove the unused `restart` parameter**

Remove it from `WslcCommands.Run`, `WslcDriver.RunContainerAsync`, the service request record/handler, the service client, and interactive/compose run calls.

**Step 2: Add import warnings**

When Docker Run or Compose input contains a non-empty restart policy, write a warning stating that WSLC does not support restart policies and that the policy will be ignored. Do not expose or store it as a selectable configuration.

**Step 3: Run the focused tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~RestartPolicy`

Expected: PASS.

### Task 3: Remove the unsupported Quick Actions control

**Files:**
- Modify: `src/WinContainers.App/Pages/QuickActionsControl.xaml`
- Modify: `src/WinContainers.App/Pages/QuickActionsControl.xaml.cs`
- Modify: `src/WinContainers.App/ViewModels/QuickActionsViewModel.cs`

**Step 1: Remove the policy ComboBox and its misleading description**

Delete the restart-policy control and its event synchronization code, along with the unused ViewModel policy properties and parsed-service field.

**Step 2: Re-run focused tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~RestartPolicy`

Expected: PASS.

### Task 4: Verify the complete change

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
