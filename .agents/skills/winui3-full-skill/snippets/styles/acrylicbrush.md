# AcrylicBrush

`AcrylicBrush` produces a translucent, frosted-glass effect that blurs the content behind the painted region. Two flavours exist: **in-app acrylic** (blurs XAML content within the same window) and **background/desktop acrylic** (handled via `DesktopAcrylicBackdrop` — see `snippets/styles/systembackdrops.md`).

---

## Using the Built-in Theme Resource

```xaml
<!-- Recommended: use the system-provided acrylic brush resource -->
<Rectangle Fill="{ThemeResource AcrylicInAppFillColorDefaultBrush}" />
```

---

## Custom In-App AcrylicBrush in a ResourceDictionary

```xaml
<Page.Resources>
    <ResourceDictionary>
        <ResourceDictionary.ThemeDictionaries>
            <ResourceDictionary x:Key="Default">
                <media:AcrylicBrush
                    x:Key="CustomAcrylicBrush"
                    TintColor="Black"
                    TintOpacity="0.8"
                    FallbackColor="Green" />
            </ResourceDictionary>
            <ResourceDictionary x:Key="Light">
                <media:AcrylicBrush
                    x:Key="CustomAcrylicBrush"
                    TintColor="White"
                    TintOpacity="0.6"
                    FallbackColor="WhiteSmoke" />
            </ResourceDictionary>
        </ResourceDictionary.ThemeDictionaries>
    </ResourceDictionary>
</Page.Resources>

<!-- Usage -->
<Border
    Width="300"
    Height="200"
    Background="{ThemeResource CustomAcrylicBrush}"
    CornerRadius="8" />
```

> **Namespace required:**
> ```xaml
> xmlns:media="using:Microsoft.UI.Xaml.Media"
> ```

---

## Luminosity Variant

```xaml
<Page.Resources>
    <ResourceDictionary.ThemeDictionaries>
        <ResourceDictionary x:Key="Default">
            <media:AcrylicBrush
                x:Key="CustomAcrylicLuminosity"
                TintColor="SkyBlue"
                TintOpacity="0.5"
                TintLuminosityOpacity="0.85"
                FallbackColor="SkyBlue" />
        </ResourceDictionary>
    </ResourceDictionary.ThemeDictionaries>
</Page.Resources>

<Rectangle Fill="{ThemeResource CustomAcrylicLuminosity}" />
```

---

## Applying to a Panel Background

```xaml
<!-- Side panel with acrylic background -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition />
    </Grid.RowDefinitions>
    <Grid.Background>
        <media:AcrylicBrush
            TintColor="{ThemeResource SystemChromeMediumColor}"
            TintOpacity="0.7"
            FallbackColor="{ThemeResource SystemChromeMediumColor}" />
    </Grid.Background>
    <!-- Content -->
</Grid>
```

---

## Modifying Properties at Runtime

```csharp
// Views/SidePanelPage.xaml.cs
private void UpdateAcrylicTint(double opacity)
{
    if (MyBorder.Background is Microsoft.UI.Xaml.Media.AcrylicBrush acrylic)
    {
        acrylic.TintOpacity = opacity;
    }
}
```

---

## Variants / Properties

| Property | Type | Description |
|---|---|---|
| `TintColor` | `Color` | Colour tint overlaid on the blur |
| `TintOpacity` | `double` 0–1 | How much the tint covers the blur |
| `TintLuminosityOpacity` | `double?` 0–1 | Controls the luminosity layer (null = auto) |
| `FallbackColor` | `Color` | Solid colour used when acrylic is unavailable |

---

## Notes

- AcrylicBrush may fall back to `FallbackColor` when: battery saver is on, high-contrast mode is active, or the window is not in focus.
- **Always define** `FallbackColor` to match your design — the fallback is visible on older hardware and accessibility modes.
- Define brushes inside `ResourceDictionary.ThemeDictionaries` (`Default`/`Light`) so they update correctly with theme changes.
- For **window-level** acrylic (blurs the desktop behind the app), use `DesktopAcrylicBackdrop` — see `snippets/styles/systembackdrops.md`.
- In-app acrylic only blurs XAML content **below** the brush in z-order within the same window.
