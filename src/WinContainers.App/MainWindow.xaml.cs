using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Graphics;
using WinContainers.Core;
using WinContainers_App.Pages;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;
using LogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App;

public sealed partial class MainWindow : Window
{
    public static MainWindow? Instance { get; private set; }

    public static int ReturnToPivotIndex { get; set; } = -1;

    private readonly INavigationService _navigation;
    private readonly IOutputService _output;
    private nint _mainHwnd;

    public MainWindow()
    {
        InitializeComponent();
        Instance = this;

        _navigation = ViewModelLocator.NavigationService;
        _output = ViewModelLocator.OutputService;

        AppWindow.SetIcon("Assets/AppIcon.ico");

        var hwnd = Win32Interop.GetWindowFromWindowId(AppWindow.Id);
        var dpi = GetDpiForWindow(hwnd);
        var scale = dpi / 96.0;
        AppWindow.Resize(new SizeInt32((int)(1320 * scale), (int)(920 * scale)));

        _navigation.SetFrame(RootFrame);

        _output.OutputWritten += OnOutputWritten;

        RootNavigation.SelectedItem = RootNavigation.MenuItems.OfType<NavigationViewItem>().First();
        NavigateTo("Dashboard");

        _mainHwnd = hwnd;

        AppWindow.Closing += (_, args) =>
        {
            args.Cancel = true;
            ShowWindow(hwnd, SW_HIDE);
            _output.Write("WinContainers minimized to tray. Right-click the tray icon to exit.", LogLevel.Info);
        };

        TrayService.ShowWindowRequested += () => DispatcherQueue.TryEnqueue(() =>
        {
            ShowWindow(_mainHwnd, SW_SHOW);
            Activate();
        });

        TrayService.ExitRequested += () => DispatcherQueue.TryEnqueue(() =>
        {
            TrayService.Stop();
            Application.Current.Exit();
        });

        TrayService.Start();
    }

    [DllImport("user32.dll")]
    private static extern uint GetDpiForWindow(IntPtr hWnd);

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private LogLevel _minLogLevel = LogLevel.Info;

    private void OnOutputWritten(object? sender, OutputWrittenEventArgs e)
    {
        if (e.Level < _minLogLevel) return;

        DispatcherQueue.TryEnqueue(() =>
        {
            var prefix = OutputTextBlock.Text.Length > 0 ? "\n" : "";
            OutputTextBlock.Text += $"{prefix}{e.Message}";
            OutputTabText.Text = $"Last output: {DateTime.Now:HH:mm:ss}";
            OutputScrollViewer.ChangeView(null, OutputScrollViewer.ScrollableHeight, null);
        });
    }

    private void LogLevelFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _minLogLevel = (LogLevelFilter.SelectedItem as string) switch
        {
            "All" => LogLevel.Debug,
            "Warnings" => LogLevel.Warning,
            _ => LogLevel.Info
        };
    }

    private void RootNavigation_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
            NavigateTo(tag);
    }

    private void NavigateTo(string tag)
    {
        Type pageType = tag switch
        {
            "Dashboard" => typeof(DashboardPage),
            "Terminal" => typeof(TerminalPage),
            "Images" => typeof(ImagesPage),
            "Settings" => typeof(SettingsPage),
            _ => typeof(DashboardPage)
        };

        // Show/hide navigation based on page type
        RootNavigation.Visibility = pageType == typeof(OnboardingPage)
            ? Visibility.Collapsed
            : Visibility.Visible;

        try
        {
            RootFrame.Navigate(pageType);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(
                System.IO.Path.Combine(System.IO.Path.GetTempPath(), "WinContainers-nav-error.log"),
                $"{DateTime.Now:O} NavigateTo({tag}): {ex}\n{ex.StackTrace}");
            throw;
        }
    }

    public void NavigateToPage(Type pageType, object? parameter = null)
        => RootFrame.Navigate(pageType, parameter);

    public void NavigateToMainContent()
    {
        RootNavigation.Visibility = Visibility.Visible;
        NavigateTo("Dashboard");
    }

    public void NavigateBack()
    {
        if (RootFrame.CanGoBack)
            RootFrame.GoBack();
    }

    private enum OutputPaneState { Collapsed, Expanded }
    private OutputPaneState _outputPaneState = OutputPaneState.Collapsed;

    private bool _isDraggingSplitter;
    private double _dragStartY;
    private double _totalHeight;
    private double _lastContentHeight;

    private void ToggleBottomPaneButton_Click(object sender, RoutedEventArgs e)
    {
        _outputPaneState = _outputPaneState == OutputPaneState.Collapsed
            ? OutputPaneState.Expanded
            : OutputPaneState.Collapsed;

        switch (_outputPaneState)
        {
            case OutputPaneState.Collapsed:
                ContentRow.Height = new GridLength(1, GridUnitType.Star);
                BottomContentRow.Height = new GridLength(0);
                OutputScrollViewer.Visibility = Visibility.Collapsed;
                ToggleBottomPaneButton.Content = "Show output";
                ToolTipService.SetToolTip(ToggleBottomPaneButton, "Show output pane");
                break;
            case OutputPaneState.Expanded:
                ContentRow.Height = new GridLength(1, GridUnitType.Star);
                BottomContentRow.Height = new GridLength(240);
                OutputScrollViewer.Visibility = Visibility.Visible;
                ToggleBottomPaneButton.Content = "Hide output";
                ToolTipService.SetToolTip(ToggleBottomPaneButton, "Hide output pane");
                break;
        }
    }

    private void OutputSplitter_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter = true;
        _dragStartY = e.GetCurrentPoint(null).Position.Y;
        _totalHeight = AppWindow.Size.Height - 8;
        _lastContentHeight = ContentRow.ActualHeight;
        OutputSplitter.CapturePointer(e.Pointer);
    }

    private void OutputSplitter_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDraggingSplitter) return;

        var delta = e.GetCurrentPoint(null).Position.Y - _dragStartY;
        var desiredContent = _lastContentHeight + delta;
        var contentPixels = Math.Max(240, Math.Min(_totalHeight - 180, desiredContent));
        var bottomPixels = Math.Max(140, Math.Min(_totalHeight - 240, _totalHeight - contentPixels));

        ContentRow.Height = new GridLength(contentPixels);
        BottomContentRow.Height = new GridLength(bottomPixels);
        _outputPaneState = OutputPaneState.Expanded;
        ToggleBottomPaneButton.Content = "\uE011";
        ToolTipService.SetToolTip(ToggleBottomPaneButton, "Expand output pane");
    }

    private void OutputSplitter_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _isDraggingSplitter = false;
        OutputSplitter.ReleasePointerCapture(e.Pointer);
    }

    private void ClearOutputButton_Click(object sender, RoutedEventArgs e)
    {
        OutputTextBlock.Text = string.Empty;
        OutputTabText.Text = "Cleared";
        _output.Clear();
    }

    private async void HelpButton_Click(object sender, RoutedEventArgs e)
    {
        var flyout = new Flyout
        {
            Placement = FlyoutPlacementMode.RightEdgeAlignedTop
        };

        var stack = new StackPanel
        {
            Spacing = 8,
            Padding = new Thickness(8)
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Help & Diagnostics",
            Style = Application.Current.Resources["SubtitleTextBlockStyle"] as Style ?? new Style()
        });

        var versionBtn = new Button { Content = "Check WSLC version" };
        versionBtn.Click += async (_, _) =>
        {
            EnsureOutputPaneVisible();
            _output.Write("Checking WSLC version...");
            var version = await App.ServiceClient.GetVersionAsync();
            _output.Write($"WSLC: {WslcVersionFormatter.Format(version)}");
        };
        stack.Children.Add(versionBtn);

        var wslStatusBtn = new Button { Content = "Check WSL status" };
        wslStatusBtn.Click += async (_, _) =>
        {
            EnsureOutputPaneVisible();
            _output.Write("Checking WSL status...");
            var status = await GetWslStatusAsync();
            foreach (var line in status)
            {
                _output.Write($"  {line}");
            }
        };
        stack.Children.Add(wslStatusBtn);

        flyout.Content = stack;
        flyout.ShowAt(HelpButton);
    }

    private static async Task<List<string>> GetWslStatusAsync()
    {
        var statusResult = await RunCommandAsync("wsl.exe", "--status", timeoutSeconds: 15);
        var versionResult = await RunCommandAsync("wsl.exe", "--version", timeoutSeconds: 15);

        var allLines = new List<string>();

        if (statusResult.ExitCode == 0)
        {
            var output = NormalizeCommandOutput(statusResult.Output);
            var statusLines = ExtractInterestingLines(output, new[]
            {
                "Default Distribution",
                "Default Version"
            });
            allLines.AddRange(statusLines);
        }

        if (versionResult.ExitCode == 0)
        {
            var output = NormalizeCommandOutput(versionResult.Output);
            var versionLines = ExtractInterestingLines(output, new[]
            {
                "WSL version",
                "WSLg version",
                "MSRDC version",
                "Direct3D version",
                "DXCore version",
                "Windows version",
                "Kernel version"
            });
            allLines.AddRange(versionLines);
        }

        if (allLines.Count > 0)
        {
            return allLines;
        }

        var error = string.IsNullOrWhiteSpace(statusResult.Output)
            ? "WSL is not installed or `wsl --status` failed."
            : statusResult.Output;
        return new List<string> { error };
    }

    private static List<string> ExtractInterestingLines(string output, string[] keywords)
    {
        return output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => keywords.Any(keyword => line.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static async Task<(int ExitCode, string Output)> RunCommandAsync(string fileName, string arguments, int timeoutSeconds)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.WriteLine($"[MainWindow] Timed-out process already exited: {ex.Message}");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                Debug.WriteLine($"[MainWindow] Failed to kill timed-out process: {ex.Message}");
            }

            return (-1, $"Command timed out after {timeoutSeconds} seconds.");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        var combined = string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}";
        return (process.ExitCode, combined.Trim());
    }

    private static string NormalizeCommandOutput(string output) =>
        output.Replace("\0", string.Empty, StringComparison.Ordinal).Trim();

    public void EnsureOutputPaneVisible()
    {
        if (_outputPaneState == OutputPaneState.Expanded)
        {
            return;
        }

        _outputPaneState = OutputPaneState.Expanded;
        ContentRow.Height = new GridLength(1, GridUnitType.Star);
        BottomContentRow.Height = new GridLength(240);
        OutputScrollViewer.Visibility = Visibility.Visible;
        ToggleBottomPaneButton.Content = "Hide output";
        ToolTipService.SetToolTip(ToggleBottomPaneButton, "Hide output pane");
    }

}
