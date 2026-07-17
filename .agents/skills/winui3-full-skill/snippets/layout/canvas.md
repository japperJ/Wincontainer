# Canvas

`Canvas` is an absolute-positioning panel. Each child uses `Canvas.Left` and `Canvas.Top` attached properties to specify its position. Use it for drawing surfaces, custom controls, and animation scenarios — not for general UI layout.

## Basic Canvas

```xaml
<Canvas Width="400" Height="300" Background="{ThemeResource LayerFillColorDefaultBrush}">
    <Rectangle
        Canvas.Left="20"
        Canvas.Top="20"
        Width="100"
        Height="60"
        Fill="{ThemeResource AccentFillColorDefaultBrush}" />

    <Ellipse
        Canvas.Left="160"
        Canvas.Top="40"
        Width="80"
        Height="80"
        Fill="{ThemeResource SystemFillColorSuccessBrush}" />

    <TextBlock
        Canvas.Left="30"
        Canvas.Top="120"
        FontSize="18"
        Text="Absolute positioning" />
</Canvas>
```

## Canvas with z-order (Canvas.ZIndex)

```xaml
<Canvas Width="300" Height="200">
    <!-- Drawn first (bottom layer) -->
    <Rectangle
        Canvas.Left="0"
        Canvas.Top="0"
        Canvas.ZIndex="0"
        Width="200"
        Height="150"
        Fill="LightBlue" />

    <!-- Drawn on top -->
    <Rectangle
        Canvas.Left="80"
        Canvas.Top="50"
        Canvas.ZIndex="1"
        Width="160"
        Height="100"
        Fill="Orange"
        Opacity="0.8" />
</Canvas>
```

## Canvas for custom drawing with Path

```xaml
<Canvas Width="300" Height="200">
    <Path
        Canvas.Left="10"
        Canvas.Top="10"
        Data="M 0,100 C 50,0 100,200 150,100 S 250,0 300,100"
        Stroke="{ThemeResource AccentFillColorDefaultBrush}"
        StrokeThickness="2" />

    <Path
        Canvas.Left="20"
        Canvas.Top="50"
        Data="M 0,0 L 100,0 L 50,86 Z"
        Fill="{ThemeResource SystemFillColorCautionBrush}"
        Stroke="Transparent" />
</Canvas>
```

## Animating a child on Canvas

```csharp
// Using DispatcherQueue for UI thread safety
private async Task AnimateAsync()
{
    for (double i = 0; i <= 200; i += 2)
    {
        Canvas.SetLeft(myElement, i);
        await Task.Delay(16); // ~60 fps
    }
}
```

## Notes

- `Canvas` does **not** size itself to its children by default — set explicit `Width`/`Height` or it may collapse to zero.
- `Canvas.ZIndex` controls paint order; higher values render on top. Default is `0`.
- For general UI layout, prefer `Grid` or `StackPanel`. Canvas is best for drawing surfaces, games, and animation.
- `Canvas` does not clip children; a child positioned outside the canvas bounds is still visible unless `ClipToBounds="True"`.
- Use `Storyboard` / `DoubleAnimation` targeting `Canvas.Left`/`Canvas.Top` for XAML-driven animations.
