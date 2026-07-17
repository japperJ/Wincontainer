# BreadcrumbBar

`BreadcrumbBar` shows the user's current location in a hierarchy and lets them navigate
to any ancestor level. Use it for file explorers, settings hierarchies, and category drills.

---

## Basic BreadcrumbBar (String Items)

```xaml
<BreadcrumbBar
    x:Name="PathBar"
    AutomationProperties.Name="Navigation path" />
```

```csharp
// MainPage.xaml.cs — set path programmatically
PathBar.ItemsSource = new string[] { "Home", "Documents", "Projects", "MyApp" };
PathBar.ItemClicked += PathBar_ItemClicked;

private void PathBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
{
    // args.Index is the 0-based index of the clicked crumb
    // Trim the path back to that level
    var items = (string[])sender.ItemsSource;
    sender.ItemsSource = items[..(args.Index + 1)];
}
```

---

## Bound to ViewModel with Custom DataTemplate

```xaml
<BreadcrumbBar
    ItemsSource="{x:Bind ViewModel.Path, Mode=OneWay}"
    ItemClicked="Breadcrumb_ItemClicked"
    AutomationProperties.Name="Folder path">
    <BreadcrumbBar.ItemTemplate>
        <DataTemplate x:DataType="local:Folder">
            <BreadcrumbBarItem>
                <BreadcrumbBarItem.ContentTemplate>
                    <DataTemplate x:DataType="local:Folder">
                        <StackPanel Orientation="Horizontal" Spacing="4">
                            <FontIcon Glyph="&#xE8B7;" FontSize="14" />
                            <TextBlock
                                Text="{x:Bind Name}"
                                AutomationProperties.Name="{x:Bind Name}" />
                        </StackPanel>
                    </DataTemplate>
                </BreadcrumbBarItem.ContentTemplate>
            </BreadcrumbBarItem>
        </DataTemplate>
    </BreadcrumbBar.ItemTemplate>
</BreadcrumbBar>
```

```csharp
// MainPage.xaml.cs
private void Breadcrumb_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
{
    ViewModel.NavigateTo(args.Index);
}
```

```csharp
// ViewModels/FileExplorerViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class FileExplorerViewModel : ObservableObject
{
    public ObservableCollection<Folder> Path { get; } = new()
    {
        new Folder { Name = "Home" }
    };

    public void NavigateTo(int index)
    {
        // Remove all crumbs after the clicked one
        while (Path.Count > index + 1)
            Path.RemoveAt(Path.Count - 1);
    }

    public void NavigateInto(Folder folder)
    {
        Path.Add(folder);
        // Load folder contents
    }
}
```

---

## Notes

- When a breadcrumb is clicked, **you** are responsible for truncating the path back to
  that level (see `NavigateTo` above).
- `BreadcrumbBar` automatically shows a chevron overflow menu when items overflow the
  available width.
- Items can be strings, or any object — use `ItemTemplate` for rich item display.
- Always set `AutomationProperties.Name` on the control and on each item for accessibility.
- For top-level navigation, use `NavigationView` instead; `BreadcrumbBar` is for
  hierarchical drill-down within a single page or section.
