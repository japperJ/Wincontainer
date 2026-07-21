using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;
using WinContainers_App;
using WinContainers.Core.Models;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class ContainerDetailPage : Page, INotifyPropertyChanged
{
    private ContainerDetailViewModel? _viewModel;
    private PropertyChangedEventHandler? _inspectPropertyChangedHandler;
    private DispatcherTimer? _logsTimer;
    private WebView2? _inspectWebView;
    private List<string> _fileNavigationHistory = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    public ContainerDetailViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != value)
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }
    }

    public ContainerDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        DetachInspectPropertyChangedHandler();

        ContainerCardData data;
        if (e.Parameter is ContainerDetailArgs args)
        {
            data = new ContainerCardData
            {
                Id = args.Id,
                Name = args.Name,
                Status = args.Status,
                Image = args.Image,
                Ports = args.Ports,
                CreatedAt = args.CreatedAt
            };
        }
        else if (e.Parameter is ContainerCardData cardData)
        {
            data = cardData;
        }
        else
        {
            return;
        }

        _viewModel = ViewModelLocator.ContainerDetailViewModel;

        ViewModel = _viewModel;

        _viewModel.LoadContainer(data);

        _inspectPropertyChangedHandler = OnViewModelPropertyChanged;
        if (_inspectPropertyChangedHandler is not null)
            _viewModel.PropertyChanged += _inspectPropertyChangedHandler;

        _ = _viewModel.LoadLogsAsync();
        _ = _viewModel.LoadInspectAsync();
        _ = _viewModel.LoadFileListAsync("/");

        StartLogsTimer();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        DetachInspectPropertyChangedHandler();
        StopLogsTimer();
    }

    private void DetachInspectPropertyChangedHandler()
    {
        if (_viewModel is not null && _inspectPropertyChangedHandler is not null)
            _viewModel.PropertyChanged -= _inspectPropertyChangedHandler;

        _inspectPropertyChangedHandler = null;
    }

    private async void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ContainerDetailViewModel.InspectJson) &&
            sender is ContainerDetailViewModel viewModel)
        {
            var json = viewModel.InspectJson;
            if (!string.IsNullOrWhiteSpace(json))
                await InitializeInspectWebViewAsync(json);
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
        => _viewModel?.NavigateBack();

    // ─── Logs Tab ───────────────────────────────────────────────

    private void StartLogsTimer()
    {
        StopLogsTimer();
        _logsTimer = new DispatcherTimer();
        _logsTimer.Interval = TimeSpan.FromSeconds(3);
        _logsTimer.Tick += async (_, _) =>
        {
            if (_viewModel is not null)
                await _viewModel.LoadLogsAsync();
        };
        _logsTimer.Start();
    }

    private void StopLogsTimer()
    {
        if (_logsTimer is not null)
        {
            _logsTimer.Stop();
            _logsTimer = null;
        }
    }

    private async void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.LoadLogsAsync();
    }

    // ─── Inspect Tab ────────────────────────────────────────────

    private async void RefreshInspectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.LoadInspectAsync();
            if (_inspectWebView is not null)
                await InitializeInspectWebViewAsync(_viewModel.InspectJson);
        }
    }

    private void CopyInspectButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.InspectJson is not null)
        {
            var pkg = new Windows.ApplicationModel.DataTransfer.DataPackage();
            pkg.SetText(_viewModel.InspectJson);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }
    }

    public async Task InitializeInspectWebViewAsync(string json)
    {
        if (_inspectWebView is null)
        {
            _inspectWebView = new WebView2
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };
            InspectContainer.Children.Clear();
            InspectContainer.Children.Add(_inspectWebView);

            await _inspectWebView.EnsureCoreWebView2Async();

            var htmlPath = Path.Combine(AppContext.BaseDirectory, "Assets", "container-inspect.html");
            if (!File.Exists(htmlPath))
                htmlPath = Path.Combine(Directory.GetCurrentDirectory(), "Assets", "container-inspect.html");

            if (File.Exists(htmlPath))
            {
                var html = await File.ReadAllTextAsync(htmlPath);
                _inspectWebView.NavigateToString(html);
            }
            else
            {
                _inspectWebView.NavigateToString(
                    "<html><body style='color:#e6edf3;background:#0d1117;font-family:monospace;padding:16px;'>" +
                    "<p>Inspect viewer not found. Ensure Assets/container-inspect.html is deployed.</p></body></html>");
            }

            _inspectWebView.NavigationCompleted += (_, _) =>
            {
                _inspectWebView?.ExecuteScriptAsync(WebViewScriptEncoder.BuildSetJsonScript(json));
            };
        }
        else
        {
            _ = _inspectWebView.ExecuteScriptAsync(WebViewScriptEncoder.BuildSetJsonScript(json));
        }
    }

    // ─── Shell Tab ──────────────────────────────────────────────

    private async void ShellRunButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.RunShellCommandAsync();
    }

    private void ShellCommandBox_KeyUp(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key == Windows.System.VirtualKey.Enter)
        {
            e.Handled = true;
            if (_viewModel is not null)
            {
                _viewModel.ShellCommand = ShellCommandBox.Text;
                _ = _viewModel.RunShellCommandAsync();
            }
        }
    }

    private void ClearShellButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            _viewModel.ShellOutput = "";
    }

    // ─── Files Tab ──────────────────────────────────────────────

    private async void RefreshFilesButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.LoadFileListAsync(_viewModel.CurrentFilePath);
    }

    private void BreadcrumbSegment_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;
        if (sender is Button btn && btn.Content is string segment)
        {
            var segments = _viewModel.BreadcrumbSegments.ToList();
            var index = segments.IndexOf(segment);
            if (index >= 0)
            {
                var path = string.Join("/", segments.Take(index + 1));
                if (!path.StartsWith("/")) path = "/" + path;
                _ = _viewModel.LoadFileListAsync(path);
            }
        }
    }

    private async void FileListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is not FileEntryData entry || _viewModel is null)
            return;

        if (entry.Type == "dir")
        {
            string newPath;
            if (entry.Name == "..")
            {
                // Go to parent directory
                var current = _viewModel.CurrentFilePath.TrimEnd('/');
                var lastSlash = current.LastIndexOf('/');
                newPath = lastSlash <= 0 ? "/" : current.Substring(0, lastSlash);
            }
            else
            {
                newPath = _viewModel.CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
            }
            _fileNavigationHistory.Add(_viewModel.CurrentFilePath);
            await _viewModel.LoadFileListAsync(newPath);
        }
        else
        {
            await _viewModel.OpenFileViewerAsync(entry);
        }
    }

    private void FileListView_RightTapped(object sender, Microsoft.UI.Xaml.Input.RightTappedRoutedEventArgs e)
    {
        var element = e.OriginalSource as FrameworkElement;
        var entry = element?.DataContext as FileEntryData;
        if (entry == null || _viewModel is null) return;

        var flyout = new MenuFlyout();

        if (entry.Type == "file")
        {
            var exportItem = new MenuFlyoutItem { Text = "Export" };
            exportItem.Click += async (_, _) => await ExportFileAsync(entry);
            flyout.Items.Add(exportItem);
        }

        var chmodItem = new MenuFlyoutItem { Text = "Change permissions" };
        chmodItem.Click += async (_, _) => await ShowChangePermissionsDialogAsync(entry);
        flyout.Items.Add(chmodItem);

        var deleteItem = new MenuFlyoutItem { Text = "Delete" };
        deleteItem.Click += async (_, _) => await _viewModel.DeleteFileAsync(entry);
        flyout.Items.Add(deleteItem);

        flyout.ShowAt((ListView)sender, e.GetPosition((ListView)sender));
    }

    private async Task ExportFileAsync(FileEntryData entry)
    {
        if (_viewModel is null) return;

        var savePicker = new FileSavePicker();
        savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        savePicker.SuggestedFileName = entry.Name;
        savePicker.FileTypeChoices.Add("All files", new List<string> { "." });

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Instance);
        InitializeWithWindow.Initialize(savePicker, hwnd);

        var file = await savePicker.PickSaveFileAsync();
        if (file == null) return;

        try
        {
            var filePath = _viewModel.CurrentFilePath.TrimEnd('/') + "/" + entry.Name;
            var content = await _viewModel.ReadFileAsync(filePath);
            await File.WriteAllTextAsync(file.Path, content);
        }
        catch (Exception ex)
        {
            await _viewModel.ShowErrorAsync($"Export failed: {ex.Message}");
        }
    }

    private async Task ShowChangePermissionsDialogAsync(FileEntryData entry)
    {
        if (_viewModel is null) return;

        var dialog = new ContentDialog
        {
            Title = "Change permissions",
            PrimaryButtonText = "Apply",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var stack = new StackPanel { Spacing = 8 };
        stack.Children.Add(new TextBlock { Text = $"Set permissions for '{entry.Name}'" });

        var currentMode = entry.Permissions?.Trim() ?? "";
        var currentModeText = string.IsNullOrWhiteSpace(currentMode) ? "(unknown)" : currentMode;
        stack.Children.Add(new TextBlock { Text = $"Current: {currentModeText}", Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"] });

        var presetCombo = new ComboBox
        {
            Width = 240,
            SelectedIndex = 0,
            Items =
            {
                "Read/write for owner, read-only for others (644)",
                "Read/write/execute for owner, read/execute for others (755)",
                "Read/write for owner, no access for others (600)",
                "Custom"
            }
        };

        var modeBox = new TextBox { PlaceholderText = "e.g. 755", Width = 240, Text = currentMode switch
        {
            "-rw-r--r--" => "644",
            "drwxr-xr-x" => "755",
            "-rw-------" => "600",
            _ => currentMode
        } };
        var infoText = new TextBlock
        {
            Text = "Use a classic octal mode such as 644, 755, 600, or 777.",
            Foreground = (SolidColorBrush)Application.Current.Resources["TextFillColorSecondaryBrush"],
            TextWrapping = TextWrapping.Wrap
        };

        presetCombo.SelectionChanged += (_, _) =>
        {
            var selected = presetCombo.SelectedItem as string;
            modeBox.Text = selected switch
            {
                "Read/write for owner, read-only for others (644)" => "644",
                "Read/write/execute for owner, read/execute for others (755)" => "755",
                "Read/write for owner, no access for others (600)" => "600",
                _ => modeBox.Text
            };
            modeBox.IsEnabled = selected == "Custom";
        };

        stack.Children.Add(presetCombo);
        stack.Children.Add(modeBox);
        stack.Children.Add(infoText);
        dialog.Content = stack;

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary) return;

        var mode = modeBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(mode))
        {
            await _viewModel.ShowErrorAsync("Please enter a permission mode.");
            return;
        }

        await _viewModel.ChangePermissionsAsync(entry, mode);
    }

    private async void ImportFileButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;

        var picker = new FileOpenPicker();
        picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(MainWindow.Instance);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file == null) return;

        var error = await _viewModel.DoImportFileAsync(file.Path, file.Name);
        if (error is not null)
            await _viewModel.ShowErrorAsync(error);
    }

    private async void FileViewerBack_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is null) return;

        if (_viewModel.IsEditing)
        {
            _viewModel.CancelEditing();
        }

        _viewModel.ShowFileContent = false;
        _viewModel.ShowFileList = true;
    }

    private void FileViewerEditButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.StartEditing();
    }

    private async void FileViewerSaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.SaveFileAsync();
    }

    private void FileViewerCancelButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel?.CancelEditing();
    }

    private async void FileViewerExportButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.FileEntries is not null)
        {
            var name = _viewModel.ViewingFilePath.Split('/').LastOrDefault();
            var entry = _viewModel.FileEntries.FirstOrDefault(f => f.Name == name);
            if (entry is not null)
                await ExportFileAsync(entry);
        }
    }

    private async void FileViewerDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel?.FileEntries is not null)
        {
            var name = _viewModel.ViewingFilePath.Split('/').LastOrDefault();
            var entry = _viewModel.FileEntries.FirstOrDefault(f => f.Name == name);
            if (entry is not null)
                await _viewModel.DeleteFileAsync(entry);
        }
    }

    // ─── Container Actions ──────────────────────────────────────

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.RunActionAsync("Start");
    }

    private async void StopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.RunActionAsync("Stop");
    }

    private async void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.RunActionAsync("Restart");
    }

    private async void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
        {
            await _viewModel.RunActionAsync("Delete");
            _viewModel.NavigateBack();
        }
    }

    private void ActionErrorInfoBar_CloseButtonClick(Microsoft.UI.Xaml.Controls.InfoBar sender, object args)
    {
        _viewModel?.DismissActionError();
    }
}
