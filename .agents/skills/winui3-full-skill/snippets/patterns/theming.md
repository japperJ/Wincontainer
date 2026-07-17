# App Patterns: Theming

WinUI 3 theming involves system backdrops (Mica/Acrylic), `ThemeResource` tokens,
`RequestedTheme`, and `ResourceDictionary` overrides.

---

## System Backdrops

### Mica (Windows 11 only)

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Composition.SystemBackdrops;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Mica (default, shows desktop wallpaper tint)
        if (MicaController.IsSupported())
            SystemBackdrop = new MicaBackdrop { Kind = MicaKind.Base };
    }
}
```

### Mica Alt (stronger tint — for side panels)

```csharp
if (MicaController.IsSupported())
    SystemBackdrop = new MicaBackdrop { Kind = MicaKind.BaseAlt };
```

### Acrylic (blur + tint, works on Windows 10+)

```csharp
SystemBackdrop = new DesktopAcrylicBackdrop();
```

---

## Light / Dark Toggle at Runtime

```csharp
// Apply theme to the root element
private static void ApplyTheme(ElementTheme theme)
{
    if (App.Current is App app &&
        app.MainWindow?.Content is FrameworkElement root)
    {
        root.RequestedTheme = theme;
    }
}

// Toggle
ApplyTheme(ElementTheme.Dark);
ApplyTheme(ElementTheme.Light);
ApplyTheme(ElementTheme.Default); // follow system
```

---

## ThemeResource Tokens (Standard Colors / Brushes)

Use `{ThemeResource}` instead of hardcoded colors to automatically adapt to light/dark.

```xaml
<!-- Foreground colors -->
<TextBlock Foreground="{ThemeResource TextFillColorPrimaryBrush}" />       <!-- Primary text -->
<TextBlock Foreground="{ThemeResource TextFillColorSecondaryBrush}" />     <!-- Secondary/hint text -->
<TextBlock Foreground="{ThemeResource TextFillColorDisabledBrush}" />      <!-- Disabled text -->
<TextBlock Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}" /> <!-- Accent-colored text -->

<!-- Background fills -->
<Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}" />         <!-- Card surface -->
<Border Background="{ThemeResource LayerFillColorDefaultBrush}" />                  <!-- Panel layer -->
<Border Background="{ThemeResource SolidBackgroundFillColorBaseBrush}" />           <!-- Page background -->
<Border Background="{ThemeResource ControlFillColorDefaultBrush}" />                <!-- Control fill -->
<Border Background="{ThemeResource AccentFillColorDefaultBrush}" />                 <!-- Accent fill -->

<!-- Stroke / border -->
<Border BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}" BorderThickness="1" />
<Border BorderBrush="{ThemeResource ControlStrokeColorDefaultBrush}" BorderThickness="1" />
```

---

## Custom Theme-Adaptive Resource (Light / Dark Override)

```xaml
<!-- In App.xaml or a merged ResourceDictionary -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Light">
                <SolidColorBrush x:Key="MyBrandBrush" Color="#0078D4" />
                <x:Double x:Key="MyPanelOpacity">0.9</x:Double>
            </ResourceDictionary>
            <ResourceDictionary x:Key="Dark">
                <SolidColorBrush x:Key="MyBrandBrush" Color="#60CDFF" />
                <x:Double x:Key="MyPanelOpacity">0.7</x:Double>
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Application.Resources>

<!-- Usage -->
<Border Background="{ThemeResource MyBrandBrush}"
        Opacity="{ThemeResource MyPanelOpacity}" />
```

---

## ViewModel-Driven Theme Toggle

```xaml
<!-- SettingsPage.xaml -->
<RadioButtons
    Header="App theme"
    SelectedIndex="{x:Bind ViewModel.ThemeIndex, Mode=TwoWay}">
    <x:String>System default</x:String>
    <x:String>Light</x:String>
    <x:String>Dark</x:String>
</RadioButtons>
```

```csharp
// ViewModels/SettingsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    [ObservableProperty]
    private int _themeIndex = 0; // 0 = Default, 1 = Light, 2 = Dark

    partial void OnThemeIndexChanged(int value)
    {
        var theme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };

        if (App.Current is App app &&
            app.MainWindow?.Content is FrameworkElement root)
        {
            root.RequestedTheme = theme;
        }
    }
}
```

---

## Acrylic In-App Brush (for panels / flyouts)

```xaml
<Border>
    <Border.Background>
        <AcrylicBrush
            TintColor="{ThemeResource SolidBackgroundFillColorBase}"
            TintOpacity="0.8"
            FallbackColor="{ThemeResource SolidBackgroundFillColorBase}" />
    </Border.Background>
</Border>
```

---

## Notes

- `MicaController.IsSupported()` returns `false` on Windows 10 and in VMs — always check
  before assigning `MicaBackdrop`.
- `DesktopAcrylicBackdrop` works on Windows 10 build 19041+ as a fallback.
- `RequestedTheme` on the root `FrameworkElement` overrides the system theme locally.
- Never hardcode `#FFFFFF` / `#000000` for UI colors; always use `{ThemeResource}` tokens.
- `ThemeResource` resolves at render time and re-resolves when the theme changes;
  `StaticResource` resolves once at load time.
