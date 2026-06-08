# PasswordBox

`PasswordBox` is a text input that masks characters and is designed for secure password entry.

---

## Basic PasswordBox

```xaml
<PasswordBox
    Header="Password"
    PlaceholderText="Enter password"
    AutomationProperties.Name="Password input" />
```

---

## With Password Reveal Button

```xaml
<!-- PasswordRevealMode is ShowAll, Hidden, or Peek (default) -->
<PasswordBox
    Header="Password"
    PlaceholderText="Enter password"
    PasswordRevealMode="Peek"
    AutomationProperties.Name="Password" />
```

---

## With Max Length

```xaml
<PasswordBox
    Header="PIN"
    PlaceholderText="4-digit PIN"
    MaxLength="4"
    InputScope="NumericPin"
    AutomationProperties.Name="PIN input" />
```

---

## MVVM — Bound to ViewModel (PasswordChanged Event)

> **Important**: `PasswordBox.Password` does not support `x:Bind` `TwoWay` for security reasons.
> Use `PasswordChanged` to push the value to the ViewModel.

```xaml
<!-- View -->
<StackPanel Spacing="8">
    <PasswordBox
        x:Name="PasswordInput"
        Header="Password"
        PlaceholderText="Enter password"
        PasswordChanged="PasswordInput_PasswordChanged"
        AutomationProperties.Name="Password" />
    <PasswordBox
        x:Name="ConfirmPasswordInput"
        Header="Confirm password"
        PlaceholderText="Re-enter password"
        PasswordChanged="ConfirmPasswordInput_PasswordChanged"
        AutomationProperties.Name="Confirm password" />
    <TextBlock
        Text="{x:Bind ViewModel.PasswordError, Mode=OneWay}"
        Foreground="{ThemeResource SystemFillColorCriticalBrush}" />
    <Button
        Content="Create account"
        Command="{x:Bind ViewModel.CreateAccountCommand}"
        IsEnabled="{x:Bind ViewModel.CanCreate, Mode=OneWay}" />
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private void PasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
{
    ViewModel.Password = PasswordInput.Password;
}

private void ConfirmPasswordInput_PasswordChanged(object sender, RoutedEventArgs e)
{
    ViewModel.ConfirmPassword = ConfirmPasswordInput.Password;
}
```

```csharp
// ViewModels/RegisterViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    // Not [ObservableProperty] — never log or serialize passwords
    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;

    public string Password
    {
        set
        {
            _password = value;
            OnPropertyChanged(nameof(PasswordError));
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    public string ConfirmPassword
    {
        set
        {
            _confirmPassword = value;
            OnPropertyChanged(nameof(PasswordError));
            OnPropertyChanged(nameof(CanCreate));
        }
    }

    public string PasswordError => _password != _confirmPassword
        ? "Passwords do not match."
        : string.Empty;

    public bool CanCreate =>
        _password.Length >= 8 && _password == _confirmPassword;

    [RelayCommand(CanExecute = nameof(CanCreate))]
    private async Task CreateAccountAsync(CancellationToken ct)
    {
        await _authService.RegisterAsync(_password, ct);
        _password = string.Empty;
        _confirmPassword = string.Empty;
    }
}
```

---

## Notes

- Do **not** bind `Password` property with `TwoWay` — this is intentional for security.
  Always use `PasswordChanged` to copy to the ViewModel, and clear the local copy after use.
- `PasswordRevealMode`:
  - `Peek` (default) — shows reveal button, held to reveal
  - `Hidden` — no reveal button
  - `ShowAll` — always visible (use only for non-sensitive inputs)
- Never store the password in an `[ObservableProperty]` — it will be included in change logs.
- For PINs, set `InputScope="NumericPin"` to show the numeric keyboard on touch.
