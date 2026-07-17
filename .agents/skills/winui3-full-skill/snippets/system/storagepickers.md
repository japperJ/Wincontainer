# Storage Pickers

WinUI 3 desktop apps use `Windows.Storage.Pickers` to open OS file/folder picker dialogs. In WinUI 3, pickers require explicit window-handle initialization via `WinRT.Interop`.

---

## File Open Picker

```csharp
// Views/DocumentPage.xaml.cs
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

private async void OpenFileButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var picker = new FileOpenPicker();
    picker.ViewMode = PickerViewMode.List;
    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    picker.FileTypeFilter.Add(".txt");
    picker.FileTypeFilter.Add(".md");
    picker.FileTypeFilter.Add("*"); // Allow all files

    // Required: associate the picker with the window handle
    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
    InitializeWithWindow.Initialize(picker, hwnd);

    StorageFile? file = await picker.PickSingleFileAsync();
    if (file is not null)
    {
        string content = await FileIO.ReadTextAsync(file);
        ViewModel.LoadedText = content;
        ViewModel.FileName = file.Name;
    }
}
```

---

## File Open Picker — Multiple Files

```csharp
private async void OpenMultipleFilesButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var picker = new FileOpenPicker();
    picker.FileTypeFilter.Add(".jpg");
    picker.FileTypeFilter.Add(".png");

    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
    InitializeWithWindow.Initialize(picker, hwnd);

    var files = await picker.PickMultipleFilesAsync();
    foreach (var file in files)
        ViewModel.Images.Add(file.Path);
}
```

---

## File Save Picker

```csharp
private async void SaveFileButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var picker = new FileSavePicker();
    picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
    picker.SuggestedFileName = "MyDocument";
    picker.FileTypeChoices.Add("Markdown", new List<string> { ".md" });
    picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });

    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
    InitializeWithWindow.Initialize(picker, hwnd);

    StorageFile? file = await picker.PickSaveFileAsync();
    if (file is not null)
    {
        await FileIO.WriteTextAsync(file, ViewModel.DocumentText);
    }
}
```

---

## Folder Picker

```csharp
private async void PickFolderButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var picker = new FolderPicker();
    picker.SuggestedStartLocation = PickerLocationId.Desktop;
    picker.FileTypeFilter.Add("*"); // FolderPicker still needs at least one filter

    var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
    InitializeWithWindow.Initialize(picker, hwnd);

    StorageFolder? folder = await picker.PickSingleFolderAsync();
    if (folder is not null)
    {
        ViewModel.SelectedFolderPath = folder.Path;
    }
}
```

---

## ViewModel Integration

```csharp
// ViewModels/DocumentViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MyApp.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    private readonly Microsoft.UI.Xaml.Window _window;

    public DocumentViewModel(Microsoft.UI.Xaml.Window window)
    {
        _window = window;
    }

    [ObservableProperty]
    private string _documentText = string.Empty;

    [ObservableProperty]
    private string _fileName = string.Empty;

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".txt");

        var hwnd = WindowNative.GetWindowHandle(_window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            DocumentText = await FileIO.ReadTextAsync(file);
            FileName = file.Name;
        }
    }

    [RelayCommand]
    private async Task SaveFileAsync()
    {
        var picker = new FileSavePicker();
        picker.FileTypeChoices.Add("Text file", new List<string> { ".txt" });
        picker.SuggestedFileName = FileName;

        var hwnd = WindowNative.GetWindowHandle(_window);
        InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSaveFileAsync();
        if (file is not null)
            await FileIO.WriteTextAsync(file, DocumentText);
    }
}
```

---

## Notes

- `InitializeWithWindow.Initialize(picker, hwnd)` is **mandatory** in WinUI 3 desktop — pickers will throw without it.
- Get the `hwnd` via `WindowNative.GetWindowHandle(window)`. Pass the window from `App.xaml.cs` or inject it.
- `FileTypeFilter.Add("*")` enables "All files" filter; `FolderPicker` requires at least one filter even though it ignores it.
- `PickSingleFileAsync()` / `PickSaveFileAsync()` / `PickSingleFolderAsync()` return `null` if the user cancels.
- Use `StorageFile.CopyAsync()`, `FileIO.ReadTextAsync()`, `FileIO.WriteTextAsync()` for simple file I/O after picking.
- For access to files outside the app's package, declare `broadFileSystemAccess` capability in the package manifest (requires user permission on first access).
