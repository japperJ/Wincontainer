# InfoBar

`InfoBar` displays a persistent, non-intrusive status message at the top of a page or section. It supports four severity levels (Informational, Success, Warning, Error) and can contain an optional action button.

## Basic InfoBar

```xaml
<InfoBar
    Title="Update available"
    IsOpen="True"
    Message="A new version of the app is ready to install."
    Severity="Informational" />
```

## Severity variants

```xaml
<!-- Informational (default) -->
<InfoBar
    Title="Info"
    IsOpen="True"
    Message="Essential information for the user."
    Severity="Informational" />

<!-- Success -->
<InfoBar
    Title="Success"
    IsOpen="True"
    Message="Your changes were saved."
    Severity="Success" />

<!-- Warning -->
<InfoBar
    Title="Warning"
    IsOpen="True"
    Message="Network connection is unstable."
    Severity="Warning" />

<!-- Error -->
<InfoBar
    Title="Error"
    IsOpen="True"
    Message="Failed to load data. Please try again."
    Severity="Error" />
```

## InfoBar with action button

```xaml
<InfoBar
    Title="Update available"
    IsOpen="True"
    Message="Restart the app to apply the update."
    Severity="Informational">
    <InfoBar.ActionButton>
        <Button Command="{x:Bind ViewModel.RestartCommand}" Content="Restart now" />
    </InfoBar.ActionButton>
</InfoBar>
```

## InfoBar with hyperlink action

```xaml
<InfoBar
    Title="Learn more"
    IsOpen="True"
    Message="New keyboard shortcuts are available."
    Severity="Informational">
    <InfoBar.ActionButton>
        <HyperlinkButton Content="View shortcuts" NavigateUri="https://aka.ms/shortcuts" />
    </InfoBar.ActionButton>
</InfoBar>
```

## InfoBar bound to ViewModel

```xaml
<InfoBar
    Title="{x:Bind ViewModel.StatusTitle, Mode=OneWay}"
    IsClosable="True"
    IsIconVisible="True"
    IsOpen="{x:Bind ViewModel.IsStatusVisible, Mode=TwoWay}"
    Message="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
    Severity="{x:Bind ViewModel.StatusSeverity, Mode=OneWay}" />
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;

namespace MyApp.ViewModels;

public partial class StatusViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isStatusVisible;

    [ObservableProperty]
    private string _statusTitle = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private InfoBarSeverity _statusSeverity = InfoBarSeverity.Informational;

    public void ShowSuccess(string title, string message)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = InfoBarSeverity.Success;
        IsStatusVisible = true;
    }

    public void ShowError(string title, string message)
    {
        StatusTitle = title;
        StatusMessage = message;
        StatusSeverity = InfoBarSeverity.Error;
        IsStatusVisible = true;
    }

    [RelayCommand]
    private void DismissStatus() => IsStatusVisible = false;
}
```

## Controlling visibility and closability

```xaml
<InfoBar
    Title="Storage almost full"
    IsClosable="True"
    IsIconVisible="True"
    IsOpen="{x:Bind ViewModel.ShowStorageWarning, Mode=TwoWay}"
    Message="You have less than 500 MB remaining."
    Severity="Warning" />
```

## Notes

- `IsOpen` is a **two-way bindable** property: binding `TwoWay` lets the user's close action propagate back to the ViewModel.
- `IsClosable="True"` (default) shows the X button; set `False` to force the user to take an action.
- `IsIconVisible="True"` (default) shows the severity icon.
- Place `InfoBar` at the top of a `StackPanel` or `Grid` row so it doesn't shift content unexpectedly.
- `Severity` maps to `InfoBarSeverity` enum: `Informational`, `Success`, `Warning`, `Error`.
- Avoid showing multiple `InfoBar` controls simultaneously; prefer a queue pattern if multiple messages can occur.
