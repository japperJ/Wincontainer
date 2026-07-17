# ScrollViewer

The legacy scrollable container that wraps any single child and adds horizontal/vertical scroll bars and optional pinch-to-zoom. Widely supported and suitable for most scenarios; for newer physics-based scrolling, see `ScrollView`.

---

## Basic Usage

```xaml
<ScrollViewer Height="300" Width="400">
    <Image Source="ms-appx:///Assets/SampleMedia/cliff.jpg" Stretch="None" />
</ScrollViewer>
```

---

## With Zoom and Scroll Controls

```xaml
<ScrollViewer
    x:Name="ScrollViewerControl"
    Width="400"
    Height="266"
    HorizontalAlignment="Left"
    VerticalAlignment="Top"
    IsTabStop="True"
    IsVerticalScrollChainingEnabled="True"
    ZoomMode="Enabled"
    HorizontalScrollMode="Enabled"
    HorizontalScrollBarVisibility="Auto"
    VerticalScrollMode="Enabled"
    VerticalScrollBarVisibility="Auto"
    ViewChanged="ScrollViewerControl_ViewChanged">
    <Image
        Source="ms-appx:///Assets/SampleMedia/cliff.jpg"
        Stretch="None"
        HorizontalAlignment="Left"
        VerticalAlignment="Top"
        AutomationProperties.Name="Landscape cliff photo" />
</ScrollViewer>
```

```csharp
// Views/PhotoPage.xaml.cs
private void ScrollViewerControl_ViewChanged(object sender,
    ScrollViewerViewChangedEventArgs e)
{
    var sv = (ScrollViewer)sender;
    System.Diagnostics.Debug.WriteLine(
        $"Offset: ({sv.HorizontalOffset:F1}, {sv.VerticalOffset:F1})  Zoom: {sv.ZoomFactor:F2}");
}
```

---

## Programmatic Scroll

```csharp
// Scroll to a specific offset with animation
ScrollViewerControl.ChangeView(
    horizontalOffset: 0,
    verticalOffset: 200,
    zoomFactor: null,
    disableAnimation: false);
```

---

## Wrapping a Long Text Block

```xaml
<ScrollViewer
    VerticalScrollBarVisibility="Auto"
    HorizontalScrollBarVisibility="Disabled">
    <TextBlock
        Margin="12"
        TextWrapping="Wrap"
        Text="{x:Bind ViewModel.LongText}" />
</ScrollViewer>
```

---

## Variants

| Property | Values | Description |
|---|---|---|
| `ZoomMode` | `Enabled`, `Disabled` | Allow pinch-to-zoom |
| `HorizontalScrollMode` | `Enabled`, `Disabled`, `Auto` | Enable horizontal panning |
| `VerticalScrollMode` | `Enabled`, `Disabled`, `Auto` | Enable vertical panning |
| `HorizontalScrollBarVisibility` | `Visible`, `Auto`, `Hidden`, `Disabled` | Scroll bar appearance |
| `VerticalScrollBarVisibility` | `Visible`, `Auto`, `Hidden`, `Disabled` | Scroll bar appearance |
| `MinZoomFactor` / `MaxZoomFactor` | `float` | Clamp zoom range |

---

## Notes

- `ScrollViewer` can only have **one child**; wrap multiple items in a `StackPanel` or `Grid`.
- `IsVerticalScrollChainingEnabled` / `IsHorizontalScrollChainingEnabled` control whether scroll propagates to a parent `ScrollViewer`.
- Use `ChangeView()` (not `ScrollToVerticalOffset()`) — the old methods are deprecated.
- For physics-based inertia scrolling and velocity-based APIs, prefer the newer `ScrollView` control.
- Set `IsTabStop="True"` to allow keyboard scrolling.
