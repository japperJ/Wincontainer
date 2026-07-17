# Flyout

`Flyout` is a lightweight contextual pop-up that shows custom content anchored to a control.
Use it for overflow menus, mini-forms, color pickers, and any transient contextual UI.

---

## Basic Flyout on a Button

```xaml
<Button
    Content="Options"
    AutomationProperties.Name="Options button">
    <Button.Flyout>
        <Flyout>
            <StackPanel Width="200" Spacing="8">
                <TextBlock Text="Flyout content" Style="{ThemeResource BodyStrongTextBlockStyle}" />
                <Button Content="Action 1" HorizontalAlignment="Stretch" />
                <Button Content="Action 2" HorizontalAlignment="Stretch" />
            </StackPanel>
        </Flyout>
    </Button.Flyout>
</Button>
```

---

## MenuFlyout (Contextual Menu)

```xaml
<Button
    Content="File"
    AutomationProperties.Name="File menu">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem Text="New" Icon="Document" />
            <MenuFlyoutItem Text="Open" Icon="OpenFile" />
            <MenuFlyoutSeparator />
            <MenuFlyoutSubItem Text="Export">
                <MenuFlyoutItem Text="PDF" />
                <MenuFlyoutItem Text="HTML" />
                <MenuFlyoutItem Text="Markdown" />
            </MenuFlyoutSubItem>
            <MenuFlyoutSeparator />
            <MenuFlyoutItem Text="Close" Icon="Cancel" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

---

## Right-Click Context Flyout on a ListViewItem

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:MyItem">
            <TextBlock Text="{x:Bind Name}">
                <TextBlock.ContextFlyout>
                    <MenuFlyout>
                        <MenuFlyoutItem Text="Open" Icon="OpenFile" />
                        <MenuFlyoutItem Text="Copy" Icon="Copy" />
                        <MenuFlyoutSeparator />
                        <MenuFlyoutItem Text="Delete" Icon="Delete" />
                    </MenuFlyout>
                </TextBlock.ContextFlyout>
            </TextBlock>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

---

## Programmatic Flyout Show / Hide

```csharp
// Show a flyout programmatically anchored to a button
var flyout = new Flyout
{
    Content = new TextBlock { Text = "Saved!", Padding = new Thickness(12) }
};
flyout.ShowAt(SaveButton);

// Hide after 2 seconds
await Task.Delay(2000);
flyout.Hide();
```

---

## MVVM — Commands in MenuFlyout

```xaml
<Button Content="Item actions" AutomationProperties.Name="Item actions">
    <Button.Flyout>
        <MenuFlyout>
            <MenuFlyoutItem
                Text="Edit"
                Command="{x:Bind ViewModel.EditCommand}"
                CommandParameter="{x:Bind ViewModel.SelectedItem, Mode=OneWay}" />
            <MenuFlyoutItem
                Text="Share"
                Command="{x:Bind ViewModel.ShareCommand}"
                CommandParameter="{x:Bind ViewModel.SelectedItem, Mode=OneWay}" />
            <MenuFlyoutSeparator />
            <MenuFlyoutItem
                Text="Delete"
                Command="{x:Bind ViewModel.DeleteCommand}"
                CommandParameter="{x:Bind ViewModel.SelectedItem, Mode=OneWay}" />
        </MenuFlyout>
    </Button.Flyout>
</Button>
```

---

## Notes

- `Flyout` accepts any XAML content; `MenuFlyout` is a specialized flyout for command lists.
- `Placement` controls where the flyout anchors: `Bottom`, `Top`, `Left`, `Right`,
  `BottomEdgeAlignedLeft`, `BottomEdgeAlignedRight`, etc.
- `ContextFlyout` on any `UIElement` replaces the default right-click / long-press menu.
- `FlyoutPresenterStyle` lets you customize the flyout container's padding, corner radius, etc.
- Flyouts close automatically when the user taps/clicks outside; call `Hide()` to close programmatically.
- For modal decisions, use `ContentDialog` instead.
