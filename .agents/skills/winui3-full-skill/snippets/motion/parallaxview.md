# ParallaxView

`ParallaxView` shifts its content at a slower rate than a linked `ScrollViewer` or `ScrollView`, creating a depth/parallax effect. Typically used for a decorative background image behind a scrollable list.

---

## Parallax on a ListView

```xaml
<Grid>
    <!-- Background parallax layer -->
    <ParallaxView
        HorizontalAlignment="Left"
        VerticalAlignment="Top"
        Source="{Binding ElementName=listView}"
        VerticalShift="500">
        <Image
            Source="ms-appx:///Assets/SampleMedia/cliff.jpg"
            AutomationProperties.Name="Background landscape photo" />
    </ParallaxView>

    <!-- Foreground scrollable list (the scroll source) -->
    <ListView
        x:Name="listView"
        HorizontalAlignment="Stretch"
        VerticalAlignment="Top"
        Background="#80000000"
        ItemsSource="{x:Bind ViewModel.Items}">
        <ListView.Header>
            <TextBlock
                HorizontalAlignment="Center"
                FontSize="28"
                Foreground="White"
                Text="Scroll to see the parallax effect"
                TextWrapping="WrapWholeWords" />
        </ListView.Header>
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="local:MyItem">
                <TextBlock
                    Foreground="{ThemeResource SystemControlForegroundAltHighBrush}"
                    Text="{x:Bind Title}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Grid>
```

---

## Parallax Behind a ScrollView

```xaml
<Grid>
    <!-- Background parallax image -->
    <ParallaxView
        HorizontalAlignment="Left"
        VerticalAlignment="Top"
        Source="{Binding ElementName=contentScrollView}"
        VerticalShift="400">
        <Image Source="ms-appx:///Assets/hero-bg.jpg" Stretch="UniformToFill" />
    </ParallaxView>

    <!-- Scrollable foreground content -->
    <ScrollView
        x:Name="contentScrollView"
        HorizontalAlignment="Stretch">
        <StackPanel Padding="24" Spacing="16">
            <TextBlock
                FontSize="32"
                Foreground="White"
                Text="{x:Bind ViewModel.HeroTitle}" />
            <!-- More content below the fold -->
        </StackPanel>
    </ScrollView>
</Grid>
```

---

## Horizontal Parallax

```xaml
<Grid>
    <ParallaxView
        Source="{Binding ElementName=hScrollView}"
        HorizontalShift="300">
        <Image Source="ms-appx:///Assets/panorama.jpg" Stretch="UniformToFill" />
    </ParallaxView>

    <ScrollView
        x:Name="hScrollView"
        HorizontalScrollMode="Enabled"
        VerticalScrollMode="Disabled">
        <StackPanel Orientation="Horizontal" Spacing="8">
            <!-- Horizontally scrolled content -->
        </StackPanel>
    </ScrollView>
</Grid>
```

---

## Controlling Parallax Speed

The shift ratio is `shift / sourceScrollRange`:

- `VerticalShift="500"` with a list that scrolls 1000px means the background moves at **50% the speed** of the list.
- Smaller shift = slower parallax (deeper perceived depth).
- Larger shift = faster parallax (shallower depth; can feel jittery if too large).

```xaml
<!-- Subtle depth: background moves at ~30% scroll speed -->
<ParallaxView
    Source="{Binding ElementName=listView}"
    VerticalShift="300">
    <Image Source="ms-appx:///Assets/bg.jpg" />
</ParallaxView>
```

---

## Notes

- `Source` binds to the **scroll source** element — a `ListView`, `GridView`, `ScrollViewer`, or `ScrollView`.
- The `ParallaxView.Source` binding uses `{Binding ElementName=...}` (reflection binding), not `{x:Bind}` — this is one of the few acceptable uses of `{Binding}` in WinUI 3.
- `ParallaxView` content is typically an `Image`; any XAML element works but large images are the most common use case.
- Set the image's `Stretch` to `UniformToFill` or `Fill` to avoid gaps at the edges during parallax scrolling.
- `ParallaxView` renders in the **Composition layer** — no UI-thread overhead during scrolling.
- `HorizontalShift` and `VerticalShift` can be set simultaneously for diagonal parallax.
