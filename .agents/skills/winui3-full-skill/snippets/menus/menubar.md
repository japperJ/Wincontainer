# MenuBar

`MenuBar` provides a traditional desktop-style menu bar (File, Edit, View, Help…). Each `MenuBarItem` acts as a top-level menu; its children are `MenuFlyoutItem`, `MenuFlyoutSubItem`, `MenuFlyoutSeparator`, `ToggleMenuFlyoutItem`, or `RadioMenuFlyoutItem`.

## Basic MenuBar

```xaml
<MenuBar>
    <MenuBarItem Title="File">
        <MenuFlyoutItem Click="New_Click" Text="New" />
        <MenuFlyoutItem Click="Open_Click" Text="Open" />
        <MenuFlyoutItem Click="Save_Click" Text="Save" />
        <MenuFlyoutSeparator />
        <MenuFlyoutItem Click="Exit_Click" Text="Exit" />
    </MenuBarItem>

    <MenuBarItem Title="Edit">
        <MenuFlyoutItem Click="Undo_Click" Text="Undo" />
        <MenuFlyoutItem Click="Cut_Click" Text="Cut" />
        <MenuFlyoutItem Click="Copy_Click" Text="Copy" />
        <MenuFlyoutItem Click="Paste_Click" Text="Paste" />
    </MenuBarItem>

    <MenuBarItem Title="Help">
        <MenuFlyoutItem Click="About_Click" Text="About" />
    </MenuBarItem>
</MenuBar>
```

## MenuBar with keyboard accelerators

```xaml
<MenuBar>
    <MenuBarItem Title="File">
        <MenuFlyoutItem Text="New">
            <MenuFlyoutItem.KeyboardAccelerators>
                <KeyboardAccelerator Key="N" Modifiers="Control" />
            </MenuFlyoutItem.KeyboardAccelerators>
        </MenuFlyoutItem>
        <MenuFlyoutItem Text="Open">
            <MenuFlyoutItem.KeyboardAccelerators>
                <KeyboardAccelerator Key="O" Modifiers="Control" />
            </MenuFlyoutItem.KeyboardAccelerators>
        </MenuFlyoutItem>
        <MenuFlyoutItem Text="Save">
            <MenuFlyoutItem.KeyboardAccelerators>
                <KeyboardAccelerator Key="S" Modifiers="Control" />
            </MenuFlyoutItem.KeyboardAccelerators>
        </MenuFlyoutItem>
    </MenuBarItem>
</MenuBar>
```

## MenuBar with submenus, separators, and radio items

```xaml
<MenuBar>
    <MenuBarItem Title="File">
        <MenuFlyoutSubItem Text="New">
            <MenuFlyoutItem Text="Plain Text Document" />
            <MenuFlyoutItem Text="Rich Text Document" />
        </MenuFlyoutSubItem>
        <MenuFlyoutItem Text="Open" />
        <MenuFlyoutItem Text="Save" />
        <MenuFlyoutSeparator />
        <MenuFlyoutItem Text="Exit" />
    </MenuBarItem>

    <MenuBarItem Title="View">
        <MenuFlyoutItem Text="Output" />
        <MenuFlyoutSeparator />
        <RadioMenuFlyoutItem
            GroupName="LayoutGroup"
            Text="Landscape" />
        <RadioMenuFlyoutItem
            GroupName="LayoutGroup"
            IsChecked="True"
            Text="Portrait" />
        <MenuFlyoutSeparator />
        <RadioMenuFlyoutItem GroupName="SizeGroup" Text="Small icons" />
        <RadioMenuFlyoutItem
            GroupName="SizeGroup"
            IsChecked="True"
            Text="Medium icons" />
        <RadioMenuFlyoutItem GroupName="SizeGroup" Text="Large icons" />
    </MenuBarItem>
</MenuBar>
```

## MenuBar with ViewModel commands

```xaml
<MenuBar>
    <MenuBarItem Title="File">
        <MenuFlyoutItem Command="{x:Bind ViewModel.NewCommand}" Text="New" />
        <MenuFlyoutItem Command="{x:Bind ViewModel.OpenCommand}" Text="Open" />
        <MenuFlyoutItem Command="{x:Bind ViewModel.SaveCommand}" Text="Save" />
    </MenuBarItem>
</MenuBar>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    [RelayCommand]
    private void New() { /* create new document */ }

    [RelayCommand]
    private async Task OpenAsync(CancellationToken ct) { /* open file picker */ }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken ct) { /* save file */ }
}
```

## Notes

- `MenuBar` should be placed at the very top of the page layout, before other content.
- `MenuBarItem.Title` is the visible top-level label (e.g. "File").
- `KeyboardAccelerator` shows the shortcut hint in the menu automatically.
- `RadioMenuFlyoutItem` with the same `GroupName` behaves as a radio group — only one can be checked.
- `ToggleMenuFlyoutItem.IsChecked` is two-way bindable.
- Prefer `Command` bindings over Click event handlers to keep business logic in ViewModels.
