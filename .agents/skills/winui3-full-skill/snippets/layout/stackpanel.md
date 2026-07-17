# StackPanel

`StackPanel` arranges children sequentially in a single row or column. It is the simplest layout panel and suitable for linear arrangements of controls.

## Vertical StackPanel (default)

```xaml
<StackPanel Spacing="8">
    <TextBlock Style="{ThemeResource TitleTextBlockStyle}" Text="Settings" />
    <ToggleSwitch Header="Dark mode" />
    <ToggleSwitch Header="Notifications" />
    <Button Content="Save" />
</StackPanel>
```

## Horizontal StackPanel

```xaml
<StackPanel Orientation="Horizontal" Spacing="12">
    <Button Content="Cancel" />
    <Button Content="OK" Style="{ThemeResource AccentButtonStyle}" />
</StackPanel>
```

## Nested StackPanels

```xaml
<StackPanel Spacing="16">
    <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}" Text="Account" />
    <StackPanel Orientation="Horizontal" Spacing="12">
        <PersonPicture DisplayName="Ada Lovelace" />
        <StackPanel VerticalAlignment="Center">
            <TextBlock Style="{ThemeResource BodyStrongTextBlockStyle}" Text="Ada Lovelace" />
            <TextBlock
                Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                Style="{ThemeResource CaptionTextBlockStyle}"
                Text="ada@example.com" />
        </StackPanel>
    </StackPanel>
</StackPanel>
```

## StackPanel as ItemsPanel

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Items}">
    <ListView.ItemsPanel>
        <ItemsPanelTemplate>
            <StackPanel Orientation="Horizontal" />
        </ItemsPanelTemplate>
    </ListView.ItemsPanel>
</ListView>
```

## Notes

- `Spacing` (default `0`) adds uniform gaps between children — prefer this over per-item `Margin` for cleaner XAML.
- `Orientation="Vertical"` is the default; use `Horizontal` for toolbars and button rows.
- `StackPanel` does **not** wrap; use `WrapGrid` or `VariableSizedWrapGrid` if items should wrap.
- For two-dimensional layouts use `Grid`. For proportional sizing use `Grid` with `*` columns.
- `HorizontalAlignment` / `VerticalAlignment` on the `StackPanel` itself controls how it positions within its parent.
- Avoid using `StackPanel` inside a `ScrollViewer` for large item counts — it does not virtualise. Use `ListView` / `ItemsRepeater` instead.
