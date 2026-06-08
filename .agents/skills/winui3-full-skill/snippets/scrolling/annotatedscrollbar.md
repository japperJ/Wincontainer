# AnnotatedScrollBar

A custom scroll bar that overlays labelled tick marks alongside a `ScrollView`, providing at-a-glance positional context (e.g. alphabet letters, section names, dates). Introduced in Windows App SDK 1.3.

---

## Basic Usage — Linked to a ScrollView

```xaml
<Grid ColumnDefinitions="*, Auto">
    <!-- Main scrollable content -->
    <ScrollView
        x:Name="scrollView"
        MaxWidth="800"
        MaxHeight="500"
        Background="LightGray"
        VerticalScrollBarVisibility="Hidden">
        <ItemsRepeater
            x:Name="itemsRepeater"
            Margin="2"
            ItemsSource="{x:Bind ViewModel.ColorItems}"
            SizeChanged="ItemsRepeater_SizeChanged">
            <ItemsRepeater.Layout>
                <UniformGridLayout />
            </ItemsRepeater.Layout>
            <ItemsRepeater.ItemTemplate>
                <DataTemplate x:DataType="Microsoft.UI.Xaml.Media:SolidColorBrush">
                    <Grid
                        Width="112"
                        Height="82"
                        Margin="4"
                        Background="{x:Bind}"
                        CornerRadius="4" />
                </DataTemplate>
            </ItemsRepeater.ItemTemplate>
        </ItemsRepeater>
    </ScrollView>

    <!-- AnnotatedScrollBar replaces the built-in scroll bar -->
    <AnnotatedScrollBar
        x:Name="annotatedScrollBar"
        Grid.Column="1"
        MaxHeight="500"
        Margin="4,0,48,0"
        HorizontalAlignment="Right"
        DetailLabelRequested="AnnotatedScrollBar_DetailLabelRequested" />
</Grid>
```

---

## Code-Behind — Wire Up the ScrollController

```csharp
// Views/ColorGalleryPage.xaml.cs
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

public sealed partial class ColorGalleryPage : Page
{
    public ColorGalleryPage()
    {
        InitializeComponent();
        Loaded += ColorGalleryPage_Loaded;
    }

    private void ColorGalleryPage_Loaded(object sender,
        Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Connect the AnnotatedScrollBar's controller to the ScrollView's presenter
        scrollView.ScrollPresenter.VerticalScrollController =
            annotatedScrollBar.ScrollController;
    }

    // Provide the label text shown in the detail tooltip when hovering a tick mark
    private void AnnotatedScrollBar_DetailLabelRequested(AnnotatedScrollBar sender,
        AnnotatedScrollBarDetailLabelRequestedEventArgs args)
    {
        // args.ScrollOffset is the scroll position the tick mark represents
        // Calculate which item/section is at that offset and return a label
        double itemHeight = 90; // Width + Margin of each color swatch
        int itemsPerRow = (int)(scrollView.ActualWidth / itemHeight);
        if (itemsPerRow < 1) itemsPerRow = 1;

        int rowIndex = (int)(args.ScrollOffset / itemHeight);
        int itemIndex = rowIndex * itemsPerRow;
        itemIndex = Math.Clamp(itemIndex, 0, ViewModel.ColorItems.Count - 1);

        args.Content = $"Item {itemIndex + 1}";
    }

    private void ItemsRepeater_SizeChanged(object sender,
        Microsoft.UI.Xaml.SizeChangedEventArgs e)
    {
        // Refresh labels layout when content size changes
        annotatedScrollBar.InvalidateLabels();
    }
}
```

---

## ViewModel

```csharp
// ViewModels/ColorGalleryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class ColorGalleryViewModel : ObservableObject
{
    public ObservableCollection<SolidColorBrush> ColorItems { get; } = new();

    public ColorGalleryViewModel()
    {
        // Generate a palette of colours
        for (int r = 0; r <= 255; r += 51)
        for (int g = 0; g <= 255; g += 51)
        for (int b = 0; b <= 255; b += 51)
            ColorItems.Add(new SolidColorBrush(
                Microsoft.UI.ColorHelper.FromArgb(255, (byte)r, (byte)g, (byte)b)));
    }
}
```

---

## Adding Custom Labels

```csharp
// Add programmatic labels for section markers
private void AddSectionLabels()
{
    annotatedScrollBar.Labels.Clear();

    double sectionHeight = scrollView.ScrollPresenter.ExtentHeight / 26;
    for (char letter = 'A'; letter <= 'Z'; letter++)
    {
        double offset = (letter - 'A') * sectionHeight;
        annotatedScrollBar.Labels.Add(
            new AnnotatedScrollBarLabel(offset, letter.ToString()));
    }
}
```

---

## Notes

- **Requires Windows App SDK 1.3+** — not available in earlier releases.
- Always set `VerticalScrollBarVisibility="Hidden"` on the paired `ScrollView` — `AnnotatedScrollBar` acts as the replacement scroll bar.
- Connect via `scrollView.ScrollPresenter.VerticalScrollController = annotatedScrollBar.ScrollController` in the `Loaded` event.
- `DetailLabelRequested` fires on hover/pointer-over to populate the tooltip label; use `args.ScrollOffset` to map to a section.
- `InvalidateLabels()` forces a re-layout of the label ticks — call it when content size changes.
- `AnnotatedScrollBar.Labels` is an `IList<AnnotatedScrollBarLabel>`; each label takes an offset (in scroll pixels) and a display string.
