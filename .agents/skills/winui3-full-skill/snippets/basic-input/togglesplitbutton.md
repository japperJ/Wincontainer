# ToggleSplitButton

`ToggleSplitButton` combines a toggle area (left) with a flyout chevron (right).
The toggle area checks/unchecks and fires `IsCheckedChanged`; the chevron opens a `Flyout`
to change what the toggle action does. Typical use: list-style toggles where the user
can also select a format variant (e.g., bullet list type).

---

## Basic ToggleSplitButton

```xaml
<ToggleSplitButton
    x:Name="ListFormatButton"
    IsCheckedChanged="ListFormatButton_IsCheckedChanged"
    AutomationProperties.Name="List format toggle">
    <FontIcon Glyph="&#xE8FD;" FontSize="16" />
    <ToggleSplitButton.Flyout>
        <Flyout Placement="BottomEdgeAlignedLeft">
            <StackPanel Spacing="4">
                <Button Content="Bullet list" Click="BulletList_Click" />
                <Button Content="Numbered list" Click="NumberedList_Click" />
                <Button Content="Checklist" Click="Checklist_Click" />
            </StackPanel>
        </Flyout>
    </ToggleSplitButton.Flyout>
</ToggleSplitButton>
```

```csharp
// MainPage.xaml.cs
private void ListFormatButton_IsCheckedChanged(ToggleSplitButton sender,
    ToggleSplitButtonIsCheckedChangedEventArgs args)
{
    if (sender.IsChecked)
        ApplyListFormat(_currentListType);
    else
        RemoveListFormat();
}
```

---

## MVVM Pattern

```xaml
<!-- View -->
<ToggleSplitButton
    IsChecked="{x:Bind ViewModel.IsListActive, Mode=TwoWay}"
    AutomationProperties.Name="Toggle list format">
    <FontIcon Glyph="&#xE8FD;" FontSize="16" />
    <ToggleSplitButton.Flyout>
        <Flyout>
            <StackPanel Spacing="4" Width="160">
                <Button Content="Bullets"
                    Command="{x:Bind ViewModel.SelectBulletsCommand}"
                    HorizontalAlignment="Stretch" />
                <Button Content="Numbered"
                    Command="{x:Bind ViewModel.SelectNumberedCommand}"
                    HorizontalAlignment="Stretch" />
            </StackPanel>
        </Flyout>
    </ToggleSplitButton.Flyout>
</ToggleSplitButton>
```

```csharp
// ViewModels/FormattingViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class FormattingViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isListActive;

    [ObservableProperty]
    private string _listType = "Bullets";

    [RelayCommand]
    private void SelectBullets()
    {
        ListType = "Bullets";
        IsListActive = true;
    }

    [RelayCommand]
    private void SelectNumbered()
    {
        ListType = "Numbered";
        IsListActive = true;
    }
}
```

---

## Notes

- `IsChecked` is `bool` (not nullable) — unlike `ToggleButton`.
- `IsCheckedChanged` fires when the toggle state changes; use it instead of `Checked`/`Unchecked`.
- The flyout only updates *what* the toggle does; actually toggling still requires clicking the left area.
- Combine with `x:Bind` on `IsChecked` for clean ViewModel binding.
- For a primary action + flyout (no toggle state), use `SplitButton` instead.
