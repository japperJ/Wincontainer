# GridView

`GridView` displays items in a wrapping, horizontally-scrolling grid.
Use it for photo galleries, tile-based layouts, app launchers, and card collections.

---

## Basic Bound GridView

```xaml
<GridView
    ItemsSource="{x:Bind ViewModel.Photos, Mode=OneWay}"
    SelectionMode="Single"
    AutomationProperties.Name="Photo gallery">
    <GridView.ItemTemplate>
        <DataTemplate x:DataType="local:Photo">
            <Image
                Width="190"
                Height="130"
                Source="{x:Bind ImageUri}"
                Stretch="UniformToFill"
                AutomationProperties.Name="{x:Bind Title}" />
        </DataTemplate>
    </GridView.ItemTemplate>
</GridView>
```

---

## Card-Style Template

```xaml
<GridView
    ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
    SelectionMode="None"
    IsItemClickEnabled="True"
    ItemClick="Card_ItemClick"
    AutomationProperties.Name="Content cards">
    <GridView.ItemTemplate>
        <DataTemplate x:DataType="local:ContentItem">
            <Grid
                Width="280"
                CornerRadius="8"
                AutomationProperties.Name="{x:Bind Title}">
                <Grid.RowDefinitions>
                    <RowDefinition Height="Auto" />
                    <RowDefinition Height="*" />
                </Grid.RowDefinitions>
                <Image
                    Height="160"
                    Source="{x:Bind ImageUri}"
                    Stretch="UniformToFill" />
                <StackPanel Grid.Row="1" Padding="12,8" Spacing="4">
                    <TextBlock Text="{x:Bind Title}"
                               Style="{ThemeResource BodyStrongTextBlockStyle}" />
                    <TextBlock Text="{x:Bind Description}"
                               Style="{ThemeResource CaptionTextBlockStyle}"
                               TextTrimming="WordEllipsis"
                               MaxLines="2"
                               Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                </StackPanel>
            </Grid>
        </DataTemplate>
    </GridView.ItemTemplate>
    <GridView.ItemContainerStyle>
        <Style TargetType="GridViewItem" BasedOn="{StaticResource DefaultGridViewItemStyle}">
            <Setter Property="Margin" Value="6" />
        </Style>
    </GridView.ItemContainerStyle>
</GridView>
```

---

## Fixed Columns with ItemsWrapGrid

```xaml
<GridView
    ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
    AutomationProperties.Name="Fixed-column grid">
    <GridView.ItemsPanel>
        <ItemsPanelTemplate>
            <ItemsWrapGrid
                MaximumRowsOrColumns="3"
                Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </GridView.ItemsPanel>
    <GridView.ItemTemplate>
        <DataTemplate x:DataType="local:MyItem">
            <Border
                Width="120"
                Height="120"
                CornerRadius="4"
                Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                <TextBlock
                    Text="{x:Bind Name}"
                    HorizontalAlignment="Center"
                    VerticalAlignment="Center" />
            </Border>
        </DataTemplate>
    </GridView.ItemTemplate>
</GridView>
```

---

## MVVM ViewModel

```csharp
// ViewModels/GalleryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    private readonly IMediaService _mediaService;

    [ObservableProperty]
    private ContentItem? _selectedItem;

    public ObservableCollection<ContentItem> Items { get; } = new();

    public GalleryViewModel(IMediaService mediaService)
    {
        _mediaService = mediaService;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct)
    {
        Items.Clear();
        await foreach (var item in _mediaService.GetItemsAsync(ct))
            Items.Add(item);
    }

    [RelayCommand]
    private void SelectItem(ContentItem item) => SelectedItem = item;
}
```

---

## Notes

- `GridView` is essentially `ListView` with a different default items panel (`ItemsWrapGrid`).
  They share the same API for selection, templates, and events.
- For purely horizontal (non-wrapping) scroll, set `Orientation="Vertical"` on `ItemsWrapGrid`
  and the `GridView`'s `ScrollViewer` to horizontal.
- `IsItemClickEnabled="True"` fires `ItemClick` regardless of `SelectionMode`; useful when
  you want tap-to-open behavior without showing a selection highlight.
- `CanReorderItems="True"` + `AllowDrop="True"` enables drag-to-reorder.
- Always set `x:DataType` on `DataTemplate` for compiled `x:Bind`.
