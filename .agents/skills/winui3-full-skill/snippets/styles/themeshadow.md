# ThemeShadow

`ThemeShadow` adds a Composition-layer drop shadow to any XAML element. The shadow is aware of the element's Z-translation and automatically recalculates depth, making it far simpler than custom `DropShadow` Composition code.

---

## Basic Usage

```xaml
<Grid>
    <!-- Shadow receiver — must be behind the casting element -->
    <Grid x:Name="ShadowCastGrid" />

    <!-- Shadow caster -->
    <Border
        x:Name="ShadowRect"
        Width="200"
        Height="200"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="{ThemeResource OverlayCornerRadius}"
        Loaded="ShadowRect_Loaded">
        <Border.Shadow>
            <ThemeShadow x:Name="shadow" />
        </Border.Shadow>
    </Border>
</Grid>
```

```csharp
// Views/ShadowPage.xaml.cs
private void ShadowRect_Loaded(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // Register the Grid as a shadow receiver
    shadow.Receivers.Add(ShadowCastGrid);

    // Lift the caster off the receiver with a Z-translation
    ShadowRect.Translation = new System.Numerics.Vector3(0, 0, 32);
}
```

---

## Adjustable Z-Translation

The shadow depth is controlled by the caster's `Translation.Z` value:

```csharp
private void TranslationSlider_ValueChanged(object sender,
    Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
{
    ShadowRect.Translation = new System.Numerics.Vector3(0, 0, (float)e.NewValue);
}
```

```xaml
<Slider
    Header="Z-translation (shadow depth)"
    Minimum="0"
    Maximum="64"
    Value="32"
    ValueChanged="TranslationSlider_ValueChanged" />
```

---

## Multiple Receivers

```csharp
// Register multiple grids / images as receivers
shadow.Receivers.Add(BackgroundImage);
shadow.Receivers.Add(BackgroundGrid);
```

---

## Card with ThemeShadow (MVVM)

```xaml
<Grid>
    <Grid x:Name="CardReceiver" />

    <Border
        x:Name="Card"
        Padding="16"
        Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
        CornerRadius="8"
        Loaded="Card_Loaded">
        <Border.Shadow>
            <ThemeShadow x:Name="CardShadow" />
        </Border.Shadow>
        <StackPanel Spacing="8">
            <TextBlock Style="{StaticResource SubtitleTextBlockStyle}"
                       Text="{x:Bind ViewModel.Title}" />
            <TextBlock Text="{x:Bind ViewModel.Description}" TextWrapping="Wrap" />
        </StackPanel>
    </Border>
</Grid>
```

```csharp
private void Card_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
{
    CardShadow.Receivers.Add(CardReceiver);
    Card.Translation = new System.Numerics.Vector3(0, 0, 32);
}
```

---

## Notes

- `ThemeShadow` requires a **receiver** element in `shadow.Receivers` — without a receiver, no shadow is cast.
- The receiver must be in the **same visual tree** and positioned behind (lower Z-order than) the shadow caster.
- Use `Translation` (a `Vector3` property on `UIElement`) to lift the caster. The shadow grows with `Translation.Z`.
- `ThemeShadow` respects the system theme (dark/light), adjusting shadow colour automatically.
- `ThemeShadow` is a Composition-layer effect — it renders at hardware-accelerated speed.
- Only `UIElement`-derived types can cast `ThemeShadow`; shapes like `Path` or `Ellipse` work fine.
