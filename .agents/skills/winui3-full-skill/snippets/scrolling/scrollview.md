# ScrollView

The modern successor to `ScrollViewer` (Windows App SDK 1.0+). Provides physics-based inertia, constant-velocity scrolling, programmatic scroll with custom animations, and a `ScrollPresenter`-based architecture. Prefer `ScrollView` for new code.

---

## Basic Usage

```xaml
<ScrollView Height="266" Width="400" ZoomMode="Enabled" IsTabStop="True">
    <Image
        Source="ms-appx:///Assets/SampleMedia/cliff.jpg"
        Stretch="Uniform"
        HorizontalAlignment="Center"
        VerticalAlignment="Center"
        AutomationProperties.Name="Landscape photo" />
</ScrollView>
```

---

## Full Options Example

```xaml
<ScrollView
    x:Name="scrollView1"
    Width="400"
    Height="266"
    HorizontalAlignment="Left"
    VerticalAlignment="Top"
    ContentOrientation="None"
    IsTabStop="True"
    ZoomMode="Enabled"
    HorizontalScrollMode="Auto"
    HorizontalScrollBarVisibility="Auto"
    VerticalScrollMode="Auto"
    VerticalScrollBarVisibility="Auto">
    <Image
        Source="ms-appx:///Assets/SampleMedia/cliff.jpg"
        Stretch="Uniform"
        HorizontalAlignment="Center"
        VerticalAlignment="Center"
        AutomationProperties.Name="Landscape photo" />
</ScrollView>
```

---

## Constant-Velocity Scrolling

```csharp
// Scroll at constant velocity (value > 30 = down, < -30 = up)
scrollView.AddScrollVelocity(
    offsetVelocity: new System.Numerics.Vector2(0, 150f),
    inertiaDecayRate: null); // null = use default inertia
```

---

## Programmatic Scroll with Custom Animation

```xaml
<ScrollView
    x:Name="scrollView3"
    Width="400"
    Height="300"
    IsTabStop="True"
    ScrollAnimationStarting="ScrollView_ScrollAnimationStarting">
    <StackPanel>
        <!-- content items -->
    </StackPanel>
</ScrollView>
```

```csharp
// Views/ScrollPage.xaml.cs
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Hosting;

private void ScrollView_ScrollAnimationStarting(ScrollView sender,
    ScrollingScrollAnimationStartingEventArgs args)
{
    // Replace default animation with a custom spring/bounce effect
    var compositor = ElementCompositionPreview.GetElementVisual(sender).Compositor;
    var springAnimation = compositor.CreateSpringScalarAnimation();
    springAnimation.InitialVelocity = 200f;
    springAnimation.Period = TimeSpan.FromMilliseconds(50);
    springAnimation.DampingRatio = 0.5f;

    args.Animation = springAnimation; // override the built-in animation
}

private void BtnScrollWithAnimation_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
{
    scrollView3.ScrollTo(0, 500); // scroll to offset 500 vertically
}
```

---

## Binding List with ScrollView + ItemsRepeater

```xaml
<ScrollView
    Height="400"
    HorizontalScrollBarVisibility="Hidden"
    VerticalScrollBarVisibility="Auto">
    <ItemsRepeater ItemsSource="{x:Bind ViewModel.Items}">
        <ItemsRepeater.Layout>
            <StackLayout Spacing="8" />
        </ItemsRepeater.Layout>
        <ItemsRepeater.ItemTemplate>
            <DataTemplate x:DataType="local:MyItem">
                <TextBlock Text="{x:Bind Name}" Margin="12,4" />
            </DataTemplate>
        </ItemsRepeater.ItemTemplate>
    </ItemsRepeater>
</ScrollView>
```

---

## Variants

| Property | Values | Description |
|---|---|---|
| `ZoomMode` | `Enabled`, `Disabled` | Allow pinch-to-zoom |
| `HorizontalScrollMode` | `Enabled`, `Disabled`, `Auto` | Enable horizontal panning |
| `VerticalScrollMode` | `Enabled`, `Disabled`, `Auto` | Enable vertical panning |
| `ContentOrientation` | `None`, `Vertical`, `Horizontal`, `Both` | Constrains content layout direction |
| `HorizontalScrollBarVisibility` | `Auto`, `Visible`, `Hidden` | Scroll bar appearance |
| `VerticalScrollBarVisibility` | `Auto`, `Visible`, `Hidden` | Scroll bar appearance |

---

## Notes

- `ScrollView` exposes a `ScrollPresenter` property — use `scrollView.ScrollPresenter.VerticalScrollController` to connect an `AnnotatedScrollBar`.
- `AddScrollVelocity` requires a velocity > 30 (or < -30) to overcome static friction.
- `ScrollAnimationStarting` fires before each programmatic scroll; assign `args.Animation` to override the built-in animation.
- `ScrollTo(x, y)` / `ScrollBy(dx, dy)` are the primary programmatic scroll methods.
- Unlike `ScrollViewer`, `ScrollView` does **not** have a `ZoomFactor` property; use `ZoomTo(factor)` instead.
