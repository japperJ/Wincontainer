# Slider

A `Slider` lets the user select from a range of values by moving a thumb along a track.
Use it for settings like volume, brightness, or any continuous numeric input.

---

## Basic Slider

```xaml
<Slider
    Width="200"
    AutomationProperties.Name="Volume" />
```

```csharp
// No code-behind required for basic use.
// Bind Value to a ViewModel property:
// <Slider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}" />
```

---

## Slider with Range, Step, and Header

```xaml
<Slider
    Width="200"
    Header="Temperature (°C)"
    Minimum="16"
    Maximum="30"
    StepFrequency="0.5"
    SmallChange="0.5"
    Value="{x:Bind ViewModel.Temperature, Mode=TwoWay}"
    AutomationProperties.Name="Temperature slider" />
```

---

## Slider with Tick Marks

```xaml
<Slider
    Width="290"
    Minimum="0"
    Maximum="100"
    TickFrequency="20"
    TickPlacement="Outside"
    SnapsTo="Ticks"
    AutomationProperties.Name="Slider with ticks" />
```

---

## Vertical Slider

```xaml
<Slider
    Width="100"
    Height="200"
    Orientation="Vertical"
    Minimum="-50"
    Maximum="50"
    TickFrequency="10"
    TickPlacement="Outside"
    AutomationProperties.Name="Vertical slider" />
```

---

## MVVM — Bound to ViewModel

```xaml
<!-- View -->
<StackPanel>
    <Slider
        Width="200"
        Header="Brightness"
        Minimum="0"
        Maximum="100"
        Value="{x:Bind ViewModel.Brightness, Mode=TwoWay}"
        AutomationProperties.Name="Brightness slider" />
    <TextBlock Text="{x:Bind ViewModel.Brightness, Mode=OneWay}" />
</StackPanel>
```

```csharp
// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private double _brightness = 50;
}
```

---

## Notes

- Always set `AutomationProperties.Name` for accessibility.
- `StepFrequency` controls snapping interval; `TickFrequency` controls how often tick marks appear.
- `SnapsTo` can be `"StepValues"` (default) or `"Ticks"`.
- Prefer `TwoWay` binding when the value must persist in the ViewModel.
- Do not read `Slider.Value` directly in code-behind for MVVM apps — bind to the ViewModel.
