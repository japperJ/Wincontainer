# ListBox

`ListBox` is a simple selection list without the virtualization and advanced features of
`ListView`. Use it for small, static option lists where the user picks one or more items.
For large or dynamic collections, prefer `ListView`.

---

## Basic ListBox

```xaml
<ListBox
    AutomationProperties.Name="Color selection"
    SelectionMode="Single">
    <ListBoxItem Content="Red" />
    <ListBoxItem Content="Green" />
    <ListBoxItem Content="Blue" />
    <ListBoxItem Content="Yellow" />
</ListBox>
```

---

## Bound ListBox

```xaml
<ListBox
    ItemsSource="{x:Bind ViewModel.Options, Mode=OneWay}"
    SelectedItem="{x:Bind ViewModel.SelectedOption, Mode=TwoWay}"
    SelectionMode="Single"
    AutomationProperties.Name="Options list">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="local:Option">
            <TextBlock Text="{x:Bind Label}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

```csharp
// ViewModels/PickerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class PickerViewModel : ObservableObject
{
    [ObservableProperty]
    private Option? _selectedOption;

    public List<Option> Options { get; } = new()
    {
        new Option { Label = "Option A" },
        new Option { Label = "Option B" },
        new Option { Label = "Option C" },
    };
}
```

---

## Multi-Select ListBox

```xaml
<ListBox
    ItemsSource="{x:Bind ViewModel.Tags, Mode=OneWay}"
    SelectionMode="Multiple"
    SelectionChanged="Tags_SelectionChanged"
    AutomationProperties.Name="Tag selection">
    <ListBox.ItemTemplate>
        <DataTemplate x:DataType="x:String">
            <TextBlock Text="{x:Bind}" />
        </DataTemplate>
    </ListBox.ItemTemplate>
</ListBox>
```

```csharp
// MainPage.xaml.cs
private void Tags_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    var selected = ((ListBox)sender).SelectedItems.Cast<string>().ToList();
    ViewModel.SelectedTags = selected;
}
```

---

## Notes

- `ListBox` does **not** virtualize — avoid it for large datasets; use `ListView` instead.
- `SelectionMode`: `Single`, `Multiple`, `Extended` (shift/ctrl click range selection).
- Unlike `ListView`, `ListBox` does not have `IsItemClickEnabled` — handle `SelectionChanged`.
- For grouped or grouped-filterable lists, use `ListView` with `CollectionViewSource`.
