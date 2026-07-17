# RatingControl

`RatingControl` displays a star rating and allows users to select or view a rating value.
Use it for product reviews, feedback forms, or anywhere an aggregate rating is shown.

---

## Basic RatingControl

```xaml
<RatingControl
    AutomationProperties.Name="Product rating" />
```

---

## With Caption and Clear Support

```xaml
<RatingControl
    x:Name="ProductRating"
    Caption="1,234 ratings"
    IsClearEnabled="True"
    ValueChanged="ProductRating_ValueChanged"
    AutomationProperties.Name="Product star rating" />
```

```csharp
// MainPage.xaml.cs
private void ProductRating_ValueChanged(RatingControl sender, object args)
{
    // sender.Value is -1 when cleared (no rating selected)
    string text = sender.Value < 0 ? "No rating" : $"{sender.Value} stars";
    System.Diagnostics.Debug.WriteLine(text);
}
```

---

## Read-Only Display Rating

```xaml
<RatingControl
    Value="3.7"
    IsReadOnly="True"
    Caption="(412)"
    AutomationProperties.Name="Average rating: 3.7 out of 5" />
```

---

## Placeholder Value (Community Average Preview)

```xaml
<!-- Shows a community average until the user makes a selection -->
<RatingControl
    PlaceholderValue="3.5"
    AutomationProperties.Name="Rating with placeholder" />
```

---

## MVVM — Bound to ViewModel

```xaml
<!-- View -->
<StackPanel Spacing="4">
    <RatingControl
        Value="{x:Bind ViewModel.UserRating, Mode=TwoWay}"
        Caption="{x:Bind ViewModel.RatingCaption, Mode=OneWay}"
        IsClearEnabled="True"
        AutomationProperties.Name="User rating" />
    <TextBlock Text="{x:Bind ViewModel.RatingFeedback, Mode=OneWay}" />
</StackPanel>
```

```csharp
// ViewModels/ReviewViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ReviewViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RatingFeedback))]
    private double _userRating = -1;  // -1 = no rating

    public string RatingCaption => "Rate this item";

    public string RatingFeedback => UserRating < 0
        ? "Tap a star to rate"
        : $"You rated: {UserRating} star{(UserRating == 1 ? "" : "s")}";
}
```

---

## Notes

- `Value` of `-1` means no rating is selected (cleared).
- `PlaceholderValue` shows a dimmed "ghost" rating — good for showing a community average before a user votes.
- `IsClearEnabled="True"` lets users swipe left or click the selected star again to clear.
- `IsReadOnly="True"` turns the control into a display-only widget.
- Maximum stars defaults to 5; set `MaxRating` to change it.
