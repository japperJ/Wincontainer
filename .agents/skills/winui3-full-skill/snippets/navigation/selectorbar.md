# SelectorBar

`SelectorBar` is a compact horizontal filter/tab bar introduced in Windows App SDK 1.4.
Use it for category filters, view toggles, or content switchers within a page —
not for top-level navigation (use `NavigationView` for that).

---

## Basic SelectorBar

```xaml
<SelectorBar
    x:Name="FilterBar"
    SelectionChanged="FilterBar_SelectionChanged"
    AutomationProperties.Name="Content filter">
    <SelectorBarItem Text="All" Tag="All" />
    <SelectorBarItem Text="Active" Tag="Active" />
    <SelectorBarItem Text="Completed" Tag="Completed" />
    <SelectorBarItem Text="Archived" Tag="Archived" />
</SelectorBar>
```

```csharp
// MainPage.xaml.cs
private void FilterBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
{
    var tag = sender.SelectedItem?.Tag?.ToString() ?? "All";
    ViewModel.ApplyFilter(tag);
}
```

---

## With Icons

```xaml
<SelectorBar AutomationProperties.Name="View mode">
    <SelectorBarItem Text="Grid">
        <SelectorBarItem.Icon>
            <SymbolIcon Symbol="ViewAll" />
        </SelectorBarItem.Icon>
    </SelectorBarItem>
    <SelectorBarItem Text="List">
        <SelectorBarItem.Icon>
            <SymbolIcon Symbol="List" />
        </SelectorBarItem.Icon>
    </SelectorBarItem>
    <SelectorBarItem Text="Details">
        <SelectorBarItem.Icon>
            <SymbolIcon Symbol="DockBottom" />
        </SelectorBarItem.Icon>
    </SelectorBarItem>
</SelectorBar>
```

---

## MVVM — Bound SelectedItem

```xaml
<!-- View -->
<StackPanel Spacing="12">
    <SelectorBar
        x:Name="CategoryBar"
        SelectionChanged="CategoryBar_SelectionChanged"
        AutomationProperties.Name="Category">
        <SelectorBarItem Text="All" Tag="All" />
        <SelectorBarItem Text="News" Tag="News" />
        <SelectorBarItem Text="Sport" Tag="Sport" />
        <SelectorBarItem Text="Tech" Tag="Tech" />
    </SelectorBar>

    <ListView
        ItemsSource="{x:Bind ViewModel.FilteredItems, Mode=OneWay}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="local:Article">
                <TextBlock Text="{x:Bind Title}" />
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private void CategoryBar_SelectionChanged(SelectorBar sender,
    SelectorBarSelectionChangedEventArgs args)
{
    ViewModel.ActiveCategory = sender.SelectedItem?.Tag?.ToString() ?? "All";
}
```

```csharp
// ViewModels/FeedViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class FeedViewModel : ObservableObject
{
    private readonly List<Article> _allArticles = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilteredItems))]
    private string _activeCategory = "All";

    public IEnumerable<Article> FilteredItems =>
        ActiveCategory == "All"
            ? _allArticles
            : _allArticles.Where(a => a.Category == ActiveCategory);
}
```

---

## Notes

- `SelectorBar` requires Windows App SDK 1.4+; confirm `<PackageReference>` version.
- `SelectedItem` is read-only in XAML — use `SelectionChanged` to sync to the ViewModel.
- `SelectorBarItem.Tag` is the recommended way to store the filter key.
- For top-level navigation between pages, use `NavigationView` instead.
- `SelectorBar` is similar to `Pivot` but designed for desktop filter scenarios, not
  swipe navigation.
