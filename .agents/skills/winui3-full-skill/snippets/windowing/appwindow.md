# AppWindow

`AppWindow` (namespace `Microsoft.UI.Windowing`) is the Windows App SDK abstraction for a
top-level OS window. Every `Window` instance exposes its `AppWindow` property. Use it to
control size, position, title, icon, and presenter (windowing mode).

---

## Basic usage — resize, move, title, icon

```xaml
<!-- MainWindow.xaml — nothing special needed in XAML for basic AppWindow usage -->
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="8">
        <TextBlock Text="AppWindow demo" Style="{StaticResource TitleTextBlockStyle}" />
        <Button Content="Show sample window" Click="ShowSampleWindow_Click" />
    </StackPanel>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Set window title (shown in taskbar and title bar)
        AppWindow.Title = "My WinUI 3 App";

        // Set size (includes non-client area / borders)
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1024, 768));

        // Move to an absolute screen position
        AppWindow.Move(new Windows.Graphics.PointInt32(100, 100));

        // Set icon (affects both taskbar and title bar)
        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Keep title bar in sync with the app theme
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
    }

    private void ShowSampleWindow_Click(object sender, RoutedEventArgs e)
    {
        var win = new SampleWindow();
        win.Activate();
    }
}
```

---

## Center a window on screen

```csharp
// SampleWindow.xaml.cs
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.Graphics;

namespace MyApp;

public sealed partial class SampleWindow : Window
{
    public SampleWindow()
    {
        InitializeComponent();
        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        CenterOnScreen();
    }

    private void CenterOnScreen()
    {
        var area = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Nearest)?.WorkArea;
        if (area is null) return;

        AppWindow.Move(new PointInt32(
            (area.Value.Width  - AppWindow.Size.Width)  / 2,
            (area.Value.Height - AppWindow.Size.Height) / 2));
    }
}
```

---

## OverlappedPresenter — standard resizable window

`OverlappedPresenter` is the default presenter. Customize it to control chrome buttons,
borders, and size constraints.

```csharp
// CustomWindow.xaml.cs
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class CustomWindow : Window
{
    public CustomWindow()
    {
        InitializeComponent();

        AppWindow.SetIcon("Assets/AppIcon.ico");
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;

        var presenter = OverlappedPresenter.Create();

        presenter.IsAlwaysOnTop  = false;
        presenter.IsMaximizable  = true;
        presenter.IsMinimizable  = true;
        presenter.IsResizable    = true;
        // HasBorder must be true when HasTitleBar is true — fatal error otherwise
        presenter.SetBorderAndTitleBar(hasBorder: true, hasTitleBar: true);

        AppWindow.SetPresenter(presenter);

        // Listen for size changes (e.g., to update a Maximize/Restore button label)
        SizeChanged += OnSizeChanged;
    }

    private void OnSizeChanged(object sender, WindowSizeChangedEventArgs e)
    {
        if (AppWindow.Presenter is OverlappedPresenter p)
        {
            MaximizeRestoreButton.Content =
                p.State == OverlappedPresenterState.Maximized ? "Restore" : "Maximize";
        }
    }
}
```

### Min / max size constraints

```csharp
var presenter = OverlappedPresenter.Create();
presenter.PreferredMinimumWidth  = 400;
presenter.PreferredMinimumHeight = 400;
presenter.PreferredMaximumWidth  = 1000;
presenter.PreferredMaximumHeight = 1000;
// Disable maximize when a maximum size is set
presenter.IsMaximizable = false;
AppWindow.SetPresenter(presenter);
```

---

## FullScreenPresenter

```csharp
// Enter full screen
AppWindow.SetPresenter(FullScreenPresenter.Create());

// Exit full screen (restore to default overlapped)
AppWindow.SetPresenter(AppWindowPresenterKind.Default);
```

Handle the Escape key so users can always exit:

```csharp
protected override void OnKeyDown(KeyRoutedEventArgs e)
{
    if (e.Key == Windows.System.VirtualKey.Escape &&
        AppWindow.Presenter.Kind == AppWindowPresenterKind.FullScreen)
    {
        AppWindow.SetPresenter(AppWindowPresenterKind.Default);
        e.Handled = true;
    }
    base.OnKeyDown(e);
}
```

---

## CompactOverlayPresenter (Picture-in-Picture)

Keeps the window always on top at a small size — suitable for media players or floating tools.

```csharp
var presenter = CompactOverlayPresenter.Create();
// Size options: Small (~5 % of work area), Medium, Large
presenter.InitialSize = CompactOverlaySize.Small;
AppWindow.SetPresenter(presenter);
```

---

## Modal window

A modal window blocks interaction with its owner until closed. Unlike `ContentDialog`, it is
a fully independent `Window`.

```csharp
// Caller (e.g. MainWindow.xaml.cs)
private async void ShowModal_Click(object sender, RoutedEventArgs e)
{
    var modal = new ModalWindow();

    // Pass owner window ID so the OS enforces modal behaviour
    var ownerHwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
    WinRT.Interop.InitializeWithWindow.Initialize(modal, ownerHwnd);

    modal.Activate();

    // Disable owner while modal is open
    AppWindow.IsEnabled = false;
    modal.Closed += (_, _) => AppWindow.IsEnabled = true;
}
```

---

## Show / hide without closing

```csharp
// Hide (removes from taskbar visually, window stays alive)
AppWindow.Hide();

// Show again
AppWindow.Show();
```

---

## Notes

- `AppWindow.Resize` sets the **total** window size including borders. Use
  `AppWindow.ClientSize` to read the inner content area.
- `HasTitleBar = true` **requires** `HasBorder = true` with `OverlappedPresenter`; setting
  the combination incorrectly causes a fatal error at runtime.
- Use `DisplayArea.GetFromWindowId` (not `Screen`) for multi-monitor awareness.
- `AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode` keeps the system
  title bar in sync with the app's light/dark theme setting.
- To fully customize the title bar area (extend content, drag region, caption buttons colour),
  see the **TitleBar** snippet.
