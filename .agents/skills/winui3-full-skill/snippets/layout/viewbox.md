# Viewbox

`Viewbox` scales and stretches a single child element to fill its available space, preserving or ignoring the child's aspect ratio depending on `Stretch`. Use it to make fixed-size content (icons, vector graphics, labels) scale to any container size.

## Basic Viewbox

```xaml
<Viewbox Width="200" Height="100">
    <!-- Original size: 24x24 — will be scaled up uniformly -->
    <SymbolIcon Symbol="Home" />
</Viewbox>
```

## Stretch modes

```xaml
<!-- Uniform (default): maintains aspect ratio, fits within bounds -->
<Viewbox Stretch="Uniform">
    <TextBlock FontSize="48" Text="Hello" />
</Viewbox>

<!-- UniformToFill: maintains aspect ratio, fills bounds (may clip) -->
<Viewbox Stretch="UniformToFill">
    <TextBlock FontSize="48" Text="Hello" />
</Viewbox>

<!-- Fill: stretches to fill, ignores aspect ratio -->
<Viewbox Stretch="Fill">
    <TextBlock FontSize="48" Text="Hello" />
</Viewbox>

<!-- None: no scaling applied -->
<Viewbox Stretch="None">
    <TextBlock FontSize="48" Text="Hello" />
</Viewbox>
```

## Scalable vector badge

```xaml
<Viewbox Width="64" Height="64">
    <Grid Width="24" Height="24">
        <Ellipse Fill="{ThemeResource AccentFillColorDefaultBrush}" />
        <TextBlock
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            FontSize="12"
            FontWeight="Bold"
            Foreground="White"
            Text="7" />
    </Grid>
</Viewbox>
```

## Responsive label using Viewbox

```xaml
<!-- The TextBlock will scale to fill whatever width is available -->
<Viewbox
    HorizontalAlignment="Stretch"
    StretchDirection="DownOnly">
    <TextBlock
        FontSize="72"
        FontWeight="Bold"
        Text="Welcome" />
</Viewbox>
```

## AppBarButton with scaled icon

```xaml
<AppBarButton Label="Custom Icon">
    <AppBarButton.Content>
        <Viewbox Stretch="Uniform" Width="20" Height="20">
            <PathIcon Data="M 0,12 L 12,0 L 24,12 L 12,24 Z" />
        </Viewbox>
    </AppBarButton.Content>
</AppBarButton>
```

## Notes

- `Viewbox` accepts exactly **one** child; wrap multiple elements in a `Grid` or `StackPanel`.
- `StretchDirection` can be `Both` (default), `UpOnly`, or `DownOnly` — `DownOnly` prevents scaling larger than original size.
- `Stretch="Uniform"` is the most commonly appropriate value: it scales up/down while preserving aspect ratio.
- Avoid using `Viewbox` for text-heavy layouts — text may become blurry at non-integer scale factors. Prefer `Viewbox` for vector/icon content.
- Nesting `Viewbox` inside a `ScrollViewer` or unconstrained panel may cause layout issues because the outer container may report infinite available size.
