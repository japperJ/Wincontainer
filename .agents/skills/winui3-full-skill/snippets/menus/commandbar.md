# CommandBar

`CommandBar` is a toolbar control that houses `AppBarButton`, `AppBarToggleButton`, and `AppBarSeparator` as primary commands, plus overflow secondary commands. Place it at the top or bottom of a page.

## Basic CommandBar

```xaml
<CommandBar DefaultLabelPosition="Right">
    <AppBarButton Click="Add_Click" Icon="Add" Label="Add" />
    <AppBarButton Click="Edit_Click" Icon="Edit" Label="Edit" />
    <AppBarButton Click="Share_Click" Icon="Share" Label="Share" />

    <CommandBar.SecondaryCommands>
        <AppBarButton
            Click="Settings_Click"
            Icon="Setting"
            Label="Settings">
            <AppBarButton.KeyboardAccelerators>
                <KeyboardAccelerator Key="I" Modifiers="Control" />
            </AppBarButton.KeyboardAccelerators>
        </AppBarButton>
        <AppBarSeparator />
        <AppBarButton Click="Help_Click" Icon="Help" Label="Help" />
    </CommandBar.SecondaryCommands>
</CommandBar>
```

## CommandBar with AppBarToggleButton

```xaml
<CommandBar>
    <AppBarToggleButton
        x:Name="boldButton"
        Icon="Bold"
        Label="Bold"
        IsChecked="{x:Bind ViewModel.IsBold, Mode=TwoWay}" />
    <AppBarToggleButton
        x:Name="italicButton"
        Icon="Italic"
        Label="Italic"
        IsChecked="{x:Bind ViewModel.IsItalic, Mode=TwoWay}" />
    <AppBarSeparator />
    <AppBarButton Icon="Copy" Label="Copy" />
    <AppBarButton Icon="Paste" Label="Paste" />
</CommandBar>
```

## CommandBar in page layout (bottom)

```xaml
<Grid RowDefinitions="*,Auto">
    <Frame Grid.Row="0" x:Name="ContentFrame" />
    <CommandBar Grid.Row="1" ClosedDisplayMode="Compact">
        <AppBarButton Icon="Add" Label="New" />
        <AppBarButton Icon="Delete" Label="Delete" />
        <AppBarButton Icon="Refresh" Label="Refresh" />
    </CommandBar>
</Grid>
```

## Dynamic commands from ViewModel

```xaml
<CommandBar>
    <CommandBar.PrimaryCommands>
        <!-- Static always-present commands -->
        <AppBarButton Icon="Refresh" Label="Refresh" Command="{x:Bind ViewModel.RefreshCommand}" />
    </CommandBar.PrimaryCommands>
    <CommandBar.SecondaryCommands>
        <!-- Dynamic secondary commands bound from ViewModel -->
    </CommandBar.SecondaryCommands>
</CommandBar>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    [RelayCommand]
    private void Add() { /* ... */ }

    [RelayCommand]
    private void Edit() { /* ... */ }

    [RelayCommand]
    private void Share() { /* ... */ }

    [RelayCommand]
    private async Task RefreshAsync(CancellationToken ct) { /* ... */ }
}
```

## DefaultLabelPosition values

| Value | Description |
|---|---|
| `Bottom` (default) | Labels below icons, shown when expanded |
| `Right` | Labels to the right of icons, always visible |
| `Collapsed` | No labels shown |

## Notes

- `PrimaryCommands` appear directly on the bar; `SecondaryCommands` appear in the overflow menu (…).
- `AppBarButton.IsCompact = True` hides the label in the unexpanded state.
- `IsOpen` and `IsSticky` can be set to programmatically open the bar and prevent auto-close.
- For page-level toolbars, bind each button's `Command` to a ViewModel `RelayCommand`; avoid Click event handlers.
- `CommandBar` placed in a `Page.TopAppBar` or `Page.BottomAppBar` integrates with the page chrome.
