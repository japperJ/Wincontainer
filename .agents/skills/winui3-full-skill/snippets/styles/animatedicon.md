# AnimatedIcon

`AnimatedIcon` wraps a Lottie `IAnimatedVisualSource` inside an icon-sized element and drives the animation progress automatically in response to **control visual states** (e.g. PointerOver, Pressed, Selected). Ideal for interactive icon buttons and `NavigationViewItem` icons.

---

## Basic Usage — In a Button

```xaml
<Button
    Width="75"
    PointerEntered="Button_PointerEntered"
    PointerExited="Button_PointerExited"
    AutomationProperties.Name="Search">
    <AnimatedIcon x:Name="SearchAnimatedIcon">
        <AnimatedIcon.Source>
            <animatedvisuals:AnimatedFindVisualSource />
        </AnimatedIcon.Source>
        <AnimatedIcon.FallbackIconSource>
            <SymbolIconSource Symbol="Find" />
        </AnimatedIcon.FallbackIconSource>
    </AnimatedIcon>
</Button>
```

> **Namespaces required:**
> ```xaml
> xmlns:animatedvisuals="using:Microsoft.UI.Xaml.Controls.AnimatedVisuals"
> ```

```csharp
// Views/SearchPage.xaml.cs
private void Button_PointerEntered(object sender,
    Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
{
    AnimatedIcon.SetState(SearchAnimatedIcon, "PointerOver");
}

private void Button_PointerExited(object sender,
    Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
{
    AnimatedIcon.SetState(SearchAnimatedIcon, "Normal");
}
```

---

## In a NavigationViewItem (Automatic State Driving)

When `AnimatedIcon` is placed in `NavigationViewItem.Icon`, the `NavigationView` drives the state automatically — no code-behind required.

```xaml
<NavigationView IsSettingsVisible="False">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Settings">
            <NavigationViewItem.Icon>
                <AnimatedIcon>
                    <AnimatedIcon.Source>
                        <animatedvisuals:AnimatedSettingsVisualSource />
                    </AnimatedIcon.Source>
                    <AnimatedIcon.FallbackIconSource>
                        <FontIconSource Glyph="&#xE713;" />
                    </AnimatedIcon.FallbackIconSource>
                </AnimatedIcon>
            </NavigationViewItem.Icon>
        </NavigationViewItem>
    </NavigationView.MenuItems>
</NavigationView>
```

---

## Built-in Animation Sources

All are in `Microsoft.UI.Xaml.Controls.AnimatedVisuals`:

| Class | Description |
|---|---|
| `AnimatedBackVisualSource` | Back-arrow animate |
| `AnimatedChevronDownSmallVisualSource` | Small down chevron |
| `AnimatedChevronRightDownSmallVisualSource` | Chevron right→down |
| `AnimatedChevronUpDownSmallVisualSource` | Chevron up↔down |
| `AnimatedFindVisualSource` | Search/find magnifier |
| `AnimatedGlobalNavigationButtonVisualSource` | Hamburger menu |
| `AnimatedSettingsVisualSource` | Settings gear |

---

## Standard Visual States

| State string | When to apply |
|---|---|
| `"Normal"` | Default / idle |
| `"PointerOver"` | Mouse hover |
| `"Pressed"` | Mouse/touch press |
| `"Disabled"` | Control is disabled |
| `"Selected"` | Item is selected (e.g. NavigationViewItem) |
| `"PointerOverSelected"` | Hover on selected item |

---

## Custom Lottie Animation Source

1. Run `lottie` tool to generate a C# class (see `animatedvisualplayer.md`).
2. Implement `IAnimatedVisualSource2` on the generated class (the tool does this automatically).
3. Reference in XAML:
```xaml
<AnimatedIcon>
    <AnimatedIcon.Source>
        <myAnimations:MyCustomIcon />
    </AnimatedIcon.Source>
    <AnimatedIcon.FallbackIconSource>
        <FontIconSource Glyph="&#xE700;" />
    </AnimatedIcon.FallbackIconSource>
</AnimatedIcon>
```

---

## Notes

- `FallbackIconSource` is required — shown when the `IAnimatedVisualSource2` isn't available on the current OS build.
- `AnimatedIcon.SetState(element, stateName)` is the primary API; the icon interprets state transitions as animation segments.
- The animation file must define **named markers** matching the visual state names; without matching markers the icon stays still.
- `AnimatedIcon` is designed for **icon sizes** (16–48 px); for large Lottie playback use `AnimatedVisualPlayer`.
- Controls that natively understand `AnimatedIcon`: `NavigationViewItem`, `TreeViewItem`, `Expander`, `CheckBox`, `RadioButton`, `ToggleSwitch`, `TabViewItem`.
