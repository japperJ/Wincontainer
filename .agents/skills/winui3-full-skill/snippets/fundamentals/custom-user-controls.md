# Custom User Controls

A **UserControl** is a reusable piece of UI with its own XAML file, code-behind, and
(optionally) dependency properties. Use it to encapsulate repeated UI patterns, keeping
pages clean and enabling reuse across the app.

---

## Minimal UserControl

```xaml
<!-- Controls/StatusBadge.xaml -->
<UserControl
    x:Class="MyApp.Controls.StatusBadge"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Border
        x:Name="Root"
        CornerRadius="999"
        Padding="8,4"
        Background="{ThemeResource AccentFillColorDefaultBrush}">
        <TextBlock
            x:Name="LabelText"
            Style="{StaticResource CaptionTextBlockStyle}"
            Foreground="{ThemeResource TextOnAccentFillColorPrimaryBrush}" />
    </Border>
</UserControl>
```

```csharp
// Controls/StatusBadge.xaml.cs
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Controls;

public sealed partial class StatusBadge : UserControl
{
    public StatusBadge()
    {
        InitializeComponent();
    }
}
```

---

## Adding DependencyProperties

`DependencyProperty` is the WinUI 3 / WPF property system used for data binding, animation,
and style setters on custom controls.

```csharp
// Controls/StatusBadge.xaml.cs
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MyApp.Controls;

public sealed partial class StatusBadge : UserControl
{
    // ── Label ─────────────────────────────────────────────────────────────
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(StatusBadge),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StatusBadge)d;
        control.LabelText.Text = (string)e.NewValue;
    }

    // ── BadgeColor ────────────────────────────────────────────────────────
    public static readonly DependencyProperty BadgeColorProperty =
        DependencyProperty.Register(
            nameof(BadgeColor),
            typeof(Brush),
            typeof(StatusBadge),
            new PropertyMetadata(null, OnBadgeColorChanged));

    public Brush BadgeColor
    {
        get => (Brush)GetValue(BadgeColorProperty);
        set => SetValue(BadgeColorProperty, value);
    }

    private static void OnBadgeColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (StatusBadge)d;
        control.Root.Background = (Brush)e.NewValue;
    }

    public StatusBadge()
    {
        InitializeComponent();
    }
}
```

Usage:

```xaml
<!-- MyPage.xaml -->
<Page
    xmlns:controls="using:MyApp.Controls">

    <controls:StatusBadge
        Label="Active"
        BadgeColor="{ThemeResource AccentFillColorDefaultBrush}" />

    <controls:StatusBadge
        Label="Error"
        BadgeColor="{ThemeResource SystemFillColorCriticalBackgroundBrush}" />
</Page>
```

---

## Binding DependencyProperties with x:Bind inside the UserControl

Use `{x:Bind}` inside the UserControl's own XAML by referencing `this` as the data source.

```xaml
<!-- Controls/ProductCard.xaml -->
<UserControl
    x:Class="MyApp.Controls.ProductCard"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Name="this">

    <Border
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
        BorderThickness="1"
        CornerRadius="8"
        Padding="16">
        <StackPanel Spacing="4">
            <TextBlock
                Text="{x:Bind this.Title, Mode=OneWay}"
                Style="{StaticResource BodyStrongTextBlockStyle}" />
            <TextBlock
                Text="{x:Bind this.Description, Mode=OneWay}"
                TextWrapping="WrapWholeWords"
                Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
        </StackPanel>
    </Border>
</UserControl>
```

```csharp
// Controls/ProductCard.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Controls;

public sealed partial class ProductCard : UserControl
{
    public static readonly DependencyProperty TitleProperty =
        DependencyProperty.Register(nameof(Title), typeof(string),
            typeof(ProductCard), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty DescriptionProperty =
        DependencyProperty.Register(nameof(Description), typeof(string),
            typeof(ProductCard), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public ProductCard()
    {
        InitializeComponent();
    }
}
```

---

## Routed events — raising a custom event from a UserControl

```csharp
// Controls/SearchBar.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Controls;

public sealed partial class SearchBar : UserControl
{
    // Custom event — raised when the user submits a query
    public event EventHandler<string>? QuerySubmitted;

    public static readonly DependencyProperty PlaceholderProperty =
        DependencyProperty.Register(nameof(Placeholder), typeof(string),
            typeof(SearchBar), new PropertyMetadata("Search…"));

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public SearchBar()
    {
        InitializeComponent();
    }

    private void SearchBox_QuerySubmitted(AutoSuggestBox sender,
        AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        QuerySubmitted?.Invoke(this, args.QueryText);
    }
}
```

```xaml
<!-- Controls/SearchBar.xaml -->
<UserControl
    x:Class="MyApp.Controls.SearchBar"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Name="this">

    <AutoSuggestBox
        PlaceholderText="{x:Bind this.Placeholder, Mode=OneWay}"
        QueryIcon="Find"
        QuerySubmitted="SearchBox_QuerySubmitted" />
</UserControl>
```

Consumer:

```xaml
<controls:SearchBar
    Placeholder="Search products…"
    QuerySubmitted="SearchBar_QuerySubmitted" />
```

```csharp
private void SearchBar_QuerySubmitted(object sender, string query)
{
    ViewModel.SearchCommand.Execute(query);
}
```

---

## UserControl with an injected ViewModel

When a UserControl needs its own ViewModel (not inherited from the parent), inject it
via a constructor parameter or a DependencyProperty.

```csharp
// Controls/RecentFilesPanel.xaml.cs
using Microsoft.UI.Xaml.Controls;
using MyApp.ViewModels;

namespace MyApp.Controls;

public sealed partial class RecentFilesPanel : UserControl
{
    public RecentFilesPanelViewModel ViewModel { get; }

    public RecentFilesPanel(RecentFilesPanelViewModel viewModel)
    {
        ViewModel = viewModel;
        InitializeComponent();
    }
}
```

```xaml
<!-- Controls/RecentFilesPanel.xaml -->
<UserControl
    x:Class="MyApp.Controls.RecentFilesPanel"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    x:Name="this">

    <ListView ItemsSource="{x:Bind this.ViewModel.RecentFiles, Mode=OneWay}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="x:String">
                <TextBlock Text="{x:Bind}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</UserControl>
```

---

## Notes

- Name the `UserControl` element `x:Name="this"` in XAML so that `{x:Bind this.MyProperty}`
  works without ambiguity.
- `DependencyProperty` fields must be `static readonly`; the backing CLR property simply
  calls `GetValue`/`SetValue`.
- Use `PropertyMetadata` callbacks (`OnXxxChanged`) instead of overriding the CLR property
  setter to react to value changes — the CLR setter is not always called (e.g., animation,
  style setters).
- Keep business logic out of UserControls; they should be purely presentational. Bind them
  to ViewModel properties exposed via `DependencyProperty`.
- For complex controls that need visual states, `ControlTemplate` customisation, or
  templated children, prefer a `TemplatedControl` (`Control` subclass) over a `UserControl`.
