# CheckBox

**Category:** Basic Input  
**Namespace:** `Microsoft.UI.Xaml.Controls`

Three-state selection control. Use for independent on/off settings.

## Basic checkbox

```xaml
<CheckBox Content="Enable notifications"
          AutomationProperties.Name="Enable notifications"
          IsChecked="{x:Bind ViewModel.NotificationsEnabled, Mode=TwoWay}" />
```

## Three-state checkbox (indeterminate)

```xaml
<CheckBox x:Name="SelectAllCheckBox"
          Content="Select all"
          IsThreeState="True"
          Checked="SelectAll_Checked"
          Unchecked="SelectAll_Unchecked"
          Indeterminate="SelectAll_Indeterminate" />
```

```csharp
private void SelectAll_Checked(object sender, RoutedEventArgs e)
{
    foreach (var item in Items)
        item.IsSelected = true;
}

private void SelectAll_Unchecked(object sender, RoutedEventArgs e)
{
    foreach (var item in Items)
        item.IsSelected = false;
}
```

## CheckBox in a DataTemplate (ListView item)

```xaml
<DataTemplate x:DataType="local:TaskItem">
    <CheckBox Content="{x:Bind Title}"
              IsChecked="{x:Bind IsCompleted, Mode=TwoWay}" />
</DataTemplate>
```

## MVVM binding

```xaml
<CheckBox Content="Dark mode"
          IsChecked="{x:Bind ViewModel.IsDarkMode, Mode=TwoWay}" />
```

```csharp
[ObservableProperty]
private bool _isDarkMode;
```

## Notes
- `IsChecked` is `bool?` (nullable) to support the indeterminate state.
- For a group of related options where only one can be selected, use `RadioButton` instead.
- `CheckBox.IsChecked.Value` safely unwraps the nullable bool when indeterminate is not used.
