# Popup

`Popup` is a low-level overlay primitive that displays arbitrary content above other UI. It does not provide chrome or light-dismiss by default — use `Flyout` or `ContentDialog` for those scenarios. Use `Popup` when you need pixel-precise positioning or custom overlay behaviour.

## Basic Popup with offset positioning

```xaml
<Grid x:Name="RootGrid" HorizontalAlignment="Left" VerticalAlignment="Top">
    <Button Click="ShowPopup_Click" Content="Show Popup" />

    <Popup
        x:Name="StandardPopup"
        Closed="Popup_Closed"
        HorizontalOffset="200"
        IsLightDismissEnabled="True"
        VerticalOffset="0">
        <Grid
            MinWidth="240"
            Padding="16"
            Background="{ThemeResource AcrylicBackgroundFillColorDefaultBrush}"
            BorderBrush="{ThemeResource SurfaceStrokeColorDefaultBrush}"
            BorderThickness="1"
            CornerRadius="{StaticResource OverlayCornerRadius}">
            <StackPanel Spacing="8">
                <TextBlock FontSize="16" Text="Simple Popup" />
                <Button Click="ClosePopup_Click" Content="Close" />
            </StackPanel>
        </Grid>
    </Popup>
</Grid>
```

```csharp
// Open the Popup
private void ShowPopup_Click(object sender, RoutedEventArgs e)
{
    if (!StandardPopup.IsOpen)
        StandardPopup.IsOpen = true;
}

// Close via the inner button
private void ClosePopup_Click(object sender, RoutedEventArgs e)
{
    if (StandardPopup.IsOpen)
        StandardPopup.IsOpen = false;
}

// Optionally react to the popup closing (e.g. light-dismiss)
private void Popup_Closed(object sender, object e)
{
    // Update VM state if needed
}
```

## Popup bound to ViewModel state

```xaml
<Grid>
    <Button Command="{x:Bind ViewModel.OpenPopupCommand}" Content="Open" />

    <Popup
        x:Name="VmPopup"
        HorizontalOffset="{x:Bind ViewModel.PopupOffsetX, Mode=OneWay}"
        IsLightDismissEnabled="True"
        IsOpen="{x:Bind ViewModel.IsPopupOpen, Mode=TwoWay}"
        VerticalOffset="{x:Bind ViewModel.PopupOffsetY, Mode=OneWay}">
        <Border
            Padding="20"
            Background="{ThemeResource LayerFillColorDefaultBrush}"
            BorderBrush="{ThemeResource SurfaceStrokeColorDefaultBrush}"
            BorderThickness="1"
            CornerRadius="8">
            <StackPanel Spacing="12">
                <TextBlock Text="Popup content" />
                <Button Command="{x:Bind ViewModel.ClosePopupCommand}" Content="Dismiss" />
            </StackPanel>
        </Border>
    </Popup>
</Grid>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class PopupDemoViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isPopupOpen;

    [ObservableProperty]
    private double _popupOffsetX = 100;

    [ObservableProperty]
    private double _popupOffsetY = 50;

    [RelayCommand]
    private void OpenPopup() => IsPopupOpen = true;

    [RelayCommand]
    private void ClosePopup() => IsPopupOpen = false;
}
```

## Popup anchored to an element

```csharp
// Position popup relative to a button's location on-screen
private void AnchorButton_Click(object sender, RoutedEventArgs e)
{
    var button = (FrameworkElement)sender;
    var transform = button.TransformToVisual(RootGrid);
    var position = transform.TransformPoint(new Windows.Foundation.Point(0, button.ActualHeight + 4));

    AnchoredPopup.HorizontalOffset = position.X;
    AnchoredPopup.VerticalOffset = position.Y;
    AnchoredPopup.IsOpen = true;
}
```

## Notes

- `Popup` has no built-in dismiss button or backdrop — add `IsLightDismissEnabled="True"` for touch/click-outside dismissal.
- For most scenarios prefer `Flyout` (auto-positioned, light-dismiss, accessible) or `ContentDialog` (modal, keyboard-trapped).
- `Popup` content is **not** part of the visual tree until `IsOpen = true`; bindings inside are not evaluated until first open.
- Set `XamlRoot` when opening a `Popup` from outside the main window tree:
  ```csharp
  popup.XamlRoot = this.XamlRoot;
  ```
- `HorizontalOffset` / `VerticalOffset` are relative to the popup's **parent element**, not the window.
- Use `ThemeResource AcrylicBackgroundFillColorDefaultBrush` or `LayerFillColorDefaultBrush` for the popup surface to match system theming.
