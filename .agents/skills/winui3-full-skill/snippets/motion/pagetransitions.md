# Page Transitions

Page transitions animate content as users navigate between pages in a `Frame`. Apply a `NavigationThemeTransition` to the `Frame.ContentTransitions` collection, then pass a `NavigationTransitionInfo` object to `Frame.Navigate()` to select the style per navigation.

---

## Basic Setup — NavigationThemeTransition on the Frame

```xaml
<!-- Views/ShellPage.xaml -->
<Frame x:Name="ContentFrame">
    <Frame.ContentTransitions>
        <TransitionCollection>
            <NavigationThemeTransition />
        </TransitionCollection>
    </Frame.ContentTransitions>
</Frame>
```

---

## Navigating with a Specific Transition

```csharp
// Navigate forward with Entrance (slide up + fade in)
ContentFrame.Navigate(typeof(SettingsPage), null,
    new EntranceNavigationTransitionInfo());

// Navigate backward with Slide from Left
ContentFrame.Navigate(typeof(HomePage), null,
    new SlideNavigationTransitionInfo
    {
        Effect = SlideNavigationTransitionEffect.FromLeft
    });

// Drill-in (zoom in from center — for hierarchical navigation)
ContentFrame.Navigate(typeof(DetailPage), item,
    new DrillInNavigationTransitionInfo());

// Suppress all animation (instant switch)
ContentFrame.Navigate(typeof(LoadingPage), null,
    new SuppressNavigationTransitionInfo());
```

---

## Available NavigationTransitionInfo Types

| Class | Effect | Typical use |
|---|---|---|
| `EntranceNavigationTransitionInfo` | Slide up + fade in (default) | First-time page load |
| `SlideNavigationTransitionInfo` | Slide from left or right | Back/forward paging |
| `DrillInNavigationTransitionInfo` | Zoom in from center | Hierarchical drill-down |
| `SuppressNavigationTransitionInfo` | No animation | Instant switch (settings restore) |

---

## Integration with NavigationView

```csharp
// Views/ShellPage.xaml.cs
private void NavView_SelectionChanged(NavigationView sender,
    NavigationViewSelectionChangedEventArgs args)
{
    if (args.IsSettingsSelected)
    {
        ContentFrame.Navigate(typeof(SettingsPage),
            null, args.RecommendedNavigationTransitionInfo);
        return;
    }

    if (args.SelectedItemContainer?.Tag is Type pageType)
    {
        // Use the recommended transition from NavigationView
        ContentFrame.Navigate(pageType, null,
            args.RecommendedNavigationTransitionInfo);
    }
}
```

`args.RecommendedNavigationTransitionInfo` picks the appropriate animation based on how the user navigated (click, keyboard, etc.).

---

## Back Navigation with GoBack

```csharp
private void BackButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    if (ContentFrame.CanGoBack)
    {
        ContentFrame.GoBack(new SlideNavigationTransitionInfo
        {
            Effect = SlideNavigationTransitionEffect.FromRight
        });
    }
}
```

---

## Disabling Transitions on a Specific Page

```csharp
ContentFrame.Navigate(typeof(SplashPage), null,
    new SuppressNavigationTransitionInfo());
```

---

## Notes

- `NavigationThemeTransition` on `ContentTransitions` is the entry point — without it, `Frame.Navigate` uses no animation regardless of `NavigationTransitionInfo`.
- Always use `args.RecommendedNavigationTransitionInfo` from `NavigationView.SelectionChanged` for automatic, Fluent-compliant transitions.
- `SlideNavigationTransitionInfo.Effect` values: `FromLeft`, `FromRight` (use `FromRight` for forward, `FromLeft` for backward navigation to match user expectation).
- Do not add transitions to both `ContentTransitions` **and** the navigate call — they stack. Pick one approach.
- `DrillInNavigationTransitionInfo` works best for 2-level hierarchies; for deeper drill-downs prefer `SlideNavigationTransitionInfo`.
