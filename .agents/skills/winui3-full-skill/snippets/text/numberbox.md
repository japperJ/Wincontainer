# NumberBox

`NumberBox` is a numeric text input that supports spin buttons, expression evaluation,
formatting, and range constraints. Use it for any integer or decimal input.

---

## Basic NumberBox

```xaml
<NumberBox
    Header="Quantity"
    PlaceholderText="0"
    Value="NaN"
    Minimum="0"
    Maximum="999"
    AutomationProperties.Name="Quantity input" />
```

---

## With Spin Buttons

```xaml
<!-- Inline spin buttons (arrows always visible) -->
<NumberBox
    Header="Font size"
    Value="12"
    Minimum="8"
    Maximum="72"
    SmallChange="1"
    LargeChange="10"
    SpinButtonPlacementMode="Inline"
    AutomationProperties.Name="Font size" />

<!-- Compact spin buttons (arrows shown on focus) -->
<NumberBox
    Header="Zoom %"
    Value="100"
    Minimum="10"
    Maximum="400"
    SmallChange="10"
    SpinButtonPlacementMode="Compact"
    AutomationProperties.Name="Zoom level" />
```

---

## Expression Evaluation

```xaml
<!-- User can type "2^10" and it evaluates to 1024 -->
<NumberBox
    Header="Enter a value or expression"
    PlaceholderText="e.g. 2 + 3 * 4"
    AcceptsExpression="True"
    Value="NaN"
    AutomationProperties.Name="Expression input" />
```

---

## Formatted NumberBox (Currency)

```xaml
<NumberBox
    x:Name="PriceBox"
    Header="Price (USD)"
    PlaceholderText="0.00"
    Minimum="0"
    SmallChange="0.01"
    NumberFormatter="{x:Bind CurrencyFormatter}"
    AutomationProperties.Name="Price input" />
```

```csharp
// MainPage.xaml.cs
using Windows.Globalization.NumberFormatting;

public IncrementNumberRounder Rounder { get; } = new()
{
    Increment = 0.01,
    RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp
};

public DecimalFormatter CurrencyFormatter { get; } = new()
{
    IntegerDigits = 1,
    FractionDigits = 2
};
```

---

## MVVM — Bound to ViewModel

```xaml
<!-- View -->
<NumberBox
    Header="Temperature (°C)"
    Value="{x:Bind ViewModel.Temperature, Mode=TwoWay}"
    Minimum="-273.15"
    Maximum="1000"
    SmallChange="0.5"
    SpinButtonPlacementMode="Compact"
    AutomationProperties.Name="Temperature" />
```

```csharp
// ViewModels/SensorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SensorViewModel : ObservableObject
{
    [ObservableProperty]
    private double _temperature = 20.0;
}
```

---

## Notes

- `Value="NaN"` means "no value" — the placeholder text is shown.
- `SmallChange` is used by spin buttons and arrow keys; `LargeChange` is used by Page Up/Down.
- `AcceptsExpression="True"` allows users to type math expressions (e.g., `10 * 5`).
- Use `NumberFormatter` with `Windows.Globalization.NumberFormatting` types for locale-aware
  display (currency, percent, significant figures).
- `ValidationMode` can be `"InvalidInputOverwritten"` (clamps to range on commit, default),
  `"Disabled"` (no validation), or `"Enabled"` (shows error state).
- Prefer `NumberBox` over `TextBox` + manual parsing for all numeric inputs.
