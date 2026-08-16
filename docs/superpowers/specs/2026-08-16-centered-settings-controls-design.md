# Centered settings controls

## Goal

Document the layout fix merged in PR #127, which makes settings buttons and toggles align to the left and size to their content.

## Scope

- Record the completed changes to `src/WinContainers.App/Pages/SettingsPage.xaml`.
- The settings action buttons and toggles changed by PR #127 have `HorizontalAlignment="Left"`.
- Do not change event handlers, view models, behavior, or other pages.

## Validation

Run the existing solution Debug build:

```text
dotnet build WinContainers.slnx -c Debug --nologo -v q
```
