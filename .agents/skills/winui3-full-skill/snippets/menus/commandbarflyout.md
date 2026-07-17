# CommandBarFlyout

`CommandBarFlyout` combines a compact toolbar (`CommandBar`) with an expandable overflow area in a floating flyout. It is ideal for contextual commands on selected items such as images, text, or list entries.

## Basic CommandBarFlyout

```xaml
<Page.Resources>
    <CommandBarFlyout x:Name="PhotoFlyout" Placement="Right">
        <AppBarButton
            Click="Share_Click"
            Icon="Share"
            Label="Share"
            ToolTipService.ToolTip="Share" />
        <AppBarButton
            Click="Save_Click"
            Icon="Save"
            Label="Save"
            ToolTipService.ToolTip="Save" />
        <AppBarButton
            Click="Delete_Click"
            Icon="Delete"
            Label="Delete"
            ToolTipService.ToolTip="Delete" />

        <CommandBarFlyout.SecondaryCommands>
            <AppBarButton Click="Resize_Click" Label="Resize" />
            <AppBarButton Click="Move_Click" Label="Move" />
            <AppBarButton Click="Rename_Click" Label="Rename" />
        </CommandBarFlyout.SecondaryCommands>
    </CommandBarFlyout>
</Page.Resources>

<!-- Attach to image: click opens collapsed flyout, right-click opens expanded -->
<Button
    x:Name="PhotoButton"
    Padding="0"
    AutomationProperties.Name="Photo"
    Click="PhotoButton_Click"
    ContextRequested="PhotoButton_ContextRequested">
    <Image Height="300" Source="ms-appx:///Assets/photo.jpg" />
</Button>
```

```csharp
// Click: show collapsed (icon-only) flyout
private void PhotoButton_Click(object sender, RoutedEventArgs e)
{
    PhotoFlyout.ShowMode = FlyoutShowMode.Transient;
    PhotoFlyout.ShowAt(PhotoButton);
}

// Right-click: show expanded flyout with all commands visible
private void PhotoButton_ContextRequested(UIElement sender, ContextRequestedEventArgs e)
{
    PhotoFlyout.ShowMode = FlyoutShowMode.Standard;
    if (e.TryGetPosition(sender, out var pos))
        PhotoFlyout.ShowAt(PhotoButton, pos);
    else
        PhotoFlyout.ShowAt(PhotoButton);
}
```

## CommandBarFlyout with ViewModel commands

```xaml
<Page.Resources>
    <CommandBarFlyout x:Name="ItemFlyout">
        <AppBarButton
            Command="{x:Bind ViewModel.EditCommand}"
            Icon="Edit"
            Label="Edit" />
        <AppBarButton
            Command="{x:Bind ViewModel.ShareCommand}"
            Icon="Share"
            Label="Share" />
        <AppBarButton
            Command="{x:Bind ViewModel.DeleteCommand}"
            Icon="Delete"
            Label="Delete" />
    </CommandBarFlyout>
</Page.Resources>
```

## Text selection CommandBarFlyout

```xaml
<RichEditBox
    x:Name="Editor"
    SelectionFlyout="{x:Null}">  <!-- Suppress default flyout -->
    <RichEditBox.ContextFlyout>
        <CommandBarFlyout>
            <AppBarButton Icon="Copy" Label="Copy" />
            <AppBarButton Icon="Cut" Label="Cut" />
            <AppBarButton Icon="Paste" Label="Paste" />
            <CommandBarFlyout.SecondaryCommands>
                <AppBarButton Label="Select all" />
                <AppBarButton Label="Translate" />
            </CommandBarFlyout.SecondaryCommands>
        </CommandBarFlyout>
    </RichEditBox.ContextFlyout>
</RichEditBox>
```

## Notes

- `PrimaryCommands` appear as icon buttons; `SecondaryCommands` appear in the expanded overflow area.
- `ShowMode = FlyoutShowMode.Transient` shows only primary icon buttons (collapsed state).
- `ShowMode = FlyoutShowMode.Standard` shows both primary and secondary commands expanded.
- For `ContextFlyout`, assign the `CommandBarFlyout` to `UIElement.ContextFlyout` for right-click/long-press behaviour.
- `Placement` controls the preferred direction: `Right`, `Left`, `Top`, `Bottom`, `Auto`.
- Always set `ToolTipService.ToolTip` on icon-only `AppBarButton`s so the user can identify them.
