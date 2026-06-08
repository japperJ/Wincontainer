# Resources

WinUI 3 uses `ResourceDictionary` to store reusable values (brushes, colours, sizes,
strings, templates, styles) that are referenced throughout the app via `StaticResource` or
`ThemeResource`.

---

## Inline ResourceDictionary on a single page

```xaml
<!-- MyPage.xaml -->
<Page
    x:Class="MyApp.MyPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Page.Resources>
        <!-- Primitive values -->
        <x:Double x:Key="CardWidth">320</x:Double>
        <Thickness x:Key="CardPadding">16</Thickness>
        <x:String x:Key="AppName">My WinUI 3 App</x:String>

        <!-- Brush defined inline -->
        <SolidColorBrush x:Key="AccentBrush" Color="#0078D4" />

        <!-- CornerRadius -->
        <CornerRadius x:Key="CardCornerRadius">8</CornerRadius>
    </Page.Resources>

    <Border
        Width="{StaticResource CardWidth}"
        Padding="{StaticResource CardPadding}"
        Background="{StaticResource AccentBrush}"
        CornerRadius="{StaticResource CardCornerRadius}">
        <TextBlock Text="{StaticResource AppName}" Foreground="White" />
    </Border>
</Page>
```

---

## App-wide ResourceDictionary in App.xaml

Resources declared in `App.xaml` are available to every page and control in the app.

```xaml
<!-- App.xaml -->
<Application
    x:Class="MyApp.App"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Application.Resources>
        <ResourceDictionary>
            <!-- Merge WinUI 3 default theme resources first -->
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <!-- App-specific dictionaries -->
                <ResourceDictionary Source="/Themes/Colors.xaml" />
                <ResourceDictionary Source="/Themes/Fonts.xaml" />
            </ResourceDictionary.MergedDictionaries>

            <!-- App-wide overrides -->
            <x:Double x:Key="AppHeaderFontSize">28</x:Double>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## Separate ResourceDictionary file

```xaml
<!-- Themes/Colors.xaml -->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Color x:Key="BrandPrimary">#0078D4</Color>
    <Color x:Key="BrandSecondary">#005A9E</Color>

    <SolidColorBrush x:Key="BrandPrimaryBrush"  Color="{StaticResource BrandPrimary}" />
    <SolidColorBrush x:Key="BrandSecondaryBrush" Color="{StaticResource BrandSecondary}" />
</ResourceDictionary>
```

---

## ThemeResource vs StaticResource

| Keyword | When resource is resolved | Use case |
|---------|--------------------------|----------|
| `StaticResource` | Once at load time | Immutable values (sizes, corner radii) |
| `ThemeResource` | At load time **and** whenever the app theme changes | Brushes, colours that must adapt to light/dark/high-contrast |

```xaml
<!-- Correct: brush adapts to light/dark -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" />

<!-- Correct: corner radius never changes with theme -->
<Border CornerRadius="{StaticResource ControlCornerRadius}" />
```

---

## Theme dictionaries (light / dark / high-contrast)

```xaml
<!-- Themes/BrandBrushes.xaml -->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Default">
            <!-- Dark theme values -->
            <SolidColorBrush x:Key="MyCardBrush" Color="#1A1A2E" />
            <SolidColorBrush x:Key="MyTextBrush"  Color="#E0E0E0" />
        </ResourceDictionary>
        <ResourceDictionary x:Key="Light">
            <SolidColorBrush x:Key="MyCardBrush" Color="#F5F5F5" />
            <SolidColorBrush x:Key="MyTextBrush"  Color="#1A1A1A" />
        </ResourceDictionary>
        <ResourceDictionary x:Key="HighContrast">
            <SolidColorBrush x:Key="MyCardBrush" Color="{ThemeResource SystemColorWindowColor}" />
            <SolidColorBrush x:Key="MyTextBrush"  Color="{ThemeResource SystemColorWindowTextColor}" />
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</ResourceDictionary>
```

```xaml
<!-- Usage — resolves from the active theme dictionary -->
<Border Background="{ThemeResource MyCardBrush}">
    <TextBlock Foreground="{ThemeResource MyTextBrush}" Text="Themed card" />
</Border>
```

---

## Accessing resources from C#

```csharp
// Read a resource value from the application dictionary
if (Application.Current.Resources.TryGetValue("AppHeaderFontSize", out var value)
    && value is double fontSize)
{
    myTextBlock.FontSize = fontSize;
}

// Read from a specific element's resource dictionary
var brush = (SolidColorBrush)Resources["AccentBrush"];
```

---

## Overriding built-in WinUI 3 resources

Look up the original key in the WinUI 3 source (e.g., `generic.xaml`) and redeclare it in
`App.xaml` or a merged dictionary:

```xaml
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
        </ResourceDictionary.MergedDictionaries>

        <!-- Override the default button corner radius app-wide -->
        <CornerRadius x:Key="ControlCornerRadius">4</CornerRadius>
    </ResourceDictionary>
</Application.Resources>
```

---

## Notes

- Always merge `XamlControlsResources` **first** in `MergedDictionaries`; app-specific
  overrides must come **after** it to take effect.
- `ThemeResource` brushes automatically update when the user switches theme at runtime —
  use them for all colour and brush values.
- Use `StaticResource` for non-colour values (sizes, thickness, corner radii) that don't
  change with the theme.
- `ResourceDictionary` entries are looked up from the nearest scope (element → page → app).
  Local resources shadow outer ones with the same key.
- For large apps, split resources into purpose-specific files (`Colors.xaml`, `Fonts.xaml`,
  `Styles.xaml`) merged in `App.xaml` to keep files manageable.
