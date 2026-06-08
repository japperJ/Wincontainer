# Multiple Windows

WinUI 3 supports multiple independent top-level windows within a single process. Each window
runs on the UI thread of the thread that created it. The recommended pattern is to create
additional windows on the **main UI thread** (simplest) or on a **dedicated UI thread** per
window (for true isolation).

---

## Open a second window from the main window

```xaml
<!-- MainWindow.xaml -->
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="12">
        <TextBlock Text="Main Window" Style="{StaticResource TitleTextBlockStyle}" />
        <Button Content="Open second window"  Click="OpenSecondWindow_Click" />
        <Button Content="Open settings window" Click="OpenSettings_Click" />
    </StackPanel>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    // Keep a reference so the window is not GC-collected
    private SecondWindow? _secondWindow;

    public MainWindow()
    {
        InitializeComponent();
        AppWindow.Title = "Main Window";
    }

    private void OpenSecondWindow_Click(object sender, RoutedEventArgs e)
    {
        if (_secondWindow is null)
        {
            _secondWindow = new SecondWindow();
            _secondWindow.Closed += (_, _) => _secondWindow = null;
        }
        _secondWindow.Activate();
    }

    private void OpenSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.Activate();
    }
}
```

---

## SecondWindow definition

```xaml
<!-- SecondWindow.xaml -->
<Window
    x:Class="MyApp.SecondWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <StackPanel Padding="24" Spacing="8">
        <TextBlock Text="Second Window" Style="{StaticResource TitleTextBlockStyle}" />
        <Button Content="Close" Click="Close_Click" />
    </StackPanel>
</Window>
```

```csharp
// SecondWindow.xaml.cs
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class SecondWindow : Window
{
    public SecondWindow()
    {
        InitializeComponent();
        AppWindow.Title  = "Second Window";
        AppWindow.Resize(new Windows.Graphics.SizeInt32(640, 480));
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
```

---

## Window on a dedicated UI thread

For true isolation (each window has its own dispatcher and message pump) create the window
on a new `DispatcherQueue` thread. This is the pattern used for media or CPU-intensive
secondary windows.

```csharp
// WindowManager.cs
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using System.Threading;

namespace MyApp;

public static class WindowManager
{
    /// <summary>
    /// Creates and activates a new Window of type <typeparamref name="T"/>
    /// on its own dedicated UI thread.
    /// </summary>
    public static void OpenOnNewThread<T>() where T : Window, new()
    {
        // STA thread with its own DispatcherQueue
        var thread = new Thread(() =>
        {
            // Create a DispatcherQueue for this thread
            var controller = DispatcherQueueController.CreateOnCurrentThread();

            var window = new T();
            window.Activate();

            // Run the message pump until the window is closed
            window.Closed += (_, _) => controller.ShutdownQueue();
            controller.DispatcherQueue.RunEventLoop();
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();
    }
}
```

```csharp
// Usage in MainWindow.xaml.cs
private void OpenOnNewThread_Click(object sender, RoutedEventArgs e)
{
    WindowManager.OpenOnNewThread<SecondWindow>();
}
```

---

## Communicating between windows

Windows on the **same** UI thread can communicate directly via shared objects or events.
Windows on **different** threads must use `DispatcherQueue.TryEnqueue` to marshal calls.

```csharp
// Shared event bus (same-thread scenario)
public static class EventBus
{
    public static event EventHandler<string>? MessageSent;
    public static void Send(string message) => MessageSent?.Invoke(null, message);
}

// Sender (MainWindow)
EventBus.Send("Hello from main window");

// Receiver (SecondWindow) — safe because both windows share the same UI thread
public SecondWindow()
{
    InitializeComponent();
    EventBus.MessageSent += (_, msg) => StatusText.Text = msg;
}
```

```csharp
// Cross-thread communication — receiver runs on a different DispatcherQueue
public SecondWindow()
{
    InitializeComponent();

    // Capture this window's DispatcherQueue
    var dq = DispatcherQueue.GetForCurrentThread();

    EventBus.MessageSent += (_, msg) =>
        dq.TryEnqueue(() => StatusText.Text = msg);
}
```

---

## Passing data to a new window

Prefer constructor parameters or a ViewModel injected via DI.

```csharp
// DetailWindow.xaml.cs
public sealed partial class DetailWindow : Window
{
    public DetailWindow(MyItem item)
    {
        InitializeComponent();
        AppWindow.Title = $"Detail — {item.Name}";
        ViewModel = new DetailViewModel(item);
        DataContext = ViewModel; // if using {Binding}; prefer x:Bind
    }

    public DetailViewModel ViewModel { get; }
}

// Caller
private void OpenDetail_Click(object sender, RoutedEventArgs e)
{
    var win = new DetailWindow(selectedItem);
    win.Activate();
}
```

---

## Notes

- Always store a reference to secondary windows (field / list) to prevent garbage collection
  while the window is open.
- `Window.Activate()` brings the window to the foreground and shows it; call it after
  `InitializeComponent()`.
- Do **not** share UI objects (`UIElement`, `FrameworkElement`) across threads — each window's
  UI elements belong to the thread that created them.
- Use `DispatcherQueue.TryEnqueue` (not `Dispatcher.Invoke`) to marshal work to another
  window's UI thread.
- For modal behaviour (blocking owner until closed), see the **AppWindow** snippet —
  `AppWindow.IsEnabled = false` on the owner.
- Each `Window` instance that needs an `IServiceProvider` should resolve its ViewModel at
  the composition root (`App.xaml.cs`) and pass it through the constructor rather than using
  a static service locator.
