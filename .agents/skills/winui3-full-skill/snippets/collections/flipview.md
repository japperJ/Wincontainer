# FlipView

`FlipView` displays one item at a time and lets users navigate between items with
previous/next buttons or swipe gestures. Use it for image carousels, onboarding walkthroughs,
and photo viewers.

---

## Basic Image Carousel

```xaml
<FlipView
    x:Name="PhotoFlipView"
    ItemsSource="{x:Bind ViewModel.Photos, Mode=OneWay}"
    AutomationProperties.Name="Photo carousel">
    <FlipView.ItemTemplate>
        <DataTemplate x:DataType="local:Photo">
            <Image
                Source="{x:Bind Uri}"
                Stretch="Uniform"
                AutomationProperties.Name="{x:Bind Title}" />
        </DataTemplate>
    </FlipView.ItemTemplate>
</FlipView>
```

---

## With PipsPager Indicator

```xaml
<StackPanel Spacing="8">
    <FlipView
        x:Name="CarouselView"
        ItemsSource="{x:Bind ViewModel.Slides, Mode=OneWay}"
        AutomationProperties.Name="Onboarding slides">
        <FlipView.ItemTemplate>
            <DataTemplate x:DataType="local:Slide">
                <StackPanel Padding="24" Spacing="12" VerticalAlignment="Center"
                            HorizontalAlignment="Center">
                    <FontIcon Glyph="{x:Bind Icon}" FontSize="48" />
                    <TextBlock
                        Text="{x:Bind Title}"
                        Style="{ThemeResource TitleTextBlockStyle}"
                        TextAlignment="Center" />
                    <TextBlock
                        Text="{x:Bind Description}"
                        Style="{ThemeResource BodyTextBlockStyle}"
                        TextAlignment="Center"
                        TextWrapping="WrapWholeWords"
                        MaxWidth="360" />
                </StackPanel>
            </DataTemplate>
        </FlipView.ItemTemplate>
    </FlipView>
    <PipsPager
        NumberOfPages="{x:Bind ViewModel.Slides.Count, Mode=OneWay}"
        SelectedPageIndex="{x:Bind CarouselView.SelectedIndex, Mode=TwoWay}"
        HorizontalAlignment="Center" />
</StackPanel>
```

---

## MVVM ViewModel

```csharp
// ViewModels/OnboardingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class OnboardingViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NextCommand))]
    [NotifyCanExecuteChangedFor(nameof(BackCommand))]
    private int _currentIndex;

    public List<Slide> Slides { get; } = new()
    {
        new Slide { Title = "Welcome",    Icon = "\uE8F4", Description = "Discover amazing features." },
        new Slide { Title = "Organize",   Icon = "\uE8B7", Description = "Keep everything in order." },
        new Slide { Title = "Get started",Icon = "\uE8FB", Description = "You're ready to go!" },
    };

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next() => CurrentIndex++;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => CurrentIndex--;

    private bool CanGoNext() => CurrentIndex < Slides.Count - 1;
    private bool CanGoBack() => CurrentIndex > 0;
}
```

---

## Notes

- `FlipView` has built-in previous/next arrow buttons — they appear on hover by default.
- Pair with `PipsPager` (bind `SelectedPageIndex` ↔ `FlipView.SelectedIndex`) for a
  page indicator.
- For a horizontal photo strip with full navigation, use `FlipView`; for a thumbnail grid,
  use `GridView`.
- `UseTouchAnimationsForAllNavigation="True"` enables swipe animations on desktop (opt-in).
- Virtualization is enabled automatically for large collections.
