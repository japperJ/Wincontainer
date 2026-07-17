# ToggleSwitch

`ToggleSwitch` represents a physical switch that can be flipped on or off.
Use it for binary settings that take effect immediately (e.g., enable/disable a feature).

---

## Basic ToggleSwitch

```xaml
<ToggleSwitch
    Header="Wi-Fi"
    AutomationProperties.Name="Wi-Fi toggle" />
```

---

## Custom On/Off Content

```xaml
<ToggleSwitch
    Header="Background sync"
    OnContent="Sync on"
    OffContent="Sync off"
    IsOn="{x:Bind ViewModel.IsSyncEnabled, Mode=TwoWay}"
    AutomationProperties.Name="Background sync toggle" />
```

---

## ToggleSwitch Driving Another Control

```xaml
<StackPanel Orientation="Horizontal" Spacing="12">
    <ToggleSwitch
        x:Name="WorkToggle"
        Header="Processing"
        OnContent="Working"
        OffContent="Idle"
        AutomationProperties.Name="Processing toggle" />
    <ProgressRing
        Width="32"
        IsActive="{x:Bind WorkToggle.IsOn, Mode=OneWay}" />
</StackPanel>
```

---

## MVVM — Bound to ViewModel

```xaml
<!-- View -->
<ToggleSwitch
    Header="Dark mode"
    IsOn="{x:Bind ViewModel.IsDarkMode, Mode=TwoWay}"
    AutomationProperties.Name="Dark mode toggle" />
```

```csharp
// ViewModels/AppearanceViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class AppearanceViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isDarkMode;

    partial void OnIsDarkModeChanged(bool value)
    {
        // Apply theme change
        App.ApplyTheme(value ? ElementTheme.Dark : ElementTheme.Light);
    }
}
```

---

## Notes

- `ToggleSwitch` is preferred over `CheckBox` when the action is immediate (settings panel).
- Use `CheckBox` instead when changes are deferred (e.g., applied via a Save button).
- `OnContent` / `OffContent` accept any object, not just strings — you can put icons inside.
- Always provide `Header` and `AutomationProperties.Name` for accessibility.
