# DatePicker

A control that lets the user choose a date using separate day, month, and year spinners. Best for known dates such as birthdates where the user doesn't need to navigate a full calendar.

---

## Basic Usage

```xaml
<DatePicker Header="Pick a date" />
```

---

## Binding to ViewModel

```xaml
<DatePicker
    Header="Select date"
    SelectedDate="{x:Bind ViewModel.SelectedDate, Mode=TwoWay}" />
```

```csharp
// ViewModels/DatePickerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class DatePickerViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTimeOffset _selectedDate = DateTimeOffset.Now;
}
```

---

## Custom Day Format with Year Hidden

```xaml
<DatePicker
    Header="Day of the week"
    DayFormat="{}{day.integer} ({dayofweek.abbreviated})"
    YearVisible="False" />
```

---

## Handling SelectedDateChanged in Code-Behind

```xaml
<DatePicker
    x:Name="BirthdatePicker"
    Header="Date of birth"
    SelectedDateChanged="BirthdatePicker_SelectedDateChanged" />
```

```csharp
// Views/ProfilePage.xaml.cs
private void BirthdatePicker_SelectedDateChanged(DatePicker sender,
    DatePickerSelectedValueChangedEventArgs args)
{
    if (args.NewDate.HasValue)
    {
        System.Diagnostics.Debug.WriteLine($"Date of birth: {args.NewDate.Value:d}");
    }
}
```

---

## Variants

### Hide individual fields
```xaml
<!-- Month/year picker only -->
<DatePicker Header="Month and year" DayVisible="False" />

<!-- Year only -->
<DatePicker Header="Year" MonthVisible="False" DayVisible="False" />
```

### Pre-set a specific date
```xaml
<DatePicker
    Header="Scheduled date"
    SelectedDate="{x:Bind ViewModel.DeadlineDate, Mode=TwoWay}" />
```

```csharp
// ViewModel
public DateTimeOffset DeadlineDate { get; set; } =
    new DateTimeOffset(2026, 12, 31, 0, 0, 0, TimeSpan.Zero);
```

---

## Notes

- `SelectedDate` is `DateTimeOffset?`; check for `null` before accessing `.Value`.
- `DayVisible`, `MonthVisible`, `YearVisible` control which spinners appear.
- `DayFormat`, `MonthFormat`, `YearFormat` accept Unicode CLDR date skeleton patterns.
- For a richer calendar-navigation experience, use `CalendarDatePicker` or `CalendarView`.
- Always supply `AutomationProperties.Name` when no `Header` is shown.
