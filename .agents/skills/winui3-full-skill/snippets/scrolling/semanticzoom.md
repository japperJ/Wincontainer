# SemanticZoom

A container that hosts two views of the same data — a zoomed-in detail view and a zoomed-out overview (group index). The user can pinch-to-zoom or click a group header to toggle between views. Ideal for alphabetically or categorically grouped lists.

---

## Basic Usage

```xaml
<Page.Resources>
    <CollectionViewSource
        x:Name="cvsGroups"
        IsSourceGrouped="True"
        ItemsPath="Items"
        Source="{x:Bind Groups}" />
</Page.Resources>

<SemanticZoom Height="500">
    <SemanticZoom.ZoomedInView>
        <GridView
            ItemsSource="{x:Bind cvsGroups.View}"
            SelectionMode="None"
            ItemTemplate="{StaticResource ZoomedInTemplate}">
            <GridView.GroupStyle>
                <GroupStyle HeaderTemplate="{StaticResource ZoomedInGroupHeaderTemplate}" />
            </GridView.GroupStyle>
        </GridView>
    </SemanticZoom.ZoomedInView>

    <SemanticZoom.ZoomedOutView>
        <ListView
            ItemsSource="{x:Bind cvsGroups.View.CollectionGroups}"
            SelectionMode="None"
            ItemTemplate="{StaticResource ZoomedOutTemplate}" />
    </SemanticZoom.ZoomedOutView>
</SemanticZoom>
```

---

## Data Templates

```xaml
<Page.Resources>
    <!-- Zoomed-in: show item detail -->
    <DataTemplate x:Key="ZoomedInTemplate" x:DataType="local:ContactItem">
        <StackPanel MinWidth="200" Margin="12,6">
            <TextBlock Style="{StaticResource BaseTextBlockStyle}" Text="{x:Bind Name}" />
            <TextBlock Style="{StaticResource BodyTextBlockStyle}" Text="{x:Bind Phone}" />
        </StackPanel>
    </DataTemplate>

    <!-- Zoomed-in group header -->
    <DataTemplate x:Key="ZoomedInGroupHeaderTemplate" x:DataType="local:ContactGroup">
        <TextBlock
            Style="{StaticResource SubtitleTextBlockStyle}"
            Foreground="{ThemeResource ApplicationForegroundThemeBrush}"
            Text="{x:Bind Key}" />
    </DataTemplate>

    <!-- Zoomed-out: show group letter/key -->
    <DataTemplate x:Key="ZoomedOutTemplate"
                  x:DataType="Microsoft.UI.Xaml.Data:ICollectionViewGroup">
        <TextBlock
            Style="{StaticResource SubtitleTextBlockStyle}"
            Text="{x:Bind ((local:ContactGroup)Group).Key}"
            TextWrapping="Wrap" />
    </DataTemplate>
</Page.Resources>
```

---

## ViewModel with Grouped Data

```csharp
// ViewModels/ContactsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Linq;

namespace MyApp.ViewModels;

public record ContactItem(string Name, string Phone);
public record ContactGroup(string Key, IEnumerable<ContactItem> Items);

public partial class ContactsViewModel : ObservableObject
{
    public ObservableCollection<ContactGroup> Groups { get; } = new();

    public ContactsViewModel()
    {
        var contacts = new[]
        {
            new ContactItem("Alice Anderson", "555-0100"),
            new ContactItem("Bob Baker", "555-0101"),
            new ContactItem("Carol Clark", "555-0102"),
            new ContactItem("David Davis", "555-0103"),
        };

        var grouped = contacts
            .GroupBy(c => c.Name[0].ToString().ToUpper())
            .OrderBy(g => g.Key)
            .Select(g => new ContactGroup(g.Key, g.ToList()));

        foreach (var group in grouped)
            Groups.Add(group);
    }
}
```

---

## Code-Behind — Ensure Focus Returns on Zoom-In

```csharp
// Views/ContactsPage.xaml.cs
private void List_GotFocus(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
{
    // Collapse the zoomed-out view when the zoomed-in list receives focus
    SemanticZoomControl.IsZoomedInViewActive = true;
}
```

---

## Notes

- `ZoomedInView` must implement `ISemanticZoomInformation` — `GridView` and `ListView` both do.
- `ZoomedOutView` typically binds to `cvsGroups.View.CollectionGroups` (the group keys), not the full item list.
- Use a `CollectionViewSource` with `IsSourceGrouped="True"` to bridge grouped data to both views.
- The zoomed-out `ListView`/`GridView`'s `ItemTemplate` must use `x:DataType="Microsoft.UI.Xaml.Data:ICollectionViewGroup"` and cast `Group` to your group type.
- `IsZoomedInViewActive` can be set programmatically to switch views.
- Pinch-to-zoom only works on touch devices; provide a clickable group header as the primary keyboard/mouse interaction.
