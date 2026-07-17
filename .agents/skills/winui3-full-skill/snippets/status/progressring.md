# ProgressRing

`ProgressRing` displays a circular spinner to indicate ongoing activity. It supports both **indeterminate** (spinning, unknown duration) and **determinate** (arc fills to a percentage) modes.

## Indeterminate ProgressRing

```xaml
<ProgressRing
    Width="60"
    Height="60"
    AutomationProperties.Name="Loading"
    IsActive="True" />
```

## Toggling active state

```xaml
<ProgressRing
    Width="50"
    Height="50"
    AutomationProperties.Name="Saving"
    IsActive="{x:Bind ViewModel.IsSaving, Mode=OneWay}" />
```

## Determinate ProgressRing

```xaml
<ProgressRing
    Width="80"
    Height="80"
    AutomationProperties.Name="Upload progress"
    IsIndeterminate="False"
    Maximum="100"
    Minimum="0"
    Value="{x:Bind ViewModel.UploadProgress, Mode=OneWay}" />
```

## ProgressRing with overlay text

```xaml
<Grid Width="80" Height="80">
    <ProgressRing
        IsIndeterminate="False"
        Maximum="100"
        Value="{x:Bind ViewModel.Progress, Mode=OneWay}" />
    <TextBlock
        HorizontalAlignment="Center"
        VerticalAlignment="Center"
        FontSize="14"
        Text="{x:Bind ViewModel.ProgressText, Mode=OneWay}" />
</Grid>
```

## Full ViewModel example

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class UploadViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressText))]
    private double _progress;

    [ObservableProperty]
    private bool _isSaving;

    [ObservableProperty]
    private bool _isIndeterminate = true;

    public string ProgressText => $"{(int)Progress}%";

    [RelayCommand]
    private async Task UploadAsync(CancellationToken ct)
    {
        IsSaving = true;
        IsIndeterminate = false;
        Progress = 0;

        try
        {
            for (int i = 0; i <= 100; i += 10)
            {
                ct.ThrowIfCancellationRequested();
                Progress = i;
                await Task.Delay(300, ct);
            }
        }
        finally
        {
            IsSaving = false;
        }
    }
}
```

## ProgressRing in a Button (loading state)

```xaml
<Button
    Command="{x:Bind ViewModel.SaveCommand}"
    IsEnabled="{x:Bind ViewModel.CanSave, Mode=OneWay}">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <ProgressRing
            Width="16"
            Height="16"
            IsActive="{x:Bind ViewModel.IsSaving, Mode=OneWay}"
            Visibility="{x:Bind ViewModel.IsSaving, Mode=OneWay, Converter={StaticResource BoolToVisibilityConverter}}" />
        <TextBlock Text="{x:Bind ViewModel.SaveButtonText, Mode=OneWay}" />
    </StackPanel>
</Button>
```

## Notes

- `IsActive` (default `True`) controls animation for indeterminate mode; binding to a ViewModel bool is the preferred approach.
- `IsIndeterminate` (default `True`) — set `False` and bind `Value` for known percentage.
- `Value` must be in range `[Minimum, Maximum]` (defaults `0–100`).
- Always provide `AutomationProperties.Name` so screen readers announce what is loading.
- The default `Background` is `Transparent`; set `Background="LightGray"` or a `ThemeResource` brush when you need a visible track.
- For horizontal progress display, use `ProgressBar` instead.
