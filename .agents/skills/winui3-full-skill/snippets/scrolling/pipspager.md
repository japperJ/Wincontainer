# PipsPager

A row (or column) of small dot indicators that show the current page position within a collection. Typically paired with `FlipView` or a custom paged view.

---

## Basic Usage — Paired with FlipView

```xaml
<StackPanel>
    <FlipView
        x:Name="Gallery"
        Height="270"
        MaxWidth="400"
        ItemsSource="{x:Bind Pictures}">
        <FlipView.ItemTemplate>
            <DataTemplate x:DataType="x:String">
                <Image Source="{x:Bind Mode=OneTime}" />
            </DataTemplate>
        </FlipView.ItemTemplate>
    </FlipView>

    <PipsPager
        x:Name="FlipViewPipsPager"
        Margin="0,12,0,0"
        HorizontalAlignment="Center"
        NumberOfPages="{x:Bind Pictures.Count}"
        SelectedPageIndex="{x:Bind Path=Gallery.SelectedIndex, Mode=TwoWay}" />
</StackPanel>
```

```csharp
// Views/GalleryPage.xaml.cs
public sealed partial class GalleryPage : Page
{
    public ObservableCollection<string> Pictures { get; } = new()
    {
        "ms-appx:///Assets/SampleMedia/cliff.jpg",
        "ms-appx:///Assets/SampleMedia/grapes.jpg",
        "ms-appx:///Assets/SampleMedia/rainier.jpg",
        "ms-appx:///Assets/SampleMedia/sunset.jpg",
    };

    public GalleryPage()
    {
        InitializeComponent();
    }
}
```

---

## Standalone with Options

```xaml
<PipsPager
    x:Name="TestPipsPager"
    NumberOfPages="10"
    Orientation="Horizontal"
    PreviousButtonVisibility="Visible"
    NextButtonVisibility="Visible"
    SelectedIndexChanged="TestPipsPager_SelectedIndexChanged" />
```

```csharp
private void TestPipsPager_SelectedIndexChanged(PipsPager sender,
    PipsPagerSelectedIndexChangedEventArgs args)
{
    System.Diagnostics.Debug.WriteLine($"Page: {sender.SelectedPageIndex + 1} / {sender.NumberOfPages}");
}
```

---

## Vertical Orientation

```xaml
<PipsPager
    NumberOfPages="5"
    Orientation="Vertical"
    PreviousButtonVisibility="VisibleOnPointerOver"
    NextButtonVisibility="VisibleOnPointerOver" />
```

---

## Bound to ViewModel

```xaml
<PipsPager
    NumberOfPages="{x:Bind ViewModel.PageCount}"
    SelectedPageIndex="{x:Bind ViewModel.CurrentPage, Mode=TwoWay}" />
```

```csharp
// ViewModels/OnboardingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    [ObservableProperty]
    private int _currentPage = 0;

    public int PageCount => 5;
}
```

---

## Variants

| Property | Values | Description |
|---|---|---|
| `Orientation` | `Horizontal`, `Vertical` | Layout direction of pip dots |
| `PreviousButtonVisibility` | `Visible`, `VisibleOnPointerOver`, `Collapsed` | Prev arrow button |
| `NextButtonVisibility` | `Visible`, `VisibleOnPointerOver`, `Collapsed` | Next arrow button |
| `NumberOfPages` | `int` | Total pages (dots) to display |
| `SelectedPageIndex` | `int` (two-way) | Currently selected page (0-based) |

---

## Notes

- `SelectedPageIndex` is 0-based and two-way bindable — it both reads and drives the selection.
- `NumberOfPages` must be set; the control will not show pips if it is 0.
- For large page counts, dots automatically compact/scroll; consider using numbered indicators instead of pips if count exceeds ~20.
- `PipsPager` does not scroll content itself — it only reflects and drives the `SelectedPageIndex`.
