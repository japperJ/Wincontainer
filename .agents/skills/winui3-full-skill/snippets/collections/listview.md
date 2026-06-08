# ListView

`ListView` displays a vertical, scrollable list of items. It supports selection, data templates,
grouping, incremental loading, drag-and-drop, and pull-to-refresh.

---

## Basic Bound ListView

```xaml
<!-- View -->
<ListView
    ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
    SelectionMode="Single"
    SelectionChanged="List_SelectionChanged"
    AutomationProperties.Name="Items list">
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:MyItem">
            <StackPanel Padding="4" Spacing="2">
                <TextBlock Text="{x:Bind Title}" Style="{ThemeResource BodyStrongTextBlockStyle}" />
                <TextBlock Text="{x:Bind Subtitle}"
                           Style="{ThemeResource CaptionTextBlockStyle}"
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
            </StackPanel>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

```csharp
// MainPage.xaml.cs
private void List_SelectionChanged(object sender, SelectionChangedEventArgs e)
{
    if (e.AddedItems.FirstOrDefault() is MyItem selected)
        ViewModel.SelectedItem = selected;
}
```

---

## MVVM — Full ViewModel with ObservableCollection

```xaml
<!-- View -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <TextBlock
        Grid.Row="0"
        Text="{x:Bind ViewModel.StatusText, Mode=OneWay}"
        Style="{ThemeResource CaptionTextBlockStyle}"
        Margin="0,0,0,4" />

    <ListView
        Grid.Row="1"
        ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
        SelectedItem="{x:Bind ViewModel.SelectedItem, Mode=TwoWay}"
        AutomationProperties.Name="Contacts list">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="local:Contact">
                <Grid Padding="4" ColumnSpacing="12">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="Auto" />
                        <ColumnDefinition Width="*" />
                    </Grid.ColumnDefinitions>
                    <PersonPicture
                        Width="40"
                        Height="40"
                        DisplayName="{x:Bind Name}" />
                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="{x:Bind Name}"
                                   Style="{ThemeResource BodyStrongTextBlockStyle}" />
                        <TextBlock Text="{x:Bind Email}"
                                   Style="{ThemeResource CaptionTextBlockStyle}"
                                   Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                    </StackPanel>
                </Grid>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Grid>
```

```csharp
// ViewModels/ContactsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class ContactsViewModel : ObservableObject
{
    private readonly IContactService _contactService;

    [ObservableProperty]
    private Contact? _selectedItem;

    [ObservableProperty]
    private string _statusText = string.Empty;

    public ObservableCollection<Contact> Items { get; } = new();

    public ContactsViewModel(IContactService contactService)
    {
        _contactService = contactService;
    }

    [RelayCommand]
    public async Task LoadAsync(CancellationToken ct)
    {
        Items.Clear();
        StatusText = "Loading…";
        var contacts = await _contactService.GetContactsAsync(ct);
        foreach (var c in contacts)
            Items.Add(c);
        StatusText = $"{Items.Count} contacts";
    }
}
```

---

## Grouped ListView

```xaml
<ListView
    ItemsSource="{x:Bind ViewModel.GroupedItems, Mode=OneWay}"
    AutomationProperties.Name="Grouped list">
    <ListView.GroupStyle>
        <GroupStyle>
            <GroupStyle.HeaderTemplate>
                <DataTemplate x:DataType="local:ContactGroup">
                    <TextBlock
                        Text="{x:Bind GroupKey}"
                        Style="{ThemeResource SubtitleTextBlockStyle}"
                        Foreground="{ThemeResource AccentTextFillColorPrimaryBrush}" />
                </DataTemplate>
            </GroupStyle.HeaderTemplate>
        </GroupStyle>
    </ListView.GroupStyle>
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:Contact">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

```csharp
// ViewModels — create grouped source
using Microsoft.UI.Xaml.Data;

var grouped = contacts
    .GroupBy(c => c.Name[0].ToString().ToUpperInvariant())
    .OrderBy(g => g.Key)
    .Select(g => new ContactGroup(g.Key, g.ToList()));

var cvs = new CollectionViewSource
{
    Source = grouped,
    IsSourceGrouped = true,
    ItemsPath = new PropertyPath("Items")
};
GroupedItems = cvs.View;
```

---

## Multi-Select with SelectAll

```xaml
<StackPanel Spacing="4">
    <Button Content="Select all" Click="SelectAll_Click" />
    <ListView
        x:Name="MultiList"
        ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
        SelectionMode="Multiple"
        AutomationProperties.Name="Multi-select list">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="local:MyItem">
                <TextBlock Text="{x:Bind Title}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private void SelectAll_Click(object sender, RoutedEventArgs e) => MultiList.SelectAll();
```

---

## Notes

- Always use `ObservableCollection<T>` so the UI updates when items are added/removed.
- For large datasets, implement `ISupportIncrementalLoading` or use `AdvancedCollectionView`
  from CommunityToolkit.WinUI.
- `SelectionMode`: `None`, `Single`, `Multiple`, `Extended`.
- Use `x:DataType` on every `DataTemplate` for compiled bindings and IntelliSense.
- `CanDragItems="True"` + `AllowDrop="True"` + `CanReorderItems="True"` enables drag-to-reorder.
- For horizontal or wrapping collections, use `GridView` instead.
