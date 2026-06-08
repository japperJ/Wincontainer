# HyperlinkButton

`HyperlinkButton` looks like a hyperlink and can navigate to a URI or trigger a `Click` event.
Use it for in-line navigation links or "learn more" style actions.

---

## Navigate to URI

```xaml
<HyperlinkButton
    Content="Visit Microsoft"
    NavigateUri="https://www.microsoft.com"
    AutomationProperties.Name="Visit Microsoft website" />
```

---

## Click Event (In-App Navigation)

```xaml
<HyperlinkButton
    Content="Go to Settings"
    Click="SettingsLink_Click"
    AutomationProperties.Name="Navigate to settings" />
```

```csharp
// MainPage.xaml.cs
private void SettingsLink_Click(object sender, RoutedEventArgs e)
{
    Frame.Navigate(typeof(SettingsPage));
}
```

---

## Disabled State

```xaml
<HyperlinkButton
    Content="Unavailable link"
    IsEnabled="False"
    NavigateUri="https://example.com"
    AutomationProperties.Name="Unavailable link" />
```

---

## MVVM — Command-Based Navigation

```xaml
<!-- View -->
<HyperlinkButton
    Content="View full report"
    Command="{x:Bind ViewModel.ViewReportCommand}"
    AutomationProperties.Name="View full report" />
```

```csharp
// ViewModels/SummaryViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class SummaryViewModel : ObservableObject
{
    private readonly INavigationService _navigation;

    public SummaryViewModel(INavigationService navigation)
    {
        _navigation = navigation;
    }

    [RelayCommand]
    private void ViewReport()
    {
        _navigation.NavigateTo(nameof(ReportPage));
    }
}
```

---

## Notes

- When `NavigateUri` is set, clicking opens the URI in the default browser — no code-behind needed.
- When `NavigateUri` is **not** set, handle the `Click` event or use a `Command`.
- Do **not** set both `NavigateUri` and `Click` — `NavigateUri` takes precedence.
- `HyperlinkButton` inherits from `ButtonBase`, so all standard button styling applies.
- For inline text links inside a `TextBlock`, use `Hyperlink` (an inline element) instead.
