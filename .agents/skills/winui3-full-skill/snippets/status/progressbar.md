# ProgressBar

`ProgressBar` displays a horizontal bar to communicate either determinate progress (known percentage) or indeterminate progress (unknown duration). It also supports `ShowPaused` and `ShowError` states.

## Indeterminate ProgressBar

```xaml
<ProgressBar
    Width="200"
    IsIndeterminate="True" />
```

## Determinate ProgressBar

```xaml
<ProgressBar
    x:Name="UploadBar"
    Width="300"
    AutomationProperties.Name="Upload progress"
    Maximum="100"
    Minimum="0"
    Value="{x:Bind ViewModel.UploadProgress, Mode=OneWay}" />
```

## Paused and Error states

```xaml
<!-- Paused (amber colour) -->
<ProgressBar
    Width="200"
    IsIndeterminate="True"
    ShowPaused="True" />

<!-- Error (red colour) -->
<ProgressBar
    Width="200"
    IsIndeterminate="True"
    ShowError="True" />
```

## Full example with ViewModel

```xaml
<StackPanel Spacing="12">
    <ProgressBar
        Width="400"
        AutomationProperties.Name="File download progress"
        IsIndeterminate="{x:Bind ViewModel.IsIndeterminate, Mode=OneWay}"
        Maximum="100"
        ShowError="{x:Bind ViewModel.HasError, Mode=OneWay}"
        ShowPaused="{x:Bind ViewModel.IsPaused, Mode=OneWay}"
        Value="{x:Bind ViewModel.Progress, Mode=OneWay}" />

    <TextBlock Text="{x:Bind ViewModel.StatusText, Mode=OneWay}" />

    <StackPanel Orientation="Horizontal" Spacing="8">
        <Button Command="{x:Bind ViewModel.StartCommand}" Content="Start" />
        <Button Command="{x:Bind ViewModel.PauseCommand}" Content="Pause" />
        <Button Command="{x:Bind ViewModel.CancelCommand}" Content="Cancel" />
    </StackPanel>
</StackPanel>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class DownloadViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private double _progress;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    [ObservableProperty]
    private bool _isPaused;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _statusText = "Ready";

    [RelayCommand]
    private async Task StartAsync()
    {
        _cts = new CancellationTokenSource();
        IsIndeterminate = false;
        HasError = false;
        IsPaused = false;
        StatusText = "Downloading…";

        try
        {
            for (int i = 0; i <= 100; i += 5)
            {
                _cts.Token.ThrowIfCancellationRequested();
                Progress = i;
                await Task.Delay(200, _cts.Token);
            }
            StatusText = "Complete";
        }
        catch (OperationCanceledException)
        {
            StatusText = "Cancelled";
            Progress = 0;
        }
        catch (Exception ex)
        {
            HasError = true;
            StatusText = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Pause()
    {
        IsPaused = !IsPaused;
        StatusText = IsPaused ? "Paused" : "Downloading…";
    }

    [RelayCommand]
    private void Cancel() => _cts?.Cancel();
}
```

## Notes

- `IsIndeterminate="True"` animates continuously; set `False` and bind `Value` for known progress.
- `Value` must be between `Minimum` (default `0`) and `Maximum` (default `100`).
- `ShowPaused="True"` changes the colour to amber — useful when a background operation is paused.
- `ShowError="True"` changes the colour to red — always accompany with an `InfoBar` or similar message.
- Always set `AutomationProperties.Name` so screen readers announce what the bar measures.
- For ring-style progress, use `ProgressRing` instead.
