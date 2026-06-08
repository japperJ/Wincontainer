# SystemBackdrops (Mica / Desktop Acrylic)

System backdrops apply material effects to an entire app window — not just individual elements. Windows App SDK provides `MicaBackdrop` and `DesktopAcrylicBackdrop` for one-line setup, and the lower-level `MicaController` / `DesktopAcrylicController` for full customization.

---

## Mica Backdrop (Simplest — One Line in XAML)

```xaml
<!-- Window.xaml or MainWindow.xaml -->
<Window
    xmlns:muxc="using:Microsoft.UI.Xaml.Controls">
    <Window.SystemBackdrop>
        <MicaBackdrop />
    </Window.SystemBackdrop>

    <!-- App content -->
    <Frame x:Name="RootFrame" />
</Window>
```

---

## Desktop Acrylic Backdrop (One Line)

```xaml
<Window>
    <Window.SystemBackdrop>
        <DesktopAcrylicBackdrop />
    </Window.SystemBackdrop>

    <Frame x:Name="RootFrame" />
</Window>
```

---

## MicaController (Full Customization)

```csharp
// App.xaml.cs or MainWindow.xaml.cs
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using WinRT;

public sealed partial class MainWindow : Window
{
    private MicaController? _micaController;
    private SystemBackdropConfiguration? _backdropConfig;

    public MainWindow()
    {
        InitializeComponent();
        TrySetMicaBackdrop();
    }

    private void TrySetMicaBackdrop()
    {
        if (!MicaController.IsSupported())
            return; // Windows 10 fallback — use solid background

        _backdropConfig = new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = SystemBackdropTheme.Default,
        };

        _micaController = new MicaController
        {
            Kind = MicaKind.Base,           // or MicaKind.BaseAlt
            TintColor = Microsoft.UI.Colors.Transparent,
            TintOpacity = 0f,
            LuminosityOpacity = 1f,
        };

        // Connect the controller to this window
        _micaController.AddSystemBackdropTarget(
            this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
        _micaController.SetSystemBackdropConfiguration(_backdropConfig);

        // Keep theme in sync with the app theme
        ((FrameworkElement)Content).ActualThemeChanged += (s, e) =>
        {
            if (_backdropConfig is not null)
                _backdropConfig.Theme = ((FrameworkElement)Content).ActualTheme switch
                {
                    ElementTheme.Dark => SystemBackdropTheme.Dark,
                    ElementTheme.Light => SystemBackdropTheme.Light,
                    _ => SystemBackdropTheme.Default
                };
        };
    }
}
```

---

## DesktopAcrylicController (Full Customization)

```csharp
private void TrySetDesktopAcrylicBackdrop()
{
    if (!DesktopAcrylicController.IsSupported())
        return;

    _backdropConfig = new SystemBackdropConfiguration { IsInputActive = true };

    var acrylicController = new DesktopAcrylicController
    {
        Kind = DesktopAcrylicKind.Thin,     // Base (darker) or Thin (lighter)
        TintColor = Microsoft.UI.Colors.Black,
        TintOpacity = 0.1f,
        LuminosityOpacity = 0.9f,
        FallbackColor = Microsoft.UI.Colors.Gray,
    };

    acrylicController.AddSystemBackdropTarget(
        this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
    acrylicController.SetSystemBackdropConfiguration(_backdropConfig);
}
```

---

## Mica Alt (for Tabbed Title Bars)

```xaml
<Window>
    <Window.SystemBackdrop>
        <MicaBackdrop Kind="BaseAlt" />
    </Window.SystemBackdrop>
</Window>
```

---

## Notes

- `MicaBackdrop` and `DesktopAcrylicBackdrop` are available in Windows App SDK 1.3+.
- **Mica** requires Windows 11 build 22000+. `MicaController.IsSupported()` returns `false` on older builds — always check before enabling.
- **Desktop Acrylic** blurs the desktop and windows behind the app; `AcrylicBrush` (in-app) blurs content within the same window.
- `MicaKind.BaseAlt` uses stronger wallpaper tinting — recommended for apps with a tabbed title bar.
- `DesktopAcrylicKind.Thin` is lighter than `Base`; `Thin` is the preferred choice for most productivity apps.
- When using the controller APIs, always call `controller.Dispose()` in the window's `Closed` event to avoid leaks.
- The backdrop renders **behind** all XAML content — set `Background="Transparent"` on your root element to let it show through.
