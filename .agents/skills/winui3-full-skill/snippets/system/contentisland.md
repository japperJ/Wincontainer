# ContentIsland

`ContentIsland` (Windows App SDK 1.5+) allows embedding XAML UI into non-XAML hosting environments — Win32 windows, WebView2 iframes, or custom compositor surfaces. It is the low-level building block behind `DesktopWindowXamlSource`.

> **Note:** Most apps do not need `ContentIsland` directly. Use `DesktopWindowXamlSource` for hosting XAML in a Win32 HWND, or use the standard WinUI 3 `Window` / `Page` model. `ContentIsland` is for advanced hosting scenarios.

---

## Basic ContentIsland Setup in a Win32 App

```csharp
// Program.cs (Win32 app with a HWND)
using Microsoft.UI.Content;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;

// 1. Initialize the WinUI 3 XAML framework (unpackaged)
var manager = WindowsAppSDK.AppLifecycle.AppInstance.GetCurrent();

// 2. Create the ContentIsland
var compositor = new Microsoft.UI.Composition.Compositor();
var island = ContentIsland.Create(compositor);

// 3. Set the XAML root content
island.Root = new Button { Content = "Hello from ContentIsland" };

// 4. Connect to a Win32 HWND (e.g. via XAML island bridge)
// island.Connect(hwnd); // platform-specific
```

---

## DesktopWindowXamlSource (Preferred for Win32 Hosting)

For hosting WinUI 3 XAML inside a classic Win32 HWND, use `DesktopWindowXamlSource`:

```csharp
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Controls;

public class XamlIslandHost
{
    private DesktopWindowXamlSource? _xamlSource;

    public void Initialize(nint parentHwnd)
    {
        _xamlSource = new DesktopWindowXamlSource();

        // Connect to the Win32 HWND
        var interop = _xamlSource.As<IDesktopWindowXamlSourceNative>();
        interop.AttachToWindow(parentHwnd);

        nint childHwnd;
        interop.GetWindowHandle(out childHwnd);

        // Size the child HWND
        SetWindowPos(childHwnd, nint.Zero, 0, 0, 800, 600,
            SWP_SHOWWINDOW);

        // Set the root XAML content
        _xamlSource.Content = new MyXamlUserControl();
    }

    public void Dispose()
    {
        _xamlSource?.Dispose();
    }
}
```

---

## Programmatic Navigation via ContentIsland

```csharp
// Navigate to a different view without a Frame
island.Root = new SettingsView();
```

---

## ContentIsland in a Custom Compositor Scenario

```csharp
using Microsoft.UI.Content;
using Microsoft.UI.Composition;

private ContentIsland? _island;

public void CreateIsland(Compositor compositor)
{
    _island = ContentIsland.Create(compositor);

    // Assign XAML content
    _island.Root = new StackPanel
    {
        Children =
        {
            new TextBlock { Text = "Island Content" },
            new Button { Content = "Click Me" }
        }
    };

    // Connect to the visual tree
    // parentVisual.Children.InsertAtTop(_island.Visual);
}
```

---

## Key Types

| Type | Namespace | Purpose |
|---|---|---|
| `ContentIsland` | `Microsoft.UI.Content` | Raw island for Composition hosting |
| `DesktopWindowXamlSource` | `Microsoft.UI.Xaml.Hosting` | Host XAML in a Win32 HWND |
| `XamlIsland` | `Microsoft.UI.Xaml.Hosting` | Higher-level island wrapper |
| `IDesktopWindowXamlSourceNative` | COM interop | Attach to parent HWND |

---

## Notes

- `ContentIsland` requires **Windows App SDK 1.5+**.
- For standard WinUI 3 apps using `Window` and `Page`, you do **not** need `ContentIsland` — it is for Win32 interop and custom hosting scenarios.
- `DesktopWindowXamlSource` is the recommended API for hosting XAML in classic Win32 applications.
- `ContentIsland` is a low-level API; prefer `DesktopWindowXamlSource` unless you need direct compositor integration.
- Tab navigation between XAML island and native Win32 controls requires manual focus routing via `DesktopWindowXamlSource.NavigateFocus()`.
- WPF and WinForms apps hosting WinUI 3 content use `ContentIsland` / `DesktopWindowXamlSource` bridges.
