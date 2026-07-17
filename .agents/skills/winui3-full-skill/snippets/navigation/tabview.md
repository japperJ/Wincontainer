# TabView

`TabView` is a multi-tab container similar to a browser or code editor tab strip.
Each `TabViewItem` holds its own content and can be closable. Use it for document editors,
terminal emulators, settings categories, or any multi-document interface.

---

## Basic TabView with Static Tabs

```xaml
<TabView
    SelectedIndex="0"
    AddTabButtonClick="TabView_AddButtonClick"
    TabCloseRequested="TabView_TabCloseRequested"
    AutomationProperties.Name="Document tabs">
    <TabView.TabItems>
        <TabViewItem Header="Document 1">
            <TabViewItem.IconSource>
                <SymbolIconSource Symbol="Document" />
            </TabViewItem.IconSource>
            <!-- Tab content -->
            <local:DocumentPage1 />
        </TabViewItem>
        <TabViewItem Header="Document 2">
            <TabViewItem.IconSource>
                <SymbolIconSource Symbol="Document" />
            </TabViewItem.IconSource>
            <local:DocumentPage2 />
        </TabViewItem>
        <TabViewItem Header="Settings" IsClosable="False">
            <TabViewItem.IconSource>
                <SymbolIconSource Symbol="Setting" />
            </TabViewItem.IconSource>
            <local:SettingsPage />
        </TabViewItem>
    </TabView.TabItems>
</TabView>
```

```csharp
// MainPage.xaml.cs
private void TabView_AddButtonClick(TabView sender, object args)
{
    sender.TabItems.Add(CreateTab($"Document {sender.TabItems.Count + 1}"));
}

private void TabView_TabCloseRequested(TabView sender, TabViewTabCloseRequestedEventArgs args)
{
    sender.TabItems.Remove(args.Tab);
}

private static TabViewItem CreateTab(string header) => new()
{
    Header = header,
    IconSource = new SymbolIconSource { Symbol = Symbol.Document },
    Content = new TextBlock { Text = $"Content for {header}" }
};
```

---

## Data-Bound TabView

```xaml
<TabView
    TabItemsSource="{x:Bind ViewModel.Documents, Mode=OneWay}"
    AddTabButtonClick="TabView_AddButtonClick"
    TabCloseRequested="TabView_TabCloseRequested"
    AutomationProperties.Name="Editor tabs">
    <TabView.TabItemTemplate>
        <DataTemplate x:DataType="local:DocumentModel">
            <TabViewItem
                Header="{x:Bind Title}"
                Content="{x:Bind Content}">
                <TabViewItem.IconSource>
                    <SymbolIconSource Symbol="Document" />
                </TabViewItem.IconSource>
            </TabViewItem>
        </DataTemplate>
    </TabView.TabItemTemplate>
</TabView>
```

```csharp
// ViewModels/EditorViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class EditorViewModel : ObservableObject
{
    public ObservableCollection<DocumentModel> Documents { get; } = new();

    [RelayCommand]
    private void AddDocument()
    {
        Documents.Add(new DocumentModel
        {
            Title = $"Untitled {Documents.Count + 1}",
            Content = new TextBlock { Text = "New document" }
        });
    }

    public void CloseDocument(DocumentModel doc) => Documents.Remove(doc);
}
```

---

## Keyboard Shortcuts

```xaml
<TabView
    AddTabButtonClick="TabView_AddButtonClick"
    TabCloseRequested="TabView_TabCloseRequested"
    Loaded="TabView_Loaded">
    <TabView.KeyboardAccelerators>
        <KeyboardAccelerator Key="T" Modifiers="Control"
            Invoked="NewTab_Invoked" />
        <KeyboardAccelerator Key="W" Modifiers="Control"
            Invoked="CloseTab_Invoked" />
        <KeyboardAccelerator Key="Number1" Modifiers="Control"
            Invoked="GoToTab_Invoked" />
    </TabView.KeyboardAccelerators>
</TabView>
```

```csharp
// MainPage.xaml.cs
private void NewTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
{
    TabView1.TabItems.Add(CreateTab($"Tab {TabView1.TabItems.Count + 1}"));
    args.Handled = true;
}

private void CloseTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
{
    if (TabView1.SelectedItem is TabViewItem tab && tab.IsClosable)
        TabView1.TabItems.Remove(tab);
    args.Handled = true;
}

private void GoToTab_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
{
    // Navigate to tab index based on accelerator key
    args.Handled = true;
}
```

---

## Accent Colored Tab Strip

```xaml
<TabView>
    <TabView.Resources>
        <ResourceDictionary>
            <ResourceDictionary.ThemeDictionaries>
                <ResourceDictionary x:Key="Light">
                    <SolidColorBrush x:Key="TabViewBackground"
                        Color="{ThemeResource SystemAccentColorLight2}" />
                </ResourceDictionary>
                <ResourceDictionary x:Key="Dark">
                    <SolidColorBrush x:Key="TabViewBackground"
                        Color="{ThemeResource SystemAccentColorDark2}" />
                </ResourceDictionary>
            </ResourceDictionary.ThemeDictionaries>
        </ResourceDictionary>
    </TabView.Resources>
</TabView>
```

---

## Notes

- Always handle `TabCloseRequested` to remove the tab; the close button does not auto-remove.
- `IsClosable="False"` hides the close button on a specific tab (e.g., a pinned Settings tab).
- `TabWidthMode`: `Equal` (default), `SizeToContent`, or `Compact` (icon-only when unselected).
- `CloseButtonOverlayMode`: `Always`, `OnHover`, or `Auto`.
- For multi-window/tear-off tab scenarios, see the TabView windowing sample in the WinUI Gallery.
- `TabView` is not a navigation control — host a `Frame` inside each `TabViewItem` if you need
  per-tab page navigation.
