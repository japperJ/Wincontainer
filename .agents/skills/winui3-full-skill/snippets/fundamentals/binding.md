# Data Binding

WinUI 3 supports two binding systems. Always prefer `x:Bind` (compiled binding); use
`{Binding}` only when reflection-based or dynamic binding is strictly necessary.

| Feature | `x:Bind` | `{Binding}` |
|---------|-----------|-------------|
| Compilation | Compile-time | Runtime reflection |
| Performance | Fast (no reflection) | Slower |
| Null safety | Compile errors for typos | Silent null failures |
| Default mode | `OneTime` | `OneWay` |
| `DataContext` | Uses `x:DataType` on the element | Uses `DataContext` hierarchy |
| Function calls | Yes (`{x:Bind ViewModel.Method(arg)}`) | No |

---

## One-time binding (default for x:Bind)

```xaml
<!-- Reads the value once at load — no change notifications needed -->
<TextBlock Text="{x:Bind ViewModel.AppVersion}" />
```

---

## One-way binding

```xaml
<!-- Page.xaml — x:DataType enables IntelliSense and compile-time checks -->
<Page
    x:Class="MyApp.ItemsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:vm="using:MyApp.ViewModels">

    <!-- x:DataType is required for x:Bind on the page root -->
    <Page.DataContext>
        <vm:ItemsViewModel />
    </Page.DataContext>

    <StackPanel>
        <!-- OneWay: UI updates when ViewModel property raises PropertyChanged -->
        <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
        <ProgressBar IsIndeterminate="{x:Bind ViewModel.IsLoading, Mode=OneWay}" />
    </StackPanel>
</Page>
```

```csharp
// ItemsPage.xaml.cs
using Microsoft.UI.Xaml.Controls;
using MyApp.ViewModels;

namespace MyApp;

public sealed partial class ItemsPage : Page
{
    public ItemsViewModel ViewModel { get; } = new ItemsViewModel();

    public ItemsPage()
    {
        InitializeComponent();
    }
}
```

```csharp
// ViewModels/ItemsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    [ObservableProperty]
    private string _title = "Items";

    [ObservableProperty]
    private bool _isLoading;
}
```

---

## Two-way binding (TwoWay)

```xaml
<!-- TwoWay: UI ↔ ViewModel synchronisation -->
<TextBox Text="{x:Bind ViewModel.SearchQuery, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}" />
<Slider Value="{x:Bind ViewModel.Volume, Mode=TwoWay}" />
<ToggleSwitch IsOn="{x:Bind ViewModel.IsDarkMode, Mode=TwoWay}" />
```

- `UpdateSourceTrigger=PropertyChanged` pushes changes on every keystroke (default is
  `LostFocus` for `TextBox`).

---

## x:DataType on DataTemplate

Every `DataTemplate` **must** declare `x:DataType` to enable compiled bindings inside it.

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ListView.ItemTemplate>
        <!-- x:DataType on the DataTemplate, not the ListView -->
        <DataTemplate x:DataType="models:Product">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Image Source="{x:Bind ThumbnailUrl}" Width="40" Height="40" />
                <StackPanel>
                    <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold" />
                    <TextBlock Text="{x:Bind Price, Converter={StaticResource CurrencyConverter}}" />
                </StackPanel>
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

```csharp
// Models/Product.cs
namespace MyApp.Models;

public class Product
{
    public string Name         { get; init; } = string.Empty;
    public decimal Price       { get; init; }
    public string ThumbnailUrl { get; init; } = string.Empty;
}
```

---

## Value converters

```csharp
// Converters/BoolToVisibilityConverter.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyApp.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility.Visible;
}
```

```xaml
<Page.Resources>
    <converters:BoolToVisibilityConverter x:Key="BoolToVisibilityConverter" />
</Page.Resources>

<StackPanel Visibility="{x:Bind ViewModel.HasItems, Mode=OneWay,
                          Converter={StaticResource BoolToVisibilityConverter}}" />
```

---

## Inline function binding (x:Bind only)

`x:Bind` can call methods on the page (or its data context) directly, eliminating simple
converters.

```xaml
<!-- Call a method defined in the page's code-behind -->
<TextBlock Text="{x:Bind FormatPrice(ViewModel.Price), Mode=OneWay}" />
<Border Visibility="{x:Bind GetVisibility(ViewModel.Count), Mode=OneWay}" />
```

```csharp
// ItemsPage.xaml.cs
public string FormatPrice(decimal price) => $"${price:F2}";

public Visibility GetVisibility(int count)
    => count > 0 ? Visibility.Visible : Visibility.Collapsed;
```

---

## Fallback and target null values

```xaml
<!-- Show "Unknown" when binding resolves to null or binding fails -->
<TextBlock Text="{x:Bind ViewModel.UserName, Mode=OneWay, FallbackValue='Unknown', TargetNullValue='(no name)'}" />
```

---

## {Binding} — when it is acceptable

```xaml
<!-- ElementName binding — x:Bind cannot reference elements by name across templates -->
<ParallaxView
    Source="{Binding ElementName=scrollViewer}"
    VerticalShift="50" />

<!-- Binding inside a DataTemplate when x:DataType is impractical (e.g., dynamic type) -->
<DataTemplate>
    <TextBlock Text="{Binding DisplayName}" />
</DataTemplate>
```

---

## ObservableCollection for list bindings

```csharp
// ViewModels/ItemsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    public ObservableCollection<string> Items { get; } = new();

    [RelayCommand]
    private void AddItem() => Items.Add($"Item {Items.Count + 1}");

    [RelayCommand]
    private void RemoveItem(string item) => Items.Remove(item);
}
```

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="x:String">
            <TextBlock Text="{x:Bind}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

---

## Notes

- `x:Bind` default mode is `OneTime`; always add `Mode=OneWay` or `Mode=TwoWay` for live
  data.
- `{Binding}` default mode is `OneWay`.
- `[ObservableProperty]` from CommunityToolkit.Mvvm generates the backing field and
  `PropertyChanged` notification automatically — no manual `SetProperty` call needed.
- Use `ObservableCollection<T>` (not `List<T>`) when the collection can change at runtime;
  `List<T>` does not raise change notifications so the UI will not update.
- Never access or modify UI elements from a background thread; use
  `DispatcherQueue.TryEnqueue` to marshal back to the UI thread.
