# MediaPlayerElement

Embeds a full-featured media player (video and audio) backed by the Windows `MediaPlayer` engine. Supports local files, HTTP streams, adaptive streaming (HLS/DASH), and custom transport controls.

---

## Basic Usage

```xaml
<MediaPlayerElement
    Source="ms-appx:///Assets/SampleMedia/video.mp4"
    AutoPlay="True"
    AreTransportControlsEnabled="True" />
```

---

## Full Example with Transport Controls

```xaml
<MediaPlayerElement
    x:Name="MediaPlayer"
    MinHeight="200"
    HorizontalAlignment="Stretch"
    AreTransportControlsEnabled="True"
    AutoPlay="False">
    <MediaPlayerElement.TransportControls>
        <MediaTransportControls
            IsCompact="False"
            IsFullWindowButtonVisible="True"
            IsVolumeButtonVisible="True"
            IsPlaybackRateButtonVisible="True" />
    </MediaPlayerElement.TransportControls>
</MediaPlayerElement>
```

---

## Opening a File via StoragePicker

```csharp
// Views/MediaPage.xaml.cs
private async void OpenFileButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var picker = new Windows.Storage.Pickers.FileOpenPicker();
    picker.FileTypeFilter.Add(".mp4");
    picker.FileTypeFilter.Add(".mp3");
    picker.FileTypeFilter.Add(".wmv");

    // Initialize the picker with the window handle (required in WinUI 3 desktop)
    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
    WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

    var file = await picker.PickSingleFileAsync();
    if (file is not null)
    {
        var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
        MediaPlayer.Source = MediaSource.CreateFromStream(stream, file.ContentType);
    }
}
```

---

## Streaming from a URI

```csharp
private void LoadStream()
{
    MediaPlayer.Source = MediaSource.CreateFromUri(
        new Uri("https://commondatastorage.googleapis.com/gtv-videos-bucket/sample/BigBuckBunny.mp4"));
    MediaPlayer.MediaPlayer.Play();
}
```

---

## Programmatic Playback Control

```csharp
// Play / Pause / Stop via the underlying MediaPlayer
private void PlayPauseButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var mp = MediaPlayerControl.MediaPlayer;
    if (mp.PlaybackSession.PlaybackState == MediaPlaybackState.Playing)
        mp.Pause();
    else
        mp.Play();
}

private void SeekButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // Seek to 30 seconds
    MediaPlayerControl.MediaPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(30);
}
```

---

## ViewModel Integration

```xaml
<MediaPlayerElement
    x:Name="MediaPlayerControl"
    AreTransportControlsEnabled="True"
    AutoPlay="{x:Bind ViewModel.AutoPlay}" />
```

```csharp
// Views/VideoPage.xaml.cs — wire source from ViewModel
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    if (ViewModel.VideoUri is not null)
        MediaPlayerControl.Source = MediaSource.CreateFromUri(ViewModel.VideoUri);
}
```

```csharp
// ViewModels/VideoViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class VideoViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _autoPlay = false;

    public Uri? VideoUri { get; set; }
}
```

---

## Notes

- `MediaPlayerElement` wraps a `Windows.Media.Playback.MediaPlayer`; access it via `MediaPlayerElement.MediaPlayer`.
- Use `MediaSource.CreateFromUri()` for network streams and `CreateFromStream()` / `CreateFromStorageFile()` for local files.
- `AreTransportControlsEnabled="True"` shows the built-in play/pause/seek bar; customize it via `MediaPlayerElement.TransportControls`.
- For background audio playback, use `SystemMediaTransportControls` and set `MediaPlayer.CommandManager.IsEnabled = true`.
- Video renders in a separate composition layer (airspace issue) — other XAML elements cannot overlay it unless you use `MediaPlayerSurface` with Composition APIs.
- Requires the `Windows.Media.Playback` namespace; no extra NuGet package needed beyond Windows App SDK.
