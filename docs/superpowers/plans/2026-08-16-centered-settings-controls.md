# Centered settings controls Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Document the merged PR #127 XAML layout fix, which makes settings controls align left and size to their content.

**Architecture:** Document one surgical change in the existing WinUI settings page: `HorizontalAlignment="Left"` was added to the three action buttons and seven settings toggles changed by PR #127; all event wiring and behavior remain unchanged.

**Tech Stack:** WinUI 3 XAML, .NET 10, MSBuild.

---

### Task 1: Record restored settings control alignment

**Files:**
- Modify: `src/WinContainers.App/Pages/SettingsPage.xaml:17-116`

- [ ] **Step 1: Record left alignment on the settings buttons and toggles**

The required change is adding `HorizontalAlignment="Left"` to the listed existing controls:

```xml
<Button Content="Apply" Click="ApplyPortButton_Click" HorizontalAlignment="Left" />
<Button Content="Apply Token" Click="ApplyTokenButton_Click" HorizontalAlignment="Left" />
<ToggleSwitch x:Name="ApiLoggingToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="RemoteApiLoggingToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="AllowRemoteApiToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="McpEnabledToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="McpLoggingToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="McpDestructiveConfirmationToggle" HorizontalAlignment="Left" ... />
<ToggleSwitch x:Name="AiConfirmToggle" HorizontalAlignment="Left" ... />
<Button Content="Save AI settings" Click="SaveAiSettingsButton_Click" HorizontalAlignment="Left" />
```

Keep every existing property, event handler, and surrounding layout unchanged.

- [ ] **Step 2: Confirm the diff contains only the requested layout attributes**

Run:

```powershell
git --no-pager diff -- src/WinContainers.App/Pages/SettingsPage.xaml
```

Expected: only the 13 additions/removals from merged PR #127, with no unrelated file changes.

- [ ] **Step 3: Build the solution**

Run:

```powershell
dotnet build WinContainers.slnx -c Debug --nologo -v q
```

Expected: build succeeds with exit code 0.

- [ ] **Step 4: Commit the restored layout fix**

Run:

```powershell
git add src/WinContainers.App/Pages/SettingsPage.xaml
git commit -m "Restore centered settings controls fix" -m "Co-authored-by: Copilot App <223556219+Copilot@users.noreply.github.com>"
```
