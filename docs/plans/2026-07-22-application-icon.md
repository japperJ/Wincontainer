# Application Icon Implementation Plan

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** Give WinContainers a recognizable branded icon in the window title bar, taskbar, and Windows shell surfaces.

**Architecture:** Keep the existing `AppWindow.SetIcon` call for the runtime window icon and declare the same ICO as the project executable icon so unpackaged builds use it for the taskbar and shortcuts. Replace the placeholder PNG/ICO artwork with a consistent container-themed icon rendered at the existing asset sizes.

**Tech Stack:** WinUI 3, Windows App SDK, MSBuild application icon metadata, PNG/ICO assets.

---

### Task 1: Add regression coverage

**Files:**
- Modify: `tests/WinContainers.Tests.Unit/RuntimeContractTests.cs`

**Step 1: Write the failing test**

Add source-contract assertions that `WinContainers.App.csproj` declares `Assets\AppIcon.ico` as `ApplicationIcon` and `MainWindow.xaml.cs` continues to set the `AppWindow` icon from that asset.

**Step 2: Run the focused test**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ApplicationIcon`

Expected: FAIL because the project does not currently declare an executable application icon.

### Task 2: Create and wire branded assets

**Files:**
- Modify: `src/WinContainers.App/WinContainers.App.csproj`
- Replace: `src/WinContainers.App/Assets/AppIcon.ico`
- Replace: `src/WinContainers.App/Assets/Square44x44Logo.scale-200.png`
- Replace: `src/WinContainers.App/Assets/Square44x44Logo.targetsize-24_altform-unplated.png`
- Replace: `src/WinContainers.App/Assets/Square44x44Logo.targetsize-48_altform-lightunplated.png`
- Replace: `src/WinContainers.App/Assets/Square150x150Logo.scale-200.png`
- Replace: `src/WinContainers.App/Assets/StoreLogo.png`
- Replace: `src/WinContainers.App/Assets/Wide310x150Logo.scale-200.png`
- Replace: `src/WinContainers.App/Assets/SplashScreen.scale-200.png`
- Replace: `src/WinContainers.App/Assets/LockScreenLogo.scale-200.png`

**Step 1: Add executable icon metadata**

Set `<ApplicationIcon>Assets\AppIcon.ico</ApplicationIcon>` in the application project.

**Step 2: Render the icon set**

Use one visual mark across shell assets: a deep navy rounded square with a cyan container stack and a coral status accent, with adequate padding and contrast at 16px/24px sizes.

**Step 3: Verify the runtime path**

Keep `AppWindow.SetIcon("Assets/AppIcon.ico")` unchanged so the title-bar/window icon and executable icon use the same source.

### Task 3: Verify the complete change

**Files:**
- No additional files.

**Step 1: Run focused tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q --filter FullyQualifiedName~ApplicationIcon`

Expected: PASS.

**Step 2: Run all unit tests**

Run: `dotnet test tests/WinContainers.Tests.Unit/WinContainers.Tests.Unit.csproj -c Debug --nologo -v q`

Expected: PASS with no warnings or errors.

**Step 3: Build the application**

Run: `dotnet build WinContainers.slnx -c Debug --nologo -v q`

Expected: Build succeeds with zero warnings and zero errors.

**Step 4: Check the diff**

Run: `git diff --check`

Expected: No whitespace errors.
