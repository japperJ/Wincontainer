# TextBox

`TextBox` is the standard single-line (or multi-line) text input control.
Use it for forms, search bars, notes, and any free-text input.

---

## Basic TextBox

```xaml
<TextBox AutomationProperties.Name="Input field" />
```

---

## With Header and Placeholder

```xaml
<TextBox
    Header="Your name"
    PlaceholderText="Enter your name"
    AutomationProperties.Name="Name input" />
```

---

## Read-Only with Custom Styling

```xaml
<TextBox
    Text="Read-only content"
    IsReadOnly="True"
    FontFamily="Consolas"
    FontSize="14"
    Foreground="{ThemeResource TextFillColorSecondaryBrush}"
    AutomationProperties.Name="Read-only text" />
```

---

## Multi-Line TextBox (Notes / Comments)

```xaml
<TextBox
    Header="Notes"
    PlaceholderText="Add a note…"
    AcceptsReturn="True"
    TextWrapping="Wrap"
    IsSpellCheckEnabled="True"
    MinHeight="100"
    MaxLength="500"
    AutomationProperties.Name="Notes input" />
```

---

## TextBox with Clear Button and Search Icon

```xaml
<TextBox
    PlaceholderText="Search…"
    QueryIcon="Find"
    ClearButtonMode="WhileEditing"
    Width="280"
    AutomationProperties.Name="Search box" />
```

---

## MVVM — Two-Way Binding

```xaml
<!-- View -->
<StackPanel Spacing="8">
    <TextBox
        Header="Email"
        PlaceholderText="user@example.com"
        Text="{x:Bind ViewModel.Email, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
        InputScope="EmailNameOrAddress"
        AutomationProperties.Name="Email input" />
    <TextBlock
        Text="{x:Bind ViewModel.ValidationMessage, Mode=OneWay}"
        Foreground="{ThemeResource SystemFillColorCriticalBrush}"
        Visibility="{x:Bind ViewModel.HasError, Mode=OneWay}" />
</StackPanel>
```

```csharp
// ViewModels/ProfileViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValidationMessage))]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string _email = string.Empty;

    public string ValidationMessage => string.IsNullOrWhiteSpace(Email) || Email.Contains('@')
        ? string.Empty
        : "Please enter a valid email address.";

    public bool HasError => !string.IsNullOrEmpty(ValidationMessage);
}
```

---

## Notes

- `UpdateSourceTrigger=PropertyChanged` updates the binding on every keystroke (default is `LostFocus`).
- Use `InputScope` (`EmailNameOrAddress`, `Number`, `Url`, etc.) to show the correct soft keyboard on touch devices.
- `MaxLength` limits character input; `IsReadOnly` prevents editing while still allowing selection/copy.
- `ClearButtonMode="WhileEditing"` shows an X button to clear text.
- For password input, use `PasswordBox`; for numeric input, use `NumberBox`.
- `AcceptsReturn="True"` + `TextWrapping="Wrap"` = multi-line; still scrolls vertically when content overflows.
