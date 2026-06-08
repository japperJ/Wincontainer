# ToolTip

`ToolTip` displays a small informational popup when the user hovers over or focuses a control. Use `ToolTipService.ToolTip` for simple string tooltips, or the `<ToolTip>` element for rich content or custom placement.

## Simple string tooltip (recommended)

```xaml
<Button
    Content="Save"
    ToolTipService.ToolTip="Save the current document (Ctrl+S)" />
```

## Tooltip with offset

```xaml
<TextBlock Text="Hover over me">
    <ToolTipService.ToolTip>
        <ToolTip Content="Offset ToolTip" VerticalOffset="-80" />
    </ToolTipService.ToolTip>
</TextBlock>
```

## Tooltip with custom placement

```xaml
<Image
    Width="300"
    Height="200"
    Source="ms-appx:///Assets/photo.jpg">
    <ToolTipService.ToolTip>
        <ToolTip
            Content="Non-occluding tooltip — stays to the right"
            Placement="Right"
            PlacementRect="0,0,300,200" />
    </ToolTipService.ToolTip>
</Image>
```

## Tooltip with rich content

```xaml
<Button AutomationProperties.Name="Color picker" Content="Pick color">
    <ToolTipService.ToolTip>
        <ToolTip>
            <StackPanel Spacing="4">
                <TextBlock FontWeight="SemiBold" Text="Color Picker" />
                <TextBlock Text="Opens a dialog to select a color from the palette." />
            </StackPanel>
        </ToolTip>
    </ToolTipService.ToolTip>
</Button>
```

## Tooltip bound to dynamic content

```xaml
<Button
    Content="Details"
    ToolTipService.ToolTip="{x:Bind ViewModel.TooltipText, Mode=OneWay}" />
```

## Programmatic tooltip (code-behind)

```csharp
var tooltip = new ToolTip { Content = "Dynamic tooltip" };
ToolTipService.SetToolTip(myButton, tooltip);
```

## Placement options

| Value | Description |
|---|---|
| `Mouse` (default) | Appears near cursor |
| `Top` | Above the target element |
| `Bottom` | Below the target element |
| `Left` | Left of the target element |
| `Right` | Right of the target element |

## Notes

- `ToolTipService.ToolTip="string"` is the simplest usage and works for plain text.
- `ToolTip.Placement` combined with `PlacementRect` prevents the tooltip from occluding the element it describes.
- Tooltips do **not** appear on touch — ensure all interactive controls also have visible labels or `AutomationProperties.Name`.
- Do not put interactive content (buttons, links) inside a tooltip; use a `Flyout` instead.
- `ToolTipService.InitialShowDelay` and `ToolTipService.ShowDuration` can be set as attached properties to control timing.
