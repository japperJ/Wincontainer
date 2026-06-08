# IconElement

WinUI 3 provides several icon types all derived from `IconElement`. Use them in `Button.Content`, `NavigationViewItem.Icon`, `AppBarButton.Icon`, `MenuFlyoutItem.Icon`, and many other controls that accept an icon.

---

## Icon Types at a Glance

| Type | Source | Example |
|---|---|---|
| `SymbolIcon` | `Symbol` enum (Segoe MDL2 Assets) | `<SymbolIcon Symbol="Home" />` |
| `FontIcon` | Unicode glyph + font family | `<FontIcon Glyph="&#xE700;" />` |
| `BitmapIcon` | PNG / JPEG URI | `<BitmapIcon UriSource="..." />` |
| `PathIcon` | Geometry path data | `<PathIcon Data="M0,0 L24,0 L12,24 Z" />` |
| `ImageIcon` | Any `ImageSource` | `<ImageIcon Source="ms-appx:///..." />` |

---

## SymbolIcon

Uses the `Symbol` enum (subset of Segoe MDL2 Assets):

```xaml
<Button AutomationProperties.Name="Refresh">
    <SymbolIcon Symbol="Refresh" />
</Button>

<!-- Common symbols -->
<SymbolIcon Symbol="Home" />
<SymbolIcon Symbol="Back" />
<SymbolIcon Symbol="Forward" />
<SymbolIcon Symbol="Find" />
<SymbolIcon Symbol="Settings" />
<SymbolIcon Symbol="Add" />
<SymbolIcon Symbol="Delete" />
<SymbolIcon Symbol="Edit" />
<SymbolIcon Symbol="Save" />
```

---

## FontIcon

Use any Unicode glyph from Segoe MDL2 Assets or Segoe Fluent Icons:

```xaml
<!-- Segoe MDL2 Assets (default FontFamily) -->
<FontIcon Glyph="&#xE700;" />   <!-- Hamburger menu -->
<FontIcon Glyph="&#xE713;" />   <!-- Settings gear -->
<FontIcon Glyph="&#xE8B5;" />   <!-- Save -->

<!-- Custom font family -->
<FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xEB51;" />
```

---

## BitmapIcon

```xaml
<BitmapIcon
    UriSource="ms-appx:///Assets/Icons/custom-icon.png"
    ShowAsMonochrome="False" />
```

Set `ShowAsMonochrome="False"` to preserve the original colours; `True` (default) renders it using the foreground colour.

---

## PathIcon

Vector path — scales without pixelation at any size:

```xaml
<!-- Triangle -->
<PathIcon Data="M 0,16 L 8,0 L 16,16 Z" />

<!-- Checkmark -->
<PathIcon Data="M 0,8 L 5,14 L 14,2" />
```

---

## ImageIcon

Supports any `ImageSource` including SVG:

```xaml
<ImageIcon Source="ms-appx:///Assets/Icons/logo.svg" />
```

---

## Using Icons in Common Controls

```xaml
<!-- NavigationViewItem -->
<NavigationViewItem Content="Home">
    <NavigationViewItem.Icon>
        <SymbolIcon Symbol="Home" />
    </NavigationViewItem.Icon>
</NavigationViewItem>

<!-- AppBarButton -->
<AppBarButton Icon="Save" Label="Save" AutomationProperties.Name="Save file" />

<!-- MenuFlyoutItem -->
<MenuFlyoutItem Text="Copy">
    <MenuFlyoutItem.Icon>
        <SymbolIcon Symbol="Copy" />
    </MenuFlyoutItem.Icon>
</MenuFlyoutItem>

<!-- Button with FontIcon -->
<Button AutomationProperties.Name="Add new item">
    <FontIcon Glyph="&#xE710;" />
</Button>
```

---

## IconSource vs IconElement

Controls that accept an `IconSource` property (e.g. `InfoBadge`, `TabViewItem`) require `SymbolIconSource`, `FontIconSource`, `BitmapIconSource`, or `PathIconSource` — not `IconElement` subtypes.

```xaml
<!-- InfoBadge uses IconSource -->
<InfoBadge>
    <InfoBadge.IconSource>
        <SymbolIconSource Symbol="Important" />
    </InfoBadge.IconSource>
</InfoBadge>
```

---

## Notes

- Always set `AutomationProperties.Name` on the parent interactive control when only an icon is displayed — screen readers cannot infer meaning from a glyph.
- `SymbolIcon` is the simplest choice for common actions; use `FontIcon` when you need a glyph not in the `Symbol` enum.
- `PathIcon` is the most scalable and theme-aware option for custom icons.
- `FontIcon.FontFamily` defaults to `"Segoe MDL2 Assets"`. For Windows 11 target, `"Segoe Fluent Icons"` offers a more modern look.
- `BitmapIcon` with `ShowAsMonochrome="True"` respects `Foreground` and adapts to dark/light themes; set `False` only for full-colour imagery.
