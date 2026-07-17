# DropDownButton

`DropDownButton` is a `Button` with a built-in chevron that opens a `MenuFlyout`.
Use it when a single action can be expanded into multiple related options.

---

## Basic DropDownButton

```xaml
<DropDownButton
    Content="Email"
    AutomationProperties.Name="Email actions">
    <DropDownButton.Flyout>
        <MenuFlyout Placement="BottomEdgeAlignedLeft">
            <MenuFlyoutItem Text="Send" />
            <MenuFlyoutItem Text="Reply" />
            <MenuFlyoutItem Text="Reply All" />
        </MenuFlyout>
    </DropDownButton.Flyout>
</DropDownButton>
```

---

## With Icons in Menu Items

```xaml
<DropDownButton AutomationProperties.Name="Email">
    <DropDownButton.Content>
        <FontIcon Glyph="&#xE715;" />
    </DropDownButton.Content>
    <DropDownButton.Flyout>
        <MenuFlyout Placement="BottomEdgeAlignedLeft">
            <MenuFlyoutItem Text="Send">
                <MenuFlyoutItem.Icon>
                    <FontIcon Glyph="&#xE725;" />
                </MenuFlyoutItem.Icon>
            </MenuFlyoutItem>
            <MenuFlyoutItem Text="Reply">
                <MenuFlyoutItem.Icon>
                    <FontIcon Glyph="&#xE8CA;" />
                </MenuFlyoutItem.Icon>
            </MenuFlyoutItem>
            <MenuFlyoutSeparator />
            <MenuFlyoutItem Text="Delete">
                <MenuFlyoutItem.Icon>
                    <FontIcon Glyph="&#xE74D;" />
                </MenuFlyoutItem.Icon>
            </MenuFlyoutItem>
        </MenuFlyout>
    </DropDownButton.Flyout>
</DropDownButton>
```

---

## MVVM — Commands in MenuFlyoutItems

```xaml
<!-- View -->
<DropDownButton
    Content="Actions"
    AutomationProperties.Name="Item actions">
    <DropDownButton.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem
                Text="Edit"
                Command="{x:Bind ViewModel.EditCommand}" />
            <MenuFlyoutItem
                Text="Duplicate"
                Command="{x:Bind ViewModel.DuplicateCommand}" />
            <MenuFlyoutSeparator />
            <MenuFlyoutItem
                Text="Delete"
                Command="{x:Bind ViewModel.DeleteCommand}" />
        </MenuFlyout>
    </DropDownButton.Flyout>
</DropDownButton>
```

```csharp
// ViewModels/ItemViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ItemViewModel : ObservableObject
{
    [RelayCommand]
    private void Edit() { /* ... */ }

    [RelayCommand]
    private void Duplicate() { /* ... */ }

    [RelayCommand]
    private void Delete() { /* ... */ }
}
```

---

## Notes

- `DropDownButton` is purely a disclosure button — the primary click only opens the flyout.
  If you need a primary action + a flyout, use `SplitButton` instead.
- Set `Placement="BottomEdgeAlignedLeft"` on the `MenuFlyout` so it aligns with the button edge.
- Always provide `AutomationProperties.Name` especially when using an icon-only button.
- Keyboard: `Alt+Down` or `Space`/`Enter` opens the flyout.
