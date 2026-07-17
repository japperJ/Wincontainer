# VariableSizedWrapGrid

`VariableSizedWrapGrid` arranges children in a grid that wraps when it runs out of space. Each child can span multiple cells using `VariableSizedWrapGrid.ColumnSpan` and `VariableSizedWrapGrid.RowSpan` attached properties. It is most often used as the `ItemsPanel` for a `GridView`.

## Basic VariableSizedWrapGrid as ItemsPanel

```xaml
<GridView ItemsSource="{x:Bind ViewModel.Tiles}">
    <GridView.ItemsPanel>
        <ItemsPanelTemplate>
            <VariableSizedWrapGrid
                ItemWidth="150"
                ItemHeight="150"
                MaximumRowsOrColumns="4"
                Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </GridView.ItemsPanel>

    <GridView.ItemTemplate>
        <DataTemplate x:DataType="local:TileItem">
            <Border
                Background="{x:Bind Color}"
                CornerRadius="4">
                <TextBlock
                    Margin="8"
                    Foreground="White"
                    Text="{x:Bind Title}" />
            </Border>
        </DataTemplate>
    </GridView.ItemTemplate>

    <GridView.ItemContainerStyle>
        <Style TargetType="GridViewItem">
            <Setter Property="Padding" Value="2" />
        </Style>
    </GridView.ItemContainerStyle>
</GridView>
```

## Variable-span items (Windows Start-style tiles)

```xaml
<GridView
    ItemsSource="{x:Bind ViewModel.Tiles}"
    PrepareContainerForItemOverride="GridView_PrepareContainer">
    <GridView.ItemsPanel>
        <ItemsPanelTemplate>
            <VariableSizedWrapGrid
                ItemWidth="100"
                ItemHeight="100"
                MaximumRowsOrColumns="6"
                Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </GridView.ItemsPanel>
</GridView>
```

```csharp
// Set column/row span per item
private void GridView_PrepareContainer(object sender, PrepareContainerForItemOverrideEventArgs e)
{
    if (e.Item is TileItem tile)
    {
        VariableSizedWrapGrid.SetColumnSpan(e.ItemContainer, tile.ColumnSpan);
        VariableSizedWrapGrid.SetRowSpan(e.ItemContainer, tile.RowSpan);
    }
}

public class TileItem
{
    public string Title { get; set; } = string.Empty;
    public string Color { get; set; } = "#0078D4";
    public int ColumnSpan { get; set; } = 1;
    public int RowSpan { get; set; } = 1;
}
```

## Standalone VariableSizedWrapGrid

```xaml
<VariableSizedWrapGrid
    ItemWidth="120"
    ItemHeight="80"
    MaximumRowsOrColumns="3"
    Orientation="Horizontal">

    <Rectangle
        VariableSizedWrapGrid.ColumnSpan="2"
        Fill="SteelBlue" />
    <Rectangle Fill="Tomato" />
    <Rectangle Fill="SeaGreen" />
    <Rectangle
        VariableSizedWrapGrid.ColumnSpan="2"
        Fill="Goldenrod" />
</VariableSizedWrapGrid>
```

## Notes

- `ItemWidth` and `ItemHeight` define the cell size; all items are snapped to multiples of these values.
- `Orientation="Horizontal"` (default) fills rows left-to-right and wraps to the next row; `Vertical` fills columns top-to-bottom.
- `MaximumRowsOrColumns` limits the number of columns (horizontal) or rows (vertical) before wrapping.
- Column/row spans work by `VariableSizedWrapGrid.SetColumnSpan(container, n)` in code-behind or via the `PrepareContainerForItemOverride` event.
- Items must be the same base cell size; `ColumnSpan`/`RowSpan` multiply that base size.
- For fully variable item dimensions without a grid constraint, use `WrapPanel` from CommunityToolkit or `ItemsRepeater` with `FlowLayout`.
