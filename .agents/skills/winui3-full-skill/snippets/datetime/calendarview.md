# CalendarView

An always-visible, full-month calendar control that supports single, multiple, or range date selection. Unlike `CalendarDatePicker`, the calendar is inline (not flyout).

---

## Basic Usage

```xaml
<CalendarView SelectionMode="Single" />
```

---

## Single Selection Bound to ViewModel

```xaml
<CalendarView
    SelectionMode="Single"
    SelectedDatesChanged="CalendarView_SelectedDatesChanged" />
```

```csharp
// Views/CalendarPage.xaml.cs
private void CalendarView_SelectedDatesChanged(CalendarView sender,
    CalendarViewSelectedDatesChangedEventArgs args)
{
    if (sender.SelectedDates.Count > 0)
    {
        ViewModel.SelectedDate = sender.SelectedDates[0];
    }
}
```

```csharp
// ViewModels/CalendarViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class CalendarViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
}
```

---

## Multiple Selection

```xaml
<CalendarView
    SelectionMode="Multiple"
    SelectedDatesChanged="CalendarView_SelectedDatesChanged" />
```

```csharp
private void CalendarView_SelectedDatesChanged(CalendarView sender,
    CalendarViewSelectedDatesChangedEventArgs args)
{
    foreach (var addedDate in args.AddedDates)
        System.Diagnostics.Debug.WriteLine($"Added: {addedDate:d}");

    foreach (var removedDate in args.RemovedDates)
        System.Diagnostics.Debug.WriteLine($"Removed: {removedDate:d}");
}
```

---

## With Calendar Options

```xaml
<CalendarView
    SelectionMode="Single"
    IsGroupLabelVisible="True"
    IsOutOfScopeEnabled="True"
    CalendarIdentifier="GregorianCalendar"
    Language="en-US" />
```

---

## Marking Specific Days (DayItemChanging)

```xaml
<CalendarView
    SelectionMode="Single"
    CalendarViewDayItemChanging="CalendarView_DayItemChanging" />
```

```csharp
private void CalendarView_DayItemChanging(CalendarView sender,
    CalendarViewDayItemChangingEventArgs args)
{
    // Phase 0: register for density bars
    if (args.Phase == 0)
    {
        args.RegisterUpdateCallback(CalendarView_DayItemChanging);
        return;
    }

    // Phase 1: set density bars for days with events
    if (args.Phase == 1)
    {
        // Example: highlight first day of each month
        if (args.Item.Date.Day == 1)
        {
            args.Item.SetDensityColors(new[] { Microsoft.UI.Colors.Orange });
        }
    }
}
```

---

## Variants

| Property | Values | Description |
|---|---|---|
| `SelectionMode` | `None`, `Single`, `Multiple` | How many dates can be selected |
| `IsGroupLabelVisible` | `true`/`false` | Show month/year label above week rows |
| `IsOutOfScopeEnabled` | `true`/`false` | Show days outside the current month |
| `CalendarIdentifier` | e.g. `"GregorianCalendar"`, `"HebrewCalendar"` | Calendar system to use |
| `Language` | BCP-47 tag e.g. `"ar-SA"` | Localization for day/month names |

---

## Notes

- `SelectedDates` is read-only; add or remove dates by calling `SelectedDates.Add(date)` / `SelectedDates.Remove(date)` in code.
- For a compact flyout variant, use `CalendarDatePicker` instead.
- Use `MinDate` and `MaxDate` to limit the visible/selectable range.
- `SetDensityColors` (called during `CalendarViewDayItemChanging` phase 1+) renders coloured bars under dates — useful for event indicators.
- Always supply `AutomationProperties.Name` for accessibility when embedding standalone.
