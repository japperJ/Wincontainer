# SplitView

`SplitView` provides a two-area layout with a collapsible pane (left or right) and a main content area. It is the building block behind `NavigationView`; use it directly when you need full control over pane content and behaviour.

## Basic SplitView

```xaml
<SplitView
    x:Name="MainSplitView"
    CompactPaneLength="48"
    DisplayMode="CompactOverlay"
    IsPaneOpen="True"
    OpenPaneLength="256"
    PaneBackground="{ThemeResource NavigationViewDefaultPaneBackground}">

    <SplitView.Pane>
        <StackPanel Padding="8">
            <TextBlock
                Margin="12,12,0,8"
                Style="{ThemeResource CaptionTextBlockStyle}"
                Text="NAVIGATION" />
            <ListView
                x:Name="NavList"
                IsItemClickEnabled="True"
                ItemClick="NavList_ItemClick"
                ItemsSource="{x:Bind ViewModel.NavItems}"
                SelectionMode="Single">
                <ListView.ItemTemplate>
                    <DataTemplate x:DataType="local:NavItem">
                        <Grid ColumnDefinitions="Auto,*" ColumnSpacing="12">
                            <FontIcon
                                Grid.Column="0"
                                FontSize="16"
                                Glyph="{x:Bind Glyph}" />
                            <TextBlock
                                Grid.Column="1"
                                VerticalAlignment="Center"
                                Text="{x:Bind Label}" />
                        </Grid>
                    </DataTemplate>
                </ListView.ItemTemplate>
            </ListView>
        </StackPanel>
    </SplitView.Pane>

    <!-- Main content -->
    <Frame x:Name="ContentFrame" />
</SplitView>
```

```csharp
private void NavList_ItemClick(object sender, ItemClickEventArgs e)
{
    var item = e.ClickedItem as NavItem;
    ContentFrame.Navigate(item?.PageType);

    // Close pane in Overlay mode after navigation
    if (MainSplitView.DisplayMode == SplitViewDisplayMode.Overlay)
        MainSplitView.IsPaneOpen = false;
}
```

## Toggling the pane

```xaml
<ToggleButton
    Content="&#xE700;"
    FontFamily="{ThemeResource SymbolThemeFontFamily}"
    IsChecked="{x:Bind MainSplitView.IsPaneOpen, Mode=TwoWay}" />
```

## DisplayMode options

```xaml
<!-- Pane overlays content when open -->
<SplitView DisplayMode="Overlay" />

<!-- Pane overlays content; shows compact strip when closed -->
<SplitView DisplayMode="CompactOverlay" CompactPaneLength="48" />

<!-- Pane pushes content when open -->
<SplitView DisplayMode="Inline" />

<!-- Pane pushes content; shows compact strip when closed -->
<SplitView DisplayMode="CompactInline" CompactPaneLength="48" />
```

## Right-side pane

```xaml
<SplitView PanePlacement="Right" DisplayMode="Overlay" OpenPaneLength="320">
    <SplitView.Pane>
        <StackPanel Padding="16">
            <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}" Text="Details" />
        </StackPanel>
    </SplitView.Pane>
    <Grid><!-- main content --></Grid>
</SplitView>
```

## Notes

- Prefer `NavigationView` over raw `SplitView` unless you need custom pane layout that NavigationView cannot provide.
- `IsPaneOpen` is two-way bindable; bind `TwoWay` to track state in ViewModel.
- `CompactPaneLength` defines the width of the visible strip when closed (for `CompactOverlay`/`CompactInline` modes).
- `PaneBackground` defaults to the system navigation pane colour; use `ThemeResource` to match system styles.
- The `Pane` property accepts any single UIElement as content.
