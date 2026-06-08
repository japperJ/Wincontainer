# TreeView

`TreeView` displays hierarchical data in an expandable/collapsible tree structure.
Use it for file explorers, organizational charts, category trees, and nested settings.

---

## Basic TreeView (Code-Behind Population)

```xaml
<TreeView
    x:Name="FileTree"
    CanDragItems="True"
    AllowDrop="True"
    Loaded="FileTree_Loaded"
    AutomationProperties.Name="File tree" />
```

```csharp
// MainPage.xaml.cs
private void FileTree_Loaded(object sender, RoutedEventArgs e)
{
    var root = new TreeViewNode { Content = "Documents", IsExpanded = true };
    root.Children.Add(new TreeViewNode { Content = "Reports" });
    root.Children.Add(new TreeViewNode { Content = "Images" });

    var reportsNode = root.Children[0];
    reportsNode.Children.Add(new TreeViewNode { Content = "Q1.docx" });
    reportsNode.Children.Add(new TreeViewNode { Content = "Q2.docx" });

    FileTree.RootNodes.Add(root);
}
```

---

## Data-Bound TreeView with ItemsSource

```xaml
<TreeView
    x:Name="BoundTree"
    ItemsSource="{x:Bind ViewModel.RootItems, Mode=OneWay}"
    AutomationProperties.Name="Category tree">
    <TreeView.ItemTemplate>
        <DataTemplate x:DataType="local:TreeItem">
            <TreeViewItem
                Content="{x:Bind Name}"
                IsExpanded="{x:Bind IsExpanded, Mode=TwoWay}"
                ItemsSource="{x:Bind Children}" />
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

```csharp
// Models/TreeItem.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MyApp.Models;

public partial class TreeItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isExpanded;

    public ObservableCollection<TreeItem> Children { get; } = new();
}
```

---

## TreeView with Template Selector (Folders vs Files)

```xaml
<Page.Resources>
    <DataTemplate x:Key="FolderTemplate" x:DataType="local:ExplorerItem">
        <TreeViewItem
            IsExpanded="True"
            ItemsSource="{x:Bind Children}"
            AutomationProperties.Name="{x:Bind Name}">
            <StackPanel Orientation="Horizontal" Spacing="6">
                <FontIcon Glyph="&#xE8B7;" FontSize="16" />
                <TextBlock Text="{x:Bind Name}" />
            </StackPanel>
        </TreeViewItem>
    </DataTemplate>
    <DataTemplate x:Key="FileTemplate" x:DataType="local:ExplorerItem">
        <TreeViewItem AutomationProperties.Name="{x:Bind Name}">
            <StackPanel Orientation="Horizontal" Spacing="6">
                <FontIcon Glyph="&#xE8A5;" FontSize="16" />
                <TextBlock Text="{x:Bind Name}" />
            </StackPanel>
        </TreeViewItem>
    </DataTemplate>
    <local:ExplorerItemTemplateSelector
        x:Key="ItemSelector"
        FolderTemplate="{StaticResource FolderTemplate}"
        FileTemplate="{StaticResource FileTemplate}" />
</Page.Resources>

<TreeView
    ItemsSource="{x:Bind ViewModel.RootItems, Mode=OneWay}"
    ItemTemplateSelector="{StaticResource ItemSelector}"
    AutomationProperties.Name="Explorer tree" />
```

```csharp
// ExplorerItemTemplateSelector.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MyApp;

public class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
        => item is ExplorerItem { IsFolder: true } ? FolderTemplate : FileTemplate;
}
```

---

## Multi-Select TreeView

```xaml
<TreeView
    ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
    SelectionMode="Multiple"
    AutomationProperties.Name="Multi-select tree">
    <TreeView.ItemTemplate>
        <DataTemplate x:DataType="local:TreeItem">
            <TreeViewItem
                Content="{x:Bind Name}"
                ItemsSource="{x:Bind Children}" />
        </DataTemplate>
    </TreeView.ItemTemplate>
</TreeView>
```

---

## Notes

- When using `ItemsSource`, each item's `DataTemplate` should return a `TreeViewItem` as
  the root element with `ItemsSource` bound to the children collection.
- `SelectionMode` can be `None`, `Single` (default), or `Multiple`.
- `CanDragItems="True"` + `AllowDrop="True"` enables drag-and-drop reordering.
- Use `ItemTemplateSelector` when different node types need different visual templates.
- For large trees, lazy-load children: populate the `Children` collection only when a
  node's `IsExpanded` changes to `true`.
