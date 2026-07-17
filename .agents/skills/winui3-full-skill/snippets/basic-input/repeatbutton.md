# RepeatButton

`RepeatButton` fires its `Click` event repeatedly while the button is pressed and held.
Use it for increment/decrement controls, scroll arrows, or any action that should repeat
at a set interval as long as the button is depressed.

---

## Basic RepeatButton

```xaml
<RepeatButton
    Content="Click and hold"
    Click="IncrementButton_Click"
    AutomationProperties.Name="Increment value" />
```

```csharp
// MainPage.xaml.cs
private int _count = 0;

private void IncrementButton_Click(object sender, RoutedEventArgs e)
{
    _count++;
    CountDisplay.Text = _count.ToString();
}
```

---

## Increment / Decrement Pair

```xaml
<StackPanel Orientation="Horizontal" Spacing="8" VerticalAlignment="Center">
    <RepeatButton
        Content="-"
        Width="40"
        Click="Decrement_Click"
        AutomationProperties.Name="Decrease quantity" />
    <TextBlock
        x:Name="QuantityDisplay"
        Width="32"
        Text="0"
        VerticalAlignment="Center"
        TextAlignment="Center"
        AutomationProperties.Name="Current quantity" />
    <RepeatButton
        Content="+"
        Width="40"
        Click="Increment_Click"
        AutomationProperties.Name="Increase quantity" />
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private int _quantity = 0;

private void Increment_Click(object sender, RoutedEventArgs e)
{
    _quantity = Math.Min(99, _quantity + 1);
    QuantityDisplay.Text = _quantity.ToString();
}

private void Decrement_Click(object sender, RoutedEventArgs e)
{
    _quantity = Math.Max(0, _quantity - 1);
    QuantityDisplay.Text = _quantity.ToString();
}
```

---

## MVVM — Bound to Commands

```xaml
<!-- View -->
<StackPanel Orientation="Horizontal" Spacing="8">
    <RepeatButton
        Content="-"
        Width="40"
        Command="{x:Bind ViewModel.DecrementCommand}"
        AutomationProperties.Name="Decrease" />
    <TextBlock
        Width="40"
        Text="{x:Bind ViewModel.Count, Mode=OneWay}"
        VerticalAlignment="Center"
        TextAlignment="Center" />
    <RepeatButton
        Content="+"
        Width="40"
        Command="{x:Bind ViewModel.IncrementCommand}"
        AutomationProperties.Name="Increase" />
</StackPanel>
```

```csharp
// ViewModels/CounterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class CounterViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DecrementCommand))]
    private int _count = 0;

    [RelayCommand]
    private void Increment() => Count++;

    [RelayCommand(CanExecute = nameof(CanDecrement))]
    private void Decrement() => Count--;

    private bool CanDecrement() => Count > 0;
}
```

---

## Notes

- `Delay` (ms) — initial pause before repetition begins (default: 400 ms).
- `Interval` (ms) — time between repeated firings while held (default: 100 ms).
- `RepeatButton` is not in the default WinUI 3 namespace; it is in
  `Microsoft.UI.Xaml.Controls.Primitives`. Add `xmlns:primitives="using:Microsoft.UI.Xaml.Controls.Primitives"`
  if referencing it from that namespace, though it is accessible as `RepeatButton` directly in XAML.
- For a numeric input field with spin buttons, prefer `NumberBox` over a manual RepeatButton pair.
