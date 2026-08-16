# Centered settings controls

## Goal

Restore the layout fix from merged PR #127 so settings buttons and toggles align to the left and size to their content.

## Scope

- Modify only `src/WinContainers.App/Pages/SettingsPage.xaml`.
- Add `HorizontalAlignment="Left"` to the settings action buttons and toggles changed by PR #127.
- Do not change event handlers, view models, behavior, or other pages.

## Validation

Run the existing solution Debug build:

```text
dotnet build WinContainers.slnx -c Debug --nologo -v q
```
