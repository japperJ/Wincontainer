# Image

Displays a raster image (JPEG, PNG, GIF, BMP, TIFF, ICO) or SVG from a local asset, app package, or remote URI. Use `BitmapImage` for runtime control over decode size and loading events.

---

## Basic Usage

```xaml
<!-- From app package assets -->
<Image Height="100" Source="ms-appx:///Assets/SampleMedia/treetops.jpg"
       AutomationProperties.Name="Tree tops landscape" />
```

---

## Decode to Render Size (Efficient)

Decoding at the display size saves memory — especially important for large photos.

```xaml
<Image Height="100" AutomationProperties.Name="Treetops">
    <Image.Source>
        <BitmapImage
            UriSource="ms-appx:///Assets/SampleMedia/treetops.jpg"
            DecodePixelHeight="100" />
    </Image.Source>
</Image>
```

---

## Stretch Modes

```xaml
<!-- None: display at natural size, may clip -->
<Image Width="100" Height="100" Source="ms-appx:///Assets/photo.jpg" Stretch="None" />

<!-- Fill: stretch to fill, ignores aspect ratio -->
<Image Width="100" Height="100" Source="ms-appx:///Assets/photo.jpg" Stretch="Fill" />

<!-- Uniform: letterbox / pillarbox, preserves aspect ratio -->
<Image Width="100" Height="100" Source="ms-appx:///Assets/photo.jpg" Stretch="Uniform" />

<!-- UniformToFill: zoom-and-crop, preserves aspect ratio -->
<Image Width="100" Height="100" Source="ms-appx:///Assets/photo.jpg" Stretch="UniformToFill" />
```

---

## Nine-Grid Scaling (for UI chrome / badges)

```xaml
<!-- Uniform stretch (no nine-grid) -->
<Image Height="82" Source="ms-appx:///Assets/badge.png" />

<!-- Nine-grid: protect 30px corners, stretch centre -->
<Image Height="164" NineGrid="30,20,30,20" Source="ms-appx:///Assets/badge.png" />
```

---

## SVG Image

```xaml
<Image Height="100" Source="ms-appx:///Assets/icon.svg"
       AutomationProperties.Name="App icon" />
```

---

## Loading from a Remote URI

```xaml
<Image
    x:Name="RemoteImage"
    Height="200"
    AutomationProperties.Name="Remote photo" />
```

```csharp
// Views/PhotoPage.xaml.cs
private async void LoadImageAsync()
{
    var bmp = new BitmapImage();
    bmp.DecodePixelHeight = 200;
    await bmp.SetSourceAsync(await new HttpClient()
        .GetStreamAsync("https://example.com/photo.jpg")
        .AsAsyncOperation());
    RemoteImage.Source = bmp;
}
```

---

## Bound to ViewModel

```xaml
<Image
    Height="200"
    Source="{x:Bind ViewModel.PhotoSource}"
    AutomationProperties.Name="{x:Bind ViewModel.PhotoAlt}" />
```

```csharp
// ViewModels/PhotoViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;

namespace MyApp.ViewModels;

public partial class PhotoViewModel : ObservableObject
{
    [ObservableProperty]
    private BitmapImage? _photoSource;

    [ObservableProperty]
    private string _photoAlt = string.Empty;

    public async Task LoadAsync(Uri uri)
    {
        var bmp = new BitmapImage(uri);
        PhotoSource = bmp;
    }
}
```

---

## Variants

| Property | Values | Description |
|---|---|---|
| `Stretch` | `None`, `Fill`, `Uniform`, `UniformToFill` | How the image fills its bounds |
| `NineGrid` | `"left,top,right,bottom"` | Protect corners from stretching |
| `DecodePixelHeight` / `DecodePixelWidth` | `int` | Decode resolution (set on `BitmapImage`) |
| `Source` | URI string, `BitmapImage`, `WriteableBitmap`, `SoftwareBitmapSource` | Image source |

---

## Notes

- Always set `AutomationProperties.Name` for accessibility — screen readers announce this as the image description.
- Prefer `DecodePixelHeight`/`DecodePixelWidth` to avoid decoding a 4K image into a 100px `Image` control.
- Use `ms-appx:///` for package assets, `ms-appdata:///` for local data, and full HTTPS URIs for remote images.
- SVG sources are rendered by the OS SVG parser; complex SVG effects (filters, masks) may not render as expected.
- `BitmapImage.ImageOpened` and `ImageFailed` let you handle load success/error asynchronously.
