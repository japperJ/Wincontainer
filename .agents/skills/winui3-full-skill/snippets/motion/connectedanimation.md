# ConnectedAnimation

`ConnectedAnimation` creates a smooth visual transition where an element appears to "fly" from its position on one page to its position on the next. Particularly effective in list-to-detail navigation scenarios.

---

## List-to-Detail Navigation — List Page

```csharp
// Views/ItemsPage.xaml.cs
private void ItemsList_ItemClick(object sender,
    ItemClickEventArgs e)
{
    var item = (MyItem)e.ClickedItem;

    // Prepare the animation BEFORE navigating
    var animationService = ConnectedAnimationService.GetForCurrentView();
    animationService.PrepareToAnimate("DetailConnectedAnimation",
        (UIElement)ItemsList.ContainerFromItem(item));

    Frame.Navigate(typeof(DetailPage), item);
}
```

---

## Detail Page — Receiving the Animation

```xaml
<!-- Views/DetailPage.xaml -->
<Image
    x:Name="HeroImage"
    Width="400"
    Height="300"
    Source="{x:Bind ViewModel.ImageUri}"
    Stretch="UniformToFill" />
```

```csharp
// Views/DetailPage.xaml.cs
protected override void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);
    ViewModel.Item = (MyItem)e.Parameter;
}

private void Page_Loaded(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var animationService = ConnectedAnimationService.GetForCurrentView();
    var animation = animationService.GetAnimation("DetailConnectedAnimation");

    if (animation is not null)
    {
        // Apply GravityConnectedAnimationConfiguration for a more natural feel
        animation.Configuration = new GravityConnectedAnimationConfiguration();
        animation.TryStart(HeroImage);
    }
}
```

---

## Returning Back from Detail to List

```csharp
// On DetailPage — prepare back animation BEFORE navigating back
private void BackButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    ConnectedAnimationService.GetForCurrentView()
        .PrepareToAnimate("BackConnectedAnimation", HeroImage);

    Frame.GoBack();
}
```

```csharp
// On ItemsPage — receive back animation in OnNavigatedTo
protected override async void OnNavigatedTo(NavigationEventArgs e)
{
    base.OnNavigatedTo(e);

    var animationService = ConnectedAnimationService.GetForCurrentView();
    var animation = animationService.GetAnimation("BackConnectedAnimation");

    if (animation is not null && ViewModel.SelectedItem is not null)
    {
        // Ensure the ListView has scrolled to the item first
        ItemsList.ScrollIntoView(ViewModel.SelectedItem, ScrollIntoViewAlignment.Default);
        await ItemsList.TryStartConnectedAnimationAsync(
            animation, ViewModel.SelectedItem, "HeroImage");
    }
}
```

---

## Same-Page Element Transition

```csharp
// Animate an element from one position to another on the same page
private void CardButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    var service = ConnectedAnimationService.GetForCurrentView();

    // Step 1: prepare from the source element
    service.PrepareToAnimate("cardExpand", SourceCard);

    // Step 2: swap content (hide source, show destination)
    SourceCard.Visibility = Visibility.Collapsed;
    ExpandedCard.Visibility = Visibility.Visible;

    // Step 3: start the animation on the destination element
    var animation = service.GetAnimation("cardExpand");
    animation?.TryStart(ExpandedCard);
}
```

---

## Animation Configurations

| Configuration | Description |
|---|---|
| `DirectConnectedAnimationConfiguration` | Fast, direct line (default) |
| `GravityConnectedAnimationConfiguration` | Natural gravity-based arc |
| `BasicConnectedAnimationConfiguration` | Cross-fade, no movement |

```csharp
animation.Configuration = new GravityConnectedAnimationConfiguration();
animation.TryStart(TargetElement);
```

---

## Notes

- Call `PrepareToAnimate()` **before** navigating; the animation token is stored in `ConnectedAnimationService`.
- Retrieve the animation on the destination page in `OnNavigatedTo` or `Loaded` — not later.
- `TryStart()` returns `true` if the animation started successfully; `false` if the source element is no longer in the visual tree.
- For `ListView` / `GridView`, use `TryStartConnectedAnimationAsync()` which handles scrolling into view automatically.
- Use a `string` key that is unique per animation pair; reuse the same key for the return journey.
- `ConnectedAnimation` is a visual effect — ensure fallback behavior (element already at target position) works without it.
