# ComboBox

**Category:** Basic Input  
**Namespace:** `Microsoft.UI.Xaml.Controls`

A drop-down list that lets the user select one item. Use when space is limited and 
there are 4+ options.

## Static items

```xaml
<ComboBox Header="Theme"
          SelectedIndex="0"
          SelectionChanged="Theme_SelectionChanged">
    <x:String>Light</x:String>
    <x:String>Dark</x:String>
    <x:String>System default</x:String>
</ComboBox>
```

## Bound to an ObservableCollection (MVVM)

```xaml
<ComboBox Header="Category"
          ItemsSource="{x:Bind ViewModel.Categories, Mode=OneWay}"
          SelectedItem="{x:Bind ViewModel.SelectedCategory, Mode=TwoWay}"
          DisplayMemberPath="Name"
          PlaceholderText="Pick a category" />
```

```csharp
[ObservableProperty]
private ObservableCollection<Category> _categories = new();

[ObservableProperty]
private Category? _selectedCategory;
```

## Custom item template

```xaml
<ComboBox Header="Color"
          ItemsSource="{x:Bind ViewModel.Colors, Mode=OneWay}"
          SelectedItem="{x:Bind ViewModel.SelectedColor, Mode=TwoWay}">
    <ComboBox.ItemTemplate>
        <DataTemplate x:DataType="local:ColorItem">
            <StackPanel Orientation="Horizontal" Spacing="8">
                <Rectangle Width="16" Height="16"
                           Fill="{x:Bind Brush}" />
                <TextBlock Text="{x:Bind Name}" />
            </StackPanel>
        </DataTemplate>
    </ComboBox.ItemTemplate>
</ComboBox>
```

## Editable ComboBox (free-text + suggestions)

```xaml
<ComboBox Header="Font"
          IsEditable="True"
          Text="{x:Bind ViewModel.FontName, Mode=TwoWay}"
          ItemsSource="{x:Bind ViewModel.FontNames, Mode=OneWay}" />
```

## Notes
- Always set `Header` for screen-reader accessibility.
- Use `PlaceholderText` when no default selection exists.
- For free-text with auto-suggest, prefer `AutoSuggestBox` over `IsEditable="True"`.
- `SelectedIndex`, `SelectedItem`, and `SelectedValue` are mutually usable; prefer `SelectedItem` when binding to typed objects.
