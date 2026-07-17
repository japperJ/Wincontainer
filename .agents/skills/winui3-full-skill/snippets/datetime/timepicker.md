# TimePicker

A control that lets the user choose a time using separate hour, minute, and AM/PM (or 24-hour) spinners.

---

## Basic Usage

```xaml
<TimePicker />
```

---

## With Header and Minute Increments

```xaml
<TimePicker Header="Arrival time" MinuteIncrement="15" />
```

---

## 24-Hour Clock, Initialized to Current Time

```xaml
<TimePicker
    xmlns:sys="using:System"
    Header="24 hour clock"
    ClockIdentifier="24HourClock"
    SelectedTime="{x:Bind sys:DateTime.Now.TimeOfDay}" />
```

---

## Binding to ViewModel

```xaml
<TimePicker
    Header="Meeting time"
    ClockIdentifier="12HourClock"
    SelectedTime="{x:Bind ViewModel.MeetingTime, Mode=TwoWay}" />
```

```csharp
// ViewModels/MeetingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class MeetingViewModel : ObservableObject
{
    [ObservableProperty]
    private TimeSpan? _meetingTime = new TimeSpan(9, 0, 0); // 9:00 AM default
}
```

---

## Handling SelectedTimeChanged in Code-Behind

```xaml
<TimePicker
    x:Name="AlarmPicker"
    Header="Alarm time"
    SelectedTimeChanged="AlarmPicker_SelectedTimeChanged" />
```

```csharp
// Views/AlarmPage.xaml.cs
private void AlarmPicker_SelectedTimeChanged(TimePicker sender,
    TimePickerSelectedValueChangedEventArgs args)
{
    if (args.NewTime.HasValue)
    {
        System.Diagnostics.Debug.WriteLine($"Alarm set for: {args.NewTime.Value:hh\\:mm}");
    }
}
```

---

## Variants

| Property | Example Value | Description |
|---|---|---|
| `ClockIdentifier` | `"12HourClock"` / `"24HourClock"` | Clock format; default is system setting |
| `MinuteIncrement` | `1`, `5`, `15`, `30` | Step size for minutes spinner |
| `Header` | `"Pick a time"` | Label displayed above the control |
| `SelectedTime` | `TimeSpan?` | Two-way bindable selected value |

---

## Notes

- `SelectedTime` is `TimeSpan?`; check for `null` before using `.Value`.
- `MinuteIncrement` must evenly divide 60 (1, 2, 3, 4, 5, 6, 10, 12, 15, 20, 30, 60).
- `ClockIdentifier` defaults to the system setting; explicitly set it when your app always needs 24-hour display.
- Combine with `DatePicker` to capture a full date-time; merge the two values in your ViewModel.
- Always supply `AutomationProperties.Name` when no `Header` is shown.
