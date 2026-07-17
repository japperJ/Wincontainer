# ItemsView

`ItemsView` is a modern, high-performance collection control that replaces `ListView`/`GridView` for most new scenarios. It supports swappable layouts (`LinedFlowLayout`, `UniformGridLayout`, `StackLayout`) and uses `ItemContainer` as its item wrapper.

## Basic ItemsView (default StackLayout)

```xaml
<DataTemplate x:Key="ImageTemplate" x:DataType="local:PhotoItem">
    <ItemContainer
        Width="200"
        Height="140"
        HorizontalAlignment="Left"
        AutomationProperties.Name="{x:Bind Title}">
        <Image
            Margin="4"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            AutomationProperties.AccessibilityView="Raw"
            Source="{x:Bind ImageUri}"
            Stretch="UniformToFill" />
    </ItemContainer>
</DataTemplate>

<ItemsView
    Width="220"
    Height="400"
    HorizontalAlignment="Left"
    IsItemInvokedEnabled="True"
    ItemInvoked="BasicItemsView_ItemInvoked"
    ItemTemplate="{StaticResource ImageTemplate}"
    ItemsSource="{x:Bind ViewModel.Photos}" />
```

```csharp
private void BasicItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs e)
{
    var item = e.InvokedItem as PhotoItem;
    StatusText = $"You invoked {item?.Title}.";
}
```

## ItemsView with LinedFlowLayout

```xaml
<DataTemplate x:Key="LinedFlowTemplate" x:DataType="local:PhotoItem">
    <ItemContainer AutomationProperties.Name="{x:Bind Title}">
        <Grid>
            <Image
                MinWidth="70"
                HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Source="{x:Bind ImageUri}"
                Stretch="UniformToFill" />
            <StackPanel
                Height="40"
                Padding="5,1"
                VerticalAlignment="Bottom"
                Background="{ThemeResource SystemControlBackgroundBaseMediumBrush}"
                Opacity="0.75">
                <TextBlock
                    Foreground="{ThemeResource SystemControlForegroundAltHighBrush}"
                    Text="{x:Bind Title}" />
                <TextBlock
                    Foreground="{ThemeResource SystemControlForegroundAltHighBrush}"
                    Style="{ThemeResource CaptionTextBlockStyle}"
                    Text="{x:Bind Likes}" />
            </StackPanel>
        </Grid>
    </ItemContainer>
</DataTemplate>

<ItemsView
    Width="500"
    Height="400"
    HorizontalAlignment="Left"
    ItemTemplate="{StaticResource LinedFlowTemplate}"
    ItemsSource="{x:Bind ViewModel.Photos}">
    <ItemsView.Layout>
        <LinedFlowLayout
            ItemsStretch="Fill"
            LineHeight="160"
            LineSpacing="5"
            MinItemSpacing="5" />
    </ItemsView.Layout>
</ItemsView>
```

## ItemsView with UniformGridLayout and Selection

```xaml
<DataTemplate x:Key="GridTemplate" x:DataType="local:PhotoItem">
    <ItemContainer AutomationProperties.Name="{x:Bind Title}">
        <Grid Width="150">
            <Image
                HorizontalAlignment="Center"
                VerticalAlignment="Center"
                Source="{x:Bind ImageUri}"
                Stretch="UniformToFill" />
            <StackPanel
                Height="40"
                Padding="5,1"
                VerticalAlignment="Bottom"
                Background="{ThemeResource SystemControlBackgroundBaseMediumBrush}"
                Opacity="0.75">
                <TextBlock
                    Foreground="{ThemeResource SystemControlForegroundAltHighBrush}"
                    Text="{x:Bind Title}" />
            </StackPanel>
        </Grid>
    </ItemContainer>
</DataTemplate>

<ItemsView
    x:Name="GridItemsView"
    Width="500"
    Height="400"
    HorizontalAlignment="Left"
    IsItemInvokedEnabled="True"
    ItemInvoked="GridItemsView_ItemInvoked"
    ItemTemplate="{StaticResource GridTemplate}"
    ItemsSource="{x:Bind ViewModel.Photos}"
    SelectionChanged="GridItemsView_SelectionChanged"
    SelectionMode="Multiple">
    <ItemsView.Layout>
        <UniformGridLayout
            MaximumRowsOrColumns="3"
            MinColumnSpacing="5"
            MinRowSpacing="5" />
    </ItemsView.Layout>
</ItemsView>
```

```csharp
private void GridItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs e)
{
    var item = e.InvokedItem as PhotoItem;
    InvocationStatus = $"You invoked {item?.Title}.";
}

private void GridItemsView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs e)
{
    SelectionStatus = $"You have selected {sender.SelectedItems.Count} item(s).";
}
```

## ViewModel

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class PhotoGalleryViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusText = string.Empty;

    public ObservableCollection<PhotoItem> Photos { get; } = new();

    public PhotoGalleryViewModel()
    {
        // Populate with real data
        for (int i = 1; i <= 20; i++)
        {
            Photos.Add(new PhotoItem
            {
                Title = $"Photo {i}",
                ImageUri = new Uri($"ms-appx:///Assets/Photos/photo{i}.jpg"),
                Likes = Random.Shared.Next(10, 500)
            });
        }
    }
}

public class PhotoItem
{
    public string Title { get; set; } = string.Empty;
    public Uri? ImageUri { get; set; }
    public int Likes { get; set; }
}
```

## Layouts

| Layout | Use case |
|---|---|
| `StackLayout` | Simple vertical/horizontal list (default) |
| `LinedFlowLayout` | Pinterest-style rows of variable-width items |
| `UniformGridLayout` | Grid of equal-size items |

## SelectionMode values

| Value | Behavior |
|---|---|
| `None` | No selection |
| `Single` | One item at a time |
| `Multiple` | Checkboxes appear on items |
| `Extended` | Ctrl+Click / Shift+Click for range |

## Notes

- Always wrap item content in `ItemContainer` inside `DataTemplate`.
- Set `x:DataType` on every `DataTemplate` for compiled bindings.
- `IsItemInvokedEnabled="True"` enables double-click/Enter invocation separately from selection.
- `ItemsView` virtualizes by default; no wrapping in `ScrollViewer` needed.
- Prefer `ItemsView` + `UniformGridLayout` over `GridView` for new code targeting Windows App SDK 1.4+.
