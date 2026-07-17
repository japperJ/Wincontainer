# MenuFlyout

`MenuFlyout` is a contextual popup menu attached to a control's `Flyout` property or shown programmatically. It contains `MenuFlyoutItem`, `ToggleMenuFlyoutItem`, `RadioMenuFlyoutItem`, `MenuFlyoutSubItem`, and `MenuFlyoutSeparator`.

## Basic MenuFlyout on a Button

```xaml
<Button Content="Options">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Cut" Icon="Cut" />
            <MenuFlyoutItem Text="Copy" Icon="Copy" />
            <MenuFlyoutItem Text="Paste" Icon="Paste" />
            <MenuFlyoutSeparator />
            <MenuFlyoutItem Text="Select all" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## MenuFlyout on AppBarButton (sort menu)

```xaml
<AppBarButton
    AutomationProperties.Name="Sort"
    Icon="Sort"
    IsCompact="True"
    ToolTipService.ToolTip="Sort">
    <AppBarButton.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem
                Click="SortByRating_Click"
                Tag="rating"
                Text="By rating" />
            <MenuFlyoutItem
                Click="SortByDate_Click"
                Tag="date"
                Text="By date" />
            <MenuFlyoutItem
                Click="SortByName_Click"
                Tag="name"
                Text="By name" />
        </MenuFlyout>
    </AppBarButton.Flyout>
</AppBarButton>
```

## MenuFlyout with toggles and separators

```xaml
<Button Content="Playback">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Reset" />
            <MenuFlyoutSeparator />
            <ToggleMenuFlyoutItem
                x:Name="RepeatItem"
                IsChecked="{x:Bind ViewModel.IsRepeatEnabled, Mode=TwoWay}"
                Text="Repeat" />
            <ToggleMenuFlyoutItem
                x:Name="ShuffleItem"
                IsChecked="{x:Bind ViewModel.IsShuffleEnabled, Mode=TwoWay}"
                Text="Shuffle" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## MenuFlyout with cascading sub-menus

```xaml
<Button Content="File Options">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Open" />
            <MenuFlyoutSubItem Text="Send to">
                <MenuFlyoutItem Text="Bluetooth" />
                <MenuFlyoutItem Text="Desktop (shortcut)" />
                <MenuFlyoutSubItem Text="Compressed file">
                    <MenuFlyoutItem Text="Compress and email" />
                    <MenuFlyoutItem Text="Compress to .zip" />
                </MenuFlyoutSubItem>
            </MenuFlyoutSubItem>
            <MenuFlyoutSeparator />
            <MenuFlyoutItem Text="Delete" Icon="Delete" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## MenuFlyout with keyboard accelerators

```xaml
<Button Content="Edit">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Icon="Copy" Text="Copy">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="C" Modifiers="Control" />
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutItem Icon="Cut" Text="Cut">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="X" Modifiers="Control" />
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
            <MenuFlyoutItem Icon="Paste" Text="Paste">
                <MenuFlyoutItem.KeyboardAccelerators>
                    <KeyboardAccelerator Key="V" Modifiers="Control" />
                </MenuFlyoutItem.KeyboardAccelerators>
            </MenuFlyoutItem>
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## RadioMenuFlyoutItem group

```xaml
<Button Content="View options">
    <Button.Flyout>
        <MenuFlyout>
            <RadioMenuFlyoutItem GroupName="LayoutGroup" Text="List" />
            <RadioMenuFlyoutItem
                GroupName="LayoutGroup"
                IsChecked="True"
                Text="Grid" />
            <RadioMenuFlyoutItem GroupName="LayoutGroup" Text="Details" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

## Programmatic show (context menu)

```xaml
<Image
    x:Name="Photo"
    Source="ms-appx:///Assets/photo.jpg"
    RightTapped="Photo_RightTapped" />
```

```csharp
private void Photo_RightTapped(object sender, RightTappedRoutedEventArgs e)
{
    var menu = new MenuFlyout();
    menu.Items.Add(new MenuFlyoutItem { Text = "Save", Icon = new SymbolIcon(Symbol.Save) });
    menu.Items.Add(new MenuFlyoutItem { Text = "Share", Icon = new SymbolIcon(Symbol.Share) });
    menu.ShowAt(Photo, e.GetPosition(Photo));
}
```

## Notes

- `MenuFlyout` is a `Flyout` subtype — assign to `.Flyout` (not `.ContextFlyout`) for button-triggered menus.
- For right-click context menus, assign the `MenuFlyout` to `UIElement.ContextFlyout`.
- `Icon` on `MenuFlyoutItem` accepts `SymbolIcon`, `FontIcon`, `BitmapIcon`, or `PathIcon`.
- `RadioMenuFlyoutItem` groups enforce single-selection within the same `GroupName`.
- Prefer `Command` binding on `MenuFlyoutItem` over Click event handlers.
