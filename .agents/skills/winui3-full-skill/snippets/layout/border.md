# Border

`Border` draws a background, border, and/or corner radius around a single child element. It is the standard way to add visual container chrome in WinUI 3.

## Basic Border

```xaml
<Border
    Padding="16"
    Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
    BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="8">
    <TextBlock Text="Card content" />
</Border>
```

## Rounded card

```xaml
<Border
    Padding="20"
    Background="{ThemeResource LayerFillColorDefaultBrush}"
    BorderBrush="{ThemeResource SurfaceStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="{StaticResource OverlayCornerRadius}">
    <StackPanel Spacing="8">
        <TextBlock Style="{ThemeResource SubtitleTextBlockStyle}" Text="Summary" />
        <TextBlock Text="Details here…" />
    </StackPanel>
</Border>
```

## Acrylic background

```xaml
<Border
    Padding="16"
    Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}"
    BorderBrush="{ThemeResource SurfaceStrokeColorDefaultBrush}"
    BorderThickness="1"
    CornerRadius="8">
    <TextBlock Text="Acrylic panel" />
</Border>
```

## Gradient background

```xaml
<Border CornerRadius="12">
    <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="1,1">
            <GradientStop Offset="0" Color="#FF0078D4" />
            <GradientStop Offset="1" Color="#FF50C2FF" />
        </LinearGradientBrush>
    </Border.Background>
    <TextBlock
        Margin="16"
        Foreground="White"
        Text="Gradient card" />
</Border>
```

## Image clipped to rounded corners

```xaml
<Border
    Width="120"
    Height="120"
    CornerRadius="60">
    <Image Source="ms-appx:///Assets/avatar.png" Stretch="UniformToFill" />
</Border>
```

## Notes

- `Border` accepts exactly **one** child; use `Grid` or `StackPanel` as child when you need multiple elements.
- `CornerRadius` takes up to four values (TopLeft, TopRight, BottomRight, BottomLeft): `"8,8,0,0"`.
- Use `ThemeResource` brushes (`CardBackgroundFillColorDefaultBrush`, `LayerFillColorDefaultBrush`, etc.) rather than hardcoded colours to support light/dark themes automatically.
- `BorderThickness` can be non-uniform: `"1,0,1,0"` for left/right borders only.
- `Padding` adds space between the border edge and the child content.
- `ClipToBounds` is not needed — `Border` clips its child to `CornerRadius` automatically.
