# Implicit Transitions

Implicit transitions automatically animate changes to `Opacity`, `Rotation`, `Scale`, `Translation`, and `Background` (brush) whenever those properties change — no storyboard or code required. Simply attach the corresponding `*Transition` property in XAML.

---

## Opacity Transition

```xaml
<Rectangle
    x:Name="MyRect"
    Width="50"
    Height="50"
    Fill="{ThemeResource SystemAccentColor}"
    Opacity="0.5">
    <Rectangle.OpacityTransition>
        <ScalarTransition />
    </Rectangle.OpacityTransition>
</Rectangle>
```

```csharp
// Changing Opacity now animates automatically
private void FadeButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    MyRect.Opacity = MyRect.Opacity < 1 ? 1.0 : 0.1;
}
```

---

## Rotation Transition

```xaml
<Rectangle
    x:Name="RotateRect"
    Width="50"
    Height="50"
    Fill="{ThemeResource SystemAccentColor}">
    <Rectangle.RotationTransition>
        <ScalarTransition />
    </Rectangle.RotationTransition>
</Rectangle>
```

```csharp
// Clockwise rotation in degrees
RotateRect.Rotation = 90;
```

---

## Scale Transition

```xaml
<Rectangle
    x:Name="ScaleRect"
    Width="50"
    Height="50"
    Fill="{ThemeResource SystemAccentColor}">
    <Rectangle.ScaleTransition>
        <Vector3Transition />
    </Rectangle.ScaleTransition>
</Rectangle>
```

```csharp
using System.Numerics;

// Scale to 2x
ScaleRect.Scale = new Vector3(2f, 2f, 1f);
```

---

## Translation Transition

```xaml
<Rectangle
    x:Name="MoveRect"
    Width="50"
    Height="50"
    Fill="{ThemeResource SystemAccentColor}">
    <Rectangle.TranslationTransition>
        <Vector3Transition />
    </Rectangle.TranslationTransition>
</Rectangle>
```

```csharp
// Move 100px right and down
MoveRect.Translation = new Vector3(100, 100, 0);
```

---

## Background (Brush) Transition

```xaml
<ContentPresenter
    x:Name="BrushPresenter"
    Width="100"
    Height="100"
    Background="Blue">
    <ContentPresenter.BackgroundTransition>
        <BrushTransition />
    </ContentPresenter.BackgroundTransition>
</ContentPresenter>
```

```csharp
// Changing Background now cross-fades
private bool _isBlue = true;

private void ToggleColorButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // Must assign a NEW brush instance (not mutate the existing one)
    _isBlue = !_isBlue;
    BrushPresenter.Background = _isBlue
        ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Blue)
        : new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Yellow);
}
```

---

## Theme Change Transition (Grid.BackgroundTransition)

```xaml
<Grid
    x:Name="ThemeGrid"
    Background="{ThemeResource SolidBackgroundFillColorBaseBrush}"
    RequestedTheme="Light">
    <Grid.BackgroundTransition>
        <BrushTransition />
    </Grid.BackgroundTransition>
    <!-- Content that animates when theme toggles -->
</Grid>
```

```csharp
private void ToggleThemeButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    ThemeGrid.RequestedTheme = ThemeGrid.RequestedTheme == ElementTheme.Dark
        ? ElementTheme.Light
        : ElementTheme.Dark;
}
```

---

## Custom Duration

```xaml
<Rectangle.OpacityTransition>
    <ScalarTransition Duration="0:0:0.5" />
</Rectangle.OpacityTransition>
```

---

## Available Transition Types

| Transition | Property | Type |
|---|---|---|
| `ScalarTransition` | `Opacity`, `Rotation` | Single float value |
| `Vector3Transition` | `Scale`, `Translation` | `System.Numerics.Vector3` |
| `BrushTransition` | `Background`, `BorderBrush` | `Brush` property |

---

## Notes

- `BrushTransition` requires assigning a **new brush instance** — mutating an existing `SolidColorBrush.Color` will not trigger the animation.
- `Rotation`, `Scale`, and `Translation` are **Composition-layer** properties (`UIElement.Rotation` etc.) — distinct from `RenderTransform`. Use these, not `RotateTransform`.
- `Vector3Transition` animates all three axes; use the `Components` property to animate only specific axes (X, Y, Z).
- Implicit transitions are disabled when `Windows.UI.Settings.UISettings.AnimationsEnabled` is `false` (user setting) — always ensure your UI is functional without animations.
- Default duration is 45ms for `ScalarTransition` / `Vector3Transition` and 167ms for `BrushTransition`; set `Duration` explicitly for custom timing.
