# CalendarDatePicker

A control that lets the user pick a single date from an inline `CalendarView` that flies out on demand. Combines the compactness of a text field with the richness of a full calendar picker.

---

## Basic Usage

```xaml
<CalendarDatePicker
    Header="Calendar"
    PlaceholderText="Pick a date" />
```

---

## Binding the Selected Date (ViewModel)

```xaml
<CalendarDatePicker
    Header="Select date"
    PlaceholderText="Pick a date"
    Date="{x:Bind ViewModel.SelectedDate, Mode=TwoWay}" />
```

```csharp
// ViewModels/ScheduleViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ScheduleViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset? _selectedDate;

    partial void OnSelectedDateChanged(DateTimeOffset? value)
    {
        if (value.HasValue)
        {
            System.Diagnostics.Debug.WriteLine($"Date selected: {value.Value:D}");
        }
    }
}
```

---

## With Min/Max Date Range

```xaml
<CalendarDatePicker
    Header="Pick a date within next 30 days"
    PlaceholderText="Pick a date"
    Date="{x:Bind ViewModel.SelectedDate, Mode=TwoWay}"
    MinDate="{x:Bind ViewModel.MinDate}"
    MaxDate="{x:Bind ViewModel.MaxDate}" />
```

```csharp
// ViewModels/RangePickerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class RangePickerViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset? _selectedDate;

    public DateTimeOffset MinDate { get; } = DateTimeOffset.Now;
    public DateTimeOffset MaxDate { get; } = DateTimeOffset.Now.AddDays(30);
}
```

---

## Handling DateChanged in Code-Behind

```xaml
<CalendarDatePicker
    x:Name="MyDatePicker"
    Header="Appointment date"
    PlaceholderText="Pick a date"
    DateChanged="MyDatePicker_DateChanged" />
```

```csharp
// Views/AppointmentPage.xaml.cs
private void MyDatePicker_DateChanged(CalendarDatePicker sender,
    CalendarDatePickerDateChangedEventArgs args)
{
    if (args.NewDate.HasValue)
    {
        System.Diagnostics.Debug.WriteLine($"New date: {args.NewDate.Value:d}");
    }
}
```

---

## Variants

### Blackout dates (unavailable)
```xaml
<CalendarDatePicker
    x:Name="PickerWithBlackout"
    Header="Pick a date"
    CalendarViewDayItemChanging="PickerWithBlackout_DayItemChanging" />
```

```csharp
// Mark weekends as blackout
private void PickerWithBlackout_DayItemChanging(CalendarView sender,
    CalendarViewDayItemChangingEventArgs args)
{
    if (args.Item.Date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
    {
        args.Item.IsBlackout = true;
    }
}
```

---

## Notes

- `Date` is `DateTimeOffset?` — check for `null` before using.
- Use `MinDate` / `MaxDate` to restrict the selectable range.
- `CalendarViewDayItemChanging` lets you mark individual days as blackout (greyed out / not selectable).
- The flyout calendar respects the system language/calendar identifier; set `CalendarIdentifier` on the inner `CalendarView` via `CalendarViewStyle` for custom calendar systems.
- Always supply `AutomationProperties.Name` when no `Header` is shown, for accessibility.
