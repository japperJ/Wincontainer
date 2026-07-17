# Clipboard

The Windows Clipboard API (`Windows.ApplicationModel.DataTransfer.Clipboard`) lets you read and write text, images, HTML, RTF, and custom data formats. In WinUI 3 desktop apps, use the WinRT `Clipboard` class directly.

---

## Copy Text to Clipboard

```csharp
using Windows.ApplicationModel.DataTransfer;

private void CopyTextButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var dataPackage = new DataPackage();
    dataPackage.SetText("Hello from WinUI 3!");
    Clipboard.SetContent(dataPackage);
}
```

---

## Paste Text from Clipboard

```csharp
private async void PasteTextButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var dataPackageView = Clipboard.GetContent();

    if (dataPackageView.Contains(StandardDataFormats.Text))
    {
        string text = await dataPackageView.GetTextAsync();
        OutputTextBlock.Text = text;
    }
}
```

---

## Copy an Image

```csharp
private async void CopyImageButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var file = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(
        new Uri("ms-appx:///Assets/SampleMedia/cliff.jpg"));

    var dataPackage = new DataPackage();
    dataPackage.SetBitmap(
        Windows.Storage.Streams.RandomAccessStreamReference.CreateFromFile(file));
    Clipboard.SetContent(dataPackage);
}
```

---

## Paste an Image

```csharp
private async void PasteImageButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var dataPackageView = Clipboard.GetContent();

    if (dataPackageView.Contains(StandardDataFormats.Bitmap))
    {
        var streamRef = await dataPackageView.GetBitmapAsync();
        using var stream = await streamRef.OpenReadAsync();

        var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
        await bitmapImage.SetSourceAsync(stream);
        PreviewImage.Source = bitmapImage;
    }
}
```

---

## Copy Multiple Formats (Text + HTML)

```csharp
private void CopyRichButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var dataPackage = new DataPackage();
    dataPackage.SetText("Hello, World!");
    dataPackage.SetHtmlFormat(
        HtmlFormatHelper.CreateHtmlFormat("<b>Hello</b>, <em>World</em>!"));
    Clipboard.SetContent(dataPackage);
}
```

---

## Listening for Clipboard Changes

```csharp
// Register in Page.Loaded
private void Page_Loaded(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    Clipboard.ContentChanged += Clipboard_ContentChanged;
}

private void Page_Unloaded(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    Clipboard.ContentChanged -= Clipboard_ContentChanged;
}

private async void Clipboard_ContentChanged(object? sender, object e)
{
    var view = Clipboard.GetContent();

    // Update UI on the UI thread
    DispatcherQueue.TryEnqueue(async () =>
    {
        if (view.Contains(StandardDataFormats.Text))
        {
            var text = await view.GetTextAsync();
            ClipboardPreview.Text = text;
        }
    });
}
```

---

## ViewModel Integration

```csharp
// ViewModels/ClipboardViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Windows.ApplicationModel.DataTransfer;

namespace MyApp.ViewModels;

public partial class ClipboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string _clipboardText = string.Empty;

    [RelayCommand]
    private void CopyText(string text)
    {
        var package = new DataPackage();
        package.SetText(text);
        Clipboard.SetContent(package);
    }

    [RelayCommand]
    private async Task PasteTextAsync()
    {
        var view = Clipboard.GetContent();
        if (view.Contains(StandardDataFormats.Text))
            ClipboardText = await view.GetTextAsync();
    }
}
```

---

## Notes

- `Clipboard.SetContent()` immediately replaces whatever was on the clipboard.
- `Clipboard.SetContentWithOptions()` accepts `ClipboardContentOptions` — use `IsAllowedInHistory = false` to prevent the content from appearing in the clipboard history.
- Always check `dataPackageView.Contains(format)` before calling the corresponding `Get*Async()` method.
- `Clipboard.GetContent()` returns a snapshot; call it again to get updated content after `ContentChanged`.
- The clipboard requires the app to be in the foreground on the UI thread for `SetContent`; `GetContent` can be called from any thread.
- For WinUI 3 desktop (unpackaged), ensure `uap` capability `"clipboardRead"` is declared in the package manifest if required by the store.
