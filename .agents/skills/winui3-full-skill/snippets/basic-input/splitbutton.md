# SplitButton

`SplitButton` has two parts: a primary invoke area that triggers a default action, and
a chevron that opens a flyout for selecting alternatives. Use it for "Send" with options,
"New" with document types, or font-color pickers.

---

## Basic SplitButton

```xaml
<SplitButton
    Content="Send"
    Click="SendButton_Click"
    AutomationProperties.Name="Send message">
    <SplitButton.Flyout>
        <MenuFlyout Placement="BottomEdgeAlignedLeft">
            <MenuFlyoutItem Text="Send now" />
            <MenuFlyoutItem Text="Schedule send" />
            <MenuFlyoutItem Text="Save as draft" />
        </MenuFlyout>
    </SplitButton.Flyout>
</SplitButton>
```

```csharp
// MainPage.xaml.cs
private void SendButton_Click(SplitButton sender, SplitButtonClickEventArgs e)
{
    // Primary action: send immediately
    MessageService.SendAsync();
}
```

---

## Icon SplitButton (Color Picker Pattern)

```xaml
<SplitButton
    x:Name="ColorButton"
    AutomationProperties.Name="Font color"
    Click="ColorButton_Click">
    <Border
        x:Name="CurrentColorSwatch"
        Width="32"
        Height="32"
        Background="Green"
        CornerRadius="4,0,0,4" />
    <SplitButton.Flyout>
        <Flyout Placement="Bottom">
            <GridView
                IsItemClickEnabled="True"
                ItemClick="ColorPicker_ItemClick">
                <GridView.ItemsPanel>
                    <ItemsPanelTemplate>
                        <ItemsWrapGrid MaximumRowsOrColumns="3" Orientation="Horizontal" />
                    </ItemsPanelTemplate>
                </GridView.ItemsPanel>
                <GridView.Items>
                    <Rectangle Width="32" Height="32" RadiusX="4" RadiusY="4"
                               AutomationProperties.Name="Red" Fill="Red" />
                    <Rectangle Width="32" Height="32" RadiusX="4" RadiusY="4"
                               AutomationProperties.Name="Green" Fill="Green" />
                    <Rectangle Width="32" Height="32" RadiusX="4" RadiusY="4"
                               AutomationProperties.Name="Blue" Fill="Blue" />
                </GridView.Items>
            </GridView>
        </Flyout>
    </SplitButton.Flyout>
</SplitButton>
```

```csharp
// MainPage.xaml.cs
private void ColorButton_Click(SplitButton sender, SplitButtonClickEventArgs e)
{
    // Apply the currently selected color
    ApplyColor(CurrentColorSwatch.Background);
}

private void ColorPicker_ItemClick(object sender, ItemClickEventArgs e)
{
    if (e.ClickedItem is Rectangle rect && rect.Fill is SolidColorBrush brush)
    {
        CurrentColorSwatch.Background = brush;
        ApplyColor(brush);
        ColorButton.Flyout.Hide();
    }
}
```

---

## MVVM Pattern

```xaml
<!-- View -->
<SplitButton
    Command="{x:Bind ViewModel.DefaultActionCommand}"
    AutomationProperties.Name="New document">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <FontIcon Glyph="&#xE8A5;" FontSize="16" />
        <TextBlock Text="New" />
    </StackPanel>
    <SplitButton.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="Blank document"
                Command="{x:Bind ViewModel.NewBlankCommand}" />
            <MenuFlyoutItem Text="From template"
                Command="{x:Bind ViewModel.NewFromTemplateCommand}" />
        </MenuFlyout>
    </SplitButton.Flyout>
</SplitButton>
```

```csharp
// ViewModels/DocumentViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DocumentViewModel : ObservableObject
{
    [RelayCommand]
    private void DefaultAction() => NewBlank();

    [RelayCommand]
    private void NewBlank() { /* create blank document */ }

    [RelayCommand]
    private void NewFromTemplate() { /* show template picker */ }
}
```

---

## Notes

- The `Click` event on `SplitButton` only fires when the **primary** (left) area is clicked —
  not when the chevron is clicked.
- Use `SplitButton` over `DropDownButton` when there is a clear default action.
- Use `ToggleSplitButton` (see `togglesplitbutton.md`) when the primary area should toggle state.
- `SplitButtonClickEventArgs` has no useful properties — use the sender to identify the button.
