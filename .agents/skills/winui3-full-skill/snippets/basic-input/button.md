# Button

**Category:** Basic Input  
**Namespace:** `Microsoft.UI.Xaml.Controls`  
**WinUI Gallery source:** `ControlPages/ButtonPage.xaml`

Triggers an immediate action. Content can be text, icon, or any UIElement.

## Basic button

```xaml
<Button Content="Click me"
        AutomationProperties.Name="Click me"
        Click="Button_Click" />
```

```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    // handle action
}
```

## Built-in style variants

```xaml
<!-- Accent (primary action) -->
<Button Content="Save"
        Style="{StaticResource AccentButtonStyle}"
        Click="Save_Click" />

<!-- Subtle (low-emphasis) -->
<Button Content="Cancel"
        Style="{StaticResource SubtleButtonStyle}"
        Click="Cancel_Click" />
```

## Button with icon content

```xaml
<Button AutomationProperties.Name="Add item" Click="Add_Click">
    <StackPanel Orientation="Horizontal" Spacing="8">
        <FontIcon Glyph="&#xE710;" FontSize="16" />
        <TextBlock Text="Add" />
    </StackPanel>
</Button>
```

## Disable / enable via binding (MVVM)

```xaml
<Button Content="Submit"
        IsEnabled="{x:Bind ViewModel.CanSubmit, Mode=OneWay}"
        Command="{x:Bind ViewModel.SubmitCommand}" />
```

```csharp
// ViewModel (CommunityToolkit.Mvvm)
[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(SubmitCommand))]
private bool _canSubmit;

[RelayCommand(CanExecute = nameof(CanSubmit))]
private void Submit() { /* … */ }
```

## Wrapping long content

```xaml
<Button HorizontalAlignment="Stretch" MaxWidth="300">
    <TextBlock Text="This is a long label that wraps nicely"
               TextWrapping="WrapWholeWords" />
</Button>
```

## Notes
- Prefer `Command` + `RelayCommand` over Click handlers for MVVM.
- Always set `AutomationProperties.Name` when the button has no text content (e.g. icon-only).
- Use `AccentButtonStyle` for the single primary action per screen.
- `RepeatButton` fires continuously while held; `ToggleButton` has on/off state.
