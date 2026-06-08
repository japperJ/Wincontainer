# ColorPicker

`ColorPicker` lets users pick a color from a spectrum, sliders, and hex/RGB input fields.
Use it in theming panels, drawing apps, or any setting that requires color selection.

---

## Basic ColorPicker

```xaml
<ColorPicker
    x:Name="MyColorPicker"
    AutomationProperties.Name="Color picker" />
```

---

## ColorPicker with All Options Shown

```xaml
<ColorPicker
    x:Name="FullColorPicker"
    IsAlphaEnabled="True"
    IsAlphaSliderVisible="True"
    IsAlphaTextInputVisible="True"
    IsColorChannelTextInputVisible="True"
    IsColorSliderVisible="True"
    IsHexInputVisible="True"
    IsMoreButtonVisible="False"
    ColorSpectrumShape="Box"
    AutomationProperties.Name="Full color picker" />
```

---

## ColorPicker in a Flyout (Compact Pattern)

```xaml
<StackPanel Spacing="8">
    <TextBlock Text="Selected color:" />
    <Rectangle
        x:Name="ColorPreview"
        Width="80"
        Height="40"
        CornerRadius="4"
        Stroke="{ThemeResource TextControlBorderBrush}"
        StrokeThickness="1">
        <Rectangle.Fill>
            <SolidColorBrush Color="{x:Bind ViewModel.SelectedColor, Mode=OneWay}" />
        </Rectangle.Fill>
    </Rectangle>
    <Button Content="Pick color" AutomationProperties.Name="Open color picker">
        <Button.Flyout>
            <Flyout>
                <ColorPicker
                    Color="{x:Bind ViewModel.SelectedColor, Mode=TwoWay}"
                    IsHexInputVisible="True"
                    AutomationProperties.Name="Color picker flyout" />
            </Flyout>
        </Button.Flyout>
    </Button>
</StackPanel>
```

```csharp
// ViewModels/ThemeViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Windows.UI;

namespace MyApp.ViewModels;

public partial class ThemeViewModel : ObservableObject
{
    [ObservableProperty]
    private Color _selectedColor = Colors.CornflowerBlue;
}
```

---

## MVVM — Full Binding

```xaml
<!-- View -->
<ColorPicker
    Color="{x:Bind ViewModel.AccentColor, Mode=TwoWay}"
    IsHexInputVisible="True"
    IsAlphaEnabled="False"
    AutomationProperties.Name="Accent color picker" />
```

```csharp
// ViewModels/PersonalizationViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.UI;

namespace MyApp.ViewModels;

public partial class PersonalizationViewModel : ObservableObject
{
    [ObservableProperty]
    private Color _accentColor = Colors.Blue;

    [RelayCommand]
    private void ResetColor() => AccentColor = Colors.Blue;
}
```

---

## Notes

- `ColorSpectrumShape` can be `"Box"` (default) or `"Ring"`.
- Alpha channel is disabled by default — enable via `IsAlphaEnabled="True"`.
- `IsMoreButtonVisible="True"` collapses the text inputs behind a "more" button, useful for compact UIs.
- For flyout usage, bind `Color` `TwoWay` so the ViewModel is updated when the flyout closes.
- `ColorPicker.Color` is of type `Windows.UI.Color`, not `Microsoft.UI.Color`.
