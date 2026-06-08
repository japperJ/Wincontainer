# ItemsRepeater

`ItemsRepeater` is a low-level, virtualized layout control for custom collection views.
Unlike `ListView`/`GridView`, it has no selection, scrolling, or item containers built in —
you compose those yourself. Use it when you need a custom layout that `ListView` or `GridView`
cannot express.

---

## Basic Vertical List

```xaml
<ScrollViewer>
    <ItemsRepeater
        ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
        AutomationProperties.Name="Custom items">
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="local:MyItem">
                <Border Padding="8" Margin="0,2" CornerRadius="4"
                        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}">
                    <TextBlock Text="{x:Bind Title}" />
                </Border>
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
        <!-- Default layout is StackLayout (vertical) -->
    </ItemsRepeater>
</ScrollViewer>
```

---

## Uniform Grid Layout

```xaml
<ScrollViewer>
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Photos, Mode=OneWay}">
        <ItemsRepeater.Layout>
            <UniformGridLayout
                MinItemWidth="180"
                MinItemHeight="140"
                MinColumnSpacing="8"
                MinRowSpacing="8" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="local:Photo">
                <Image
                    Source="{x:Bind Uri}"
                    Stretch="UniformToFill"
                    AutomationProperties.Name="{x:Bind Title}" />
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

---

## Horizontal StackLayout

```xaml
<ScrollViewer HorizontalScrollMode="Enabled"
              HorizontalScrollBarVisibility="Auto"
              VerticalScrollMode="Disabled">
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Tags, Mode=OneWay}">
        <ItemsRepeater.Layout>
            <StackLayout Orientation="Horizontal" Spacing="8" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="x:String">
                <Border Padding="8,4" CornerRadius="12"
                        Background="{ThemeResource AccentFillColorDefaultBrush}">
                    <TextBlock Text="{x:Bind}" Foreground="White" />
                </Border>
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

---

## With Item Click Handling

```xaml
<ScrollViewer>
    <ItemsRepeater
        x:Name="Repeater"
        ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
        ElementPrepared="Repeater_ElementPrepared">
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="local:MyItem">
                <Button
                    Content="{x:Bind Title}"
                    HorizontalAlignment="Stretch"
                    AutomationProperties.Name="{x:Bind Title}" />
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollViewer>
```

```csharp
// MainPage.xaml.cs
private void Repeater_ElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
{
    if (args.Element is Button btn)
    {
        btn.Click += (s, e) =>
        {
            var item = ViewModel.Items[args.Index];
            ViewModel.SelectItem(item);
        };
    }
}
```

---

## Notes

- `ItemsRepeater` does **not** include scrolling — always wrap it in a `ScrollViewer`.
- It does **not** include selection — implement your own selection logic.
- Built-in layouts: `StackLayout` (vertical or horizontal), `UniformGridLayout`, `FlowLayout`.
- Use `ItemsRepeater` when `ListView`/`GridView` cannot express your layout (e.g., masonry,
  custom virtualized panels).
- Virtualization is enabled by default when wrapped in a `ScrollViewer`.
- `ElementPrepared` fires when an element is recycled/prepared; use it to wire up events
  and avoid memory leaks (use `ElementClearing` to unwire them).
