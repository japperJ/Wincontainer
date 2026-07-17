# AppBarButton

`AppBarButton` is an icon button designed for use in `CommandBar`. It combines an icon with a text label and supports flyouts, keyboard accelerators, and compact mode.

## Basic AppBarButton variants

```xaml
<!-- Symbol icon -->
<AppBarButton Icon="Like" Label="Like" Click="Like_Click" />

<!-- Font icon -->
<AppBarButton Label="Sigma">
    <AppBarButton.Icon>
        <FontIcon FontFamily="Candara" Glyph="&#x03A3;" />
    </AppBarButton.Icon>
</AppBarButton>

<!-- Bitmap icon -->
<AppBarButton Label="Custom">
    <AppBarButton.Icon>
        <BitmapIcon UriSource="ms-appx:///Assets/custom.png" />
    </AppBarButton.Icon>
</AppBarButton>

<!-- Path icon -->
<AppBarButton Label="Shape">
    <AppBarButton.Content>
        <Viewbox Stretch="Uniform">
            <PathIcon Data="F1 M 20,20L 24,10L 24,24L 5,24" />
        </Viewbox>
    </AppBarButton.Content>
</AppBarButton>
```

## AppBarButton with keyboard accelerator

```xaml
<AppBarButton Icon="Save" Label="Save" Click="Save_Click">
    <AppBarButton.KeyboardAccelerators>
        <KeyboardAccelerator Key="S" Modifiers="Control" />
    </AppBarButton.KeyboardAccelerators>
</AppBarButton>
```

## AppBarButton with flyout (input)

```xaml
<AppBarButton
    AllowFocusOnInteraction="True"
    Icon="Edit"
    Label="Edit">
    <AppBarButton.Flyout>
        <Flyout>
            <TextBox MinWidth="240" PlaceholderText="Enter text…" />
        </Flyout>
    </AppBarButton.Flyout>
</AppBarButton>
```

## AppBarButton with MenuFlyout

```xaml
<AppBarButton
    AutomationProperties.Name="Sort options"
    Icon="Sort"
    IsCompact="True"
    Label="Sort"
    ToolTipService.ToolTip="Sort">
    <AppBarButton.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="By name" />
            <MenuFlyoutItem Text="By date" />
            <MenuFlyoutItem Text="By size" />
        </MenuFlyout>
    </AppBarButton.Flyout>
</AppBarButton>
```

## AppBarButton bound to ViewModel command

```xaml
<CommandBar>
    <AppBarButton
        Command="{x:Bind ViewModel.DeleteCommand}"
        Icon="Delete"
        Label="Delete"
        ToolTipService.ToolTip="Delete selected items" />
    <AppBarButton
        Command="{x:Bind ViewModel.ShareCommand}"
        Icon="Share"
        Label="Share"
        ToolTipService.ToolTip="Share" />
</CommandBar>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class GalleryViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeleteCommand))]
    private bool _hasSelection;

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void Delete() { /* delete selected items */ }

    [RelayCommand]
    private void Share() { /* share */ }
}
```

## AppBarToggleButton

```xaml
<AppBarToggleButton
    Icon="Shuffle"
    IsChecked="{x:Bind ViewModel.IsShuffle, Mode=TwoWay}"
    Label="Shuffle"
    ToolTipService.ToolTip="Toggle shuffle" />
```

## Notes

- `IsCompact="True"` hides the label, showing only the icon — useful when labels are shown by `DefaultLabelPosition="Right"` on the parent `CommandBar`.
- Always set `AutomationProperties.Name` when `IsCompact="True"` so screen readers announce the button purpose.
- `AllowFocusOnInteraction="True"` is required on `AppBarButton` when its flyout contains input controls (TextBox, etc.).
- Prefer `Command` binding over Click event handlers; use `CanExecute` to enable/disable the button.
- `AppBarSeparator` adds a visual divider between groups in a `CommandBar`.
