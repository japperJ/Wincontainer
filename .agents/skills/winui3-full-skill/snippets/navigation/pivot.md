# Pivot

`Pivot` provides tab-like navigation with swipeable content panels.
In WinUI 3, `Pivot` is primarily intended for **mobile / touch** scenarios.
For desktop apps, prefer `TabView` (document tabs) or `NavigationView` (top bar).

---

## Basic Pivot

```xaml
<Pivot AutomationProperties.Name="Content sections">
    <PivotItem Header="All">
        <ListView ItemsSource="{x:Bind ViewModel.AllItems, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:Item">
                    <TextBlock Text="{x:Bind Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </PivotItem>
    <PivotItem Header="Favorites">
        <ListView ItemsSource="{x:Bind ViewModel.FavoriteItems, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:Item">
                    <TextBlock Text="{x:Bind Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </PivotItem>
    <PivotItem Header="Recent">
        <ListView ItemsSource="{x:Bind ViewModel.RecentItems, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="local:Item">
                    <TextBlock Text="{x:Bind Name}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </PivotItem>
</Pivot>
```

---

## Pivot with SelectedIndex Binding

```xaml
<Pivot
    SelectedIndex="{x:Bind ViewModel.ActiveTab, Mode=TwoWay}"
    SelectionChanged="Pivot_SelectionChanged"
    AutomationProperties.Name="Sections">
    <PivotItem Header="Overview">
        <local:OverviewSection />
    </PivotItem>
    <PivotItem Header="Details">
        <local:DetailsSection />
    </PivotItem>
    <PivotItem Header="Activity">
        <local:ActivitySection />
    </PivotItem>
</Pivot>
```

```csharp
// ViewModels/ProfileViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ProfileViewModel : ObservableObject
{
    [ObservableProperty]
    private int _activeTab = 0;
}
```

---

## Notes

- On desktop, `Pivot` headers are small and not optimized for mouse interaction.
  Use `NavigationView` (top mode) or `TabView` for desktop navigation patterns.
- `Pivot` supports swipe navigation on touch devices automatically.
- `PivotItem.Header` accepts any object — put an icon + text `StackPanel` for richer headers.
- `SelectionChanged` fires when the user swipes or taps a header.
