# TeachingTip

`TeachingTip` is a rich, non-modal callout bubble for onboarding, feature announcements,
and contextual guidance. It can be anchored to a target control or shown in the center of the page.

---

## Basic TeachingTip (Page Center)

```xaml
<TeachingTip
    x:Name="WelcomeTip"
    Title="Welcome to MyApp"
    Subtitle="Tap the star to save items to your favorites."
    IsOpen="False"
    CloseButtonContent="Got it"
    AutomationProperties.Name="Welcome tip" />
```

```csharp
// Show on first launch
private void Page_Loaded(object sender, RoutedEventArgs e)
{
    if (!ViewModel.HasSeenWelcome)
    {
        WelcomeTip.IsOpen = true;
        ViewModel.HasSeenWelcome = true;
    }
}
```

---

## Anchored to a Target Control

```xaml
<Button
    x:Name="FavoriteButton"
    Content="Favorite"
    AutomationProperties.Name="Favorite" />

<TeachingTip
    x:Name="FavoriteTip"
    Target="{x:Bind FavoriteButton}"
    Title="Save favorites"
    Subtitle="Click this button to save items to your favorites list."
    PreferredPlacement="Bottom"
    IsOpen="False"
    CloseButtonContent="OK" />
```

---

## With Hero Image and Action Button

```xaml
<TeachingTip
    x:Name="FeatureTip"
    Title="New: Smart Suggestions"
    Subtitle="We now suggest items based on your history."
    HeroImageSource="/Assets/feature-preview.png"
    ActionButtonContent="Try it now"
    ActionButtonClick="FeatureTip_ActionClick"
    CloseButtonContent="Dismiss"
    IsOpen="False">
</TeachingTip>
```

```csharp
private void FeatureTip_ActionClick(TeachingTip sender, object args)
{
    sender.IsOpen = false;
    ViewModel.NavigateToSuggestions();
}
```

---

## MVVM — Open/Close via ViewModel

```xaml
<!-- View -->
<TeachingTip
    x:Name="OnboardingTip"
    Target="{x:Bind SearchBox}"
    Title="Search anything"
    Subtitle="Use the search box to find controls, docs, and more."
    IsOpen="{x:Bind ViewModel.ShowSearchTip, Mode=TwoWay}"
    CloseButtonContent="Got it" />
```

```csharp
// ViewModels/OnboardingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _showSearchTip;

    public void StartOnboarding()
    {
        ShowSearchTip = true;
    }
}
```

---

## Programmatic Show with Preferred Placement

```csharp
private void ShowNewFeatureTip()
{
    NewFeatureTip.PreferredPlacement = TeachingTipPlacementMode.BottomLeft;
    NewFeatureTip.IsOpen = true;
}
```

---

## Notes

- `TeachingTip` is non-modal — the app remains interactive while it is shown.
  For required decisions, use `ContentDialog`.
- `Target` anchors the tip to a `FrameworkElement`; omit it for a centered/page-level tip.
- `PreferredPlacement` specifies the preferred side; WinUI automatically flips if there isn't room.
- `HeroImageSource` accepts any `ImageSource` for a top illustration.
- `ActionButtonContent` adds a primary action button; `CloseButtonContent` is the dismiss button.
- `IsLightDismissEnabled="True"` allows the tip to close when the user taps outside.
- Trigger tips on user actions (first time), not on every page load.
