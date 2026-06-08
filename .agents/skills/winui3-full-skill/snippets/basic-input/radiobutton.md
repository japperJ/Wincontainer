# RadioButton / RadioButtons

**Category:** Basic Input  
**Namespace:** `Microsoft.UI.Xaml.Controls`

`RadioButton` – single control, belongs to a named group.  
`RadioButtons` – panel that manages a group automatically; preferred for MVVM.

## RadioButtons (recommended – handles grouping automatically)

```xaml
<RadioButtons Header="Alignment"
              SelectedIndex="0"
              SelectionChanged="Alignment_SelectionChanged">
    <x:String>Left</x:String>
    <x:String>Center</x:String>
    <x:String>Right</x:String>
</RadioButtons>
```

## RadioButtons bound to collection

```xaml
<RadioButtons Header="Sort order"
              ItemsSource="{x:Bind ViewModel.SortOptions, Mode=OneWay}"
              SelectedItem="{x:Bind ViewModel.SelectedSort, Mode=TwoWay}"
              MaxColumns="2" />
```

```csharp
[ObservableProperty]
private ObservableCollection<string> _sortOptions = new() { "Name", "Date", "Size" };

[ObservableProperty]
private string _selectedSort = "Name";
```

## RadioButton (manual grouping)

```xaml
<StackPanel>
    <RadioButton Content="Option A" GroupName="options" IsChecked="True" />
    <RadioButton Content="Option B" GroupName="options" />
    <RadioButton Content="Option C" GroupName="options" />
</StackPanel>
```

## RadioButton in layout (inline)

```xaml
<StackPanel Orientation="Horizontal" Spacing="16">
    <RadioButton Content="Monthly" GroupName="billing" />
    <RadioButton Content="Annually" GroupName="billing" IsChecked="True" />
</StackPanel>
```

## Notes
- Prefer `RadioButtons` over manual `RadioButton` grouping — it handles keyboard navigation and accessibility automatically.
- `RadioButtons.MaxColumns` creates a multi-column grid layout.
- `RadioButtons` is in the `Microsoft.UI.Xaml.Controls` namespace (WinUI, not built-in).
- For 2 mutually exclusive states, consider `ToggleSwitch` instead.
