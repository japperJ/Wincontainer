# RelativePanel

`RelativePanel` arranges children by specifying their position relative to other named children or to the panel edges. It is useful for adaptive layouts where relationships between elements matter more than fixed positions.

## Basic RelativePanel

```xaml
<RelativePanel>
    <TextBlock
        x:Name="titleBlock"
        RelativePanel.AlignTopWithPanel="True"
        RelativePanel.AlignLeftWithPanel="True"
        Style="{ThemeResource SubtitleTextBlockStyle}"
        Text="Title" />

    <TextBlock
        x:Name="subtitleBlock"
        Margin="0,4,0,0"
        RelativePanel.AlignLeftWithPanel="True"
        RelativePanel.Below="titleBlock"
        Text="Subtitle text" />

    <Button
        x:Name="actionButton"
        Content="Action"
        RelativePanel.AlignRightWithPanel="True"
        RelativePanel.AlignVerticalCenterWith="titleBlock" />
</RelativePanel>
```

## Card layout using RelativePanel

```xaml
<RelativePanel
    Padding="16"
    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="8">

    <Image
        x:Name="thumb"
        Width="80"
        Height="80"
        RelativePanel.AlignLeftWithPanel="True"
        RelativePanel.AlignTopWithPanel="True"
        Source="ms-appx:///Assets/thumb.png"
        Stretch="UniformToFill" />

    <TextBlock
        x:Name="nameText"
        Margin="12,0,0,0"
        RelativePanel.AlignTopWith="thumb"
        RelativePanel.RightOf="thumb"
        Style="{ThemeResource BodyStrongTextBlockStyle}"
        Text="{x:Bind ViewModel.Name}" />

    <TextBlock
        Margin="12,4,0,0"
        RelativePanel.Below="nameText"
        RelativePanel.RightOf="thumb"
        Text="{x:Bind ViewModel.Description}"
        TextTrimming="WordEllipsis" />

    <Button
        Content="View"
        RelativePanel.AlignBottomWithPanel="True"
        RelativePanel.AlignRightWithPanel="True" />
</RelativePanel>
```

## Alignment properties reference

| Attached property | Description |
|---|---|
| `AlignLeftWithPanel` | Left edge = panel left |
| `AlignRightWithPanel` | Right edge = panel right |
| `AlignTopWithPanel` | Top edge = panel top |
| `AlignBottomWithPanel` | Bottom edge = panel bottom |
| `AlignLeftWith="name"` | Left edge = named element's left edge |
| `AlignRightWith="name"` | Right edge = named element's right edge |
| `AlignTopWith="name"` | Top edge = named element's top edge |
| `AlignBottomWith="name"` | Bottom edge = named element's bottom edge |
| `AlignHorizontalCenterWithPanel` | Horizontal center = panel center |
| `AlignVerticalCenterWithPanel` | Vertical center = panel center |
| `AlignHorizontalCenterWith="name"` | Horizontal center = named element center |
| `AlignVerticalCenterWith="name"` | Vertical center = named element center |
| `LeftOf="name"` | Right edge = named element's left edge |
| `RightOf="name"` | Left edge = named element's right edge |
| `Above="name"` | Bottom edge = named element's top edge |
| `Below="name"` | Top edge = named element's bottom edge |

## Notes

- Every sibling used as an anchor must have an `x:Name`.
- `RelativePanel` is particularly useful for `VisualStateManager`-driven adaptive layouts — change attachment points in each visual state rather than toggling visibility.
- Circular dependencies (A relative to B, B relative to A) cause layout errors at runtime.
- For simple linear arrangements, `StackPanel` is simpler. For grid-based layouts, `Grid` is more explicit.
