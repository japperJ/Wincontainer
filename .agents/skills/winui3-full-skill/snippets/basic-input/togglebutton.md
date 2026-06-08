# ToggleButton

`ToggleButton` is a `Button` that toggles between checked and unchecked states.
Use it for toolbar actions that can be on/off (e.g., Bold, Italic, mute).

---

## Basic ToggleButton

```xaml
<ToggleButton
    Content="Bold"
    AutomationProperties.Name="Bold text toggle" />
```

---

## Checked / Unchecked Events

```xaml
<ToggleButton
    x:Name="MuteButton"
    Content="Mute"
    Checked="MuteButton_Checked"
    Unchecked="MuteButton_Unchecked"
    AutomationProperties.Name="Mute toggle" />
```

```csharp
// MainPage.xaml.cs
private void MuteButton_Checked(object sender, RoutedEventArgs e)
{
    AudioService.Mute();
}

private void MuteButton_Unchecked(object sender, RoutedEventArgs e)
{
    AudioService.Unmute();
}
```

---

## Icon ToggleButton (Toolbar Style)

```xaml
<ToggleButton
    IsChecked="{x:Bind ViewModel.IsBold, Mode=TwoWay}"
    AutomationProperties.Name="Bold">
    <FontIcon Glyph="&#xE8DD;" FontSize="16" />
</ToggleButton>
```

---

## MVVM — Bound to ViewModel

```xaml
<!-- View -->
<StackPanel Orientation="Horizontal" Spacing="4">
    <ToggleButton
        IsChecked="{x:Bind ViewModel.IsBold, Mode=TwoWay}"
        AutomationProperties.Name="Bold">
        <FontIcon Glyph="&#xE8DD;" FontSize="16" />
    </ToggleButton>
    <ToggleButton
        IsChecked="{x:Bind ViewModel.IsItalic, Mode=TwoWay}"
        AutomationProperties.Name="Italic">
        <FontIcon Glyph="&#xE8DB;" FontSize="16" />
    </ToggleButton>
    <ToggleButton
        IsChecked="{x:Bind ViewModel.IsUnderline, Mode=TwoWay}"
        AutomationProperties.Name="Underline">
        <FontIcon Glyph="&#xE8DC;" FontSize="16" />
    </ToggleButton>
</StackPanel>
```

```csharp
// ViewModels/EditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isBold;

    [ObservableProperty]
    private bool _isItalic;

    [ObservableProperty]
    private bool _isUnderline;
}
```

---

## Notes

- `IsChecked` is nullable (`bool?`) — it can be `true`, `false`, or `null` (indeterminate).
  For most uses, bind to a `bool` property; null/indeterminate is rarely needed.
- `ToggleButton` differs from `ToggleSwitch`: use `ToggleButton` for toolbar/compact toggle actions,
  `ToggleSwitch` for settings panels.
- `Checked` / `Unchecked` events fire when state changes; `Click` fires on every click regardless.
- For mutual exclusion across a group, use `RadioButton` or a custom group logic.
