# NavigationView

`NavigationView` is the primary top-level navigation control in WinUI 3.
It provides a collapsible left pane (or top bar) with nav items, a back button, and a content area.

---

## App Shell with NavigationView + Frame

```xaml
<!-- Views/ShellPage.xaml -->
<Page
    x:Class="MyApp.Views.ShellPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <NavigationView
        x:Name="NavView"
        IsBackButtonVisible="Visible"
        IsBackEnabled="{x:Bind ContentFrame.CanGoBack, Mode=OneWay}"
        BackRequested="NavView_BackRequested"
        ItemInvoked="NavView_ItemInvoked"
        Loaded="NavView_Loaded">

        <NavigationView.MenuItems>
            <NavigationViewItem
                Content="Home"
                Tag="HomePage"
                AutomationProperties.Name="Home">
                <NavigationViewItem.Icon>
                    <SymbolIcon Symbol="Home" />
                </NavigationViewItem.Icon>
            </NavigationViewItem>
            <NavigationViewItem
                Content="Browse"
                Tag="BrowsePage"
                AutomationProperties.Name="Browse">
                <NavigationViewItem.Icon>
                    <SymbolIcon Symbol="Library" />
                </NavigationViewItem.Icon>
            </NavigationViewItem>
            <NavigationViewItemSeparator />
            <NavigationViewItem
                Content="Favorites"
                Tag="FavoritesPage"
                AutomationProperties.Name="Favorites">
                <NavigationViewItem.Icon>
                    <SymbolIcon Symbol="Favorite" />
                </NavigationViewItem.Icon>
            </NavigationViewItem>
        </NavigationView.MenuItems>

        <NavigationView.FooterMenuItems>
            <NavigationViewItem
                Content="Settings"
                Tag="SettingsPage"
                AutomationProperties.Name="Settings">
                <NavigationViewItem.Icon>
                    <SymbolIcon Symbol="Setting" />
                </NavigationViewItem.Icon>
            </NavigationViewItem>
        </NavigationView.FooterMenuItems>

        <Frame x:Name="ContentFrame" />
    </NavigationView>
</Page>
```

```csharp
// Views/ShellPage.xaml.cs
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MyApp.Views;

public sealed partial class ShellPage : Page
{
    private static readonly Dictionary<string, Type> _pages = new()
    {
        { "HomePage",      typeof(HomePage) },
        { "BrowsePage",    typeof(BrowsePage) },
        { "FavoritesPage", typeof(FavoritesPage) },
        { "SettingsPage",  typeof(SettingsPage) },
    };

    public ShellPage()
    {
        InitializeComponent();
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigated += ContentFrame_Navigated;
        Navigate("HomePage");
    }

    private void NavView_ItemInvoked(NavigationView sender,
        NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
            Navigate("SettingsPage");
        else if (args.InvokedItemContainer?.Tag is string tag)
            Navigate(tag);
    }

    private void NavView_BackRequested(NavigationView sender,
        NavigationViewBackRequestedEventArgs args)
    {
        if (ContentFrame.CanGoBack)
            ContentFrame.GoBack();
    }

    private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
    {
        // Sync the selected nav item with the current page
        var pageType = e.SourcePageType;
        var tag = _pages.FirstOrDefault(p => p.Value == pageType).Key;
        NavView.SelectedItem = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .Concat(NavView.FooterMenuItems.OfType<NavigationViewItem>())
            .FirstOrDefault(i => i.Tag?.ToString() == tag);
    }

    private void Navigate(string tag)
    {
        if (_pages.TryGetValue(tag, out var pageType) &&
            ContentFrame.CurrentSourcePageType != pageType)
        {
            ContentFrame.Navigate(pageType);
        }
    }
}
```

---

## Top Navigation Mode

```xaml
<NavigationView
    PaneDisplayMode="Top"
    IsBackButtonVisible="Collapsed"
    AutomationProperties.Name="Top navigation">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Tag="HomePage" />
        <NavigationViewItem Content="Products" Tag="ProductsPage" />
        <NavigationViewItem Content="About" Tag="AboutPage" />
    </NavigationView.MenuItems>
    <Frame x:Name="TopFrame" />
</NavigationView>
```

---

## Hierarchical Navigation Items

```xaml
<NavigationViewItem Content="Documents" AutomationProperties.Name="Documents">
    <NavigationViewItem.Icon>
        <SymbolIcon Symbol="Document" />
    </NavigationViewItem.Icon>
    <NavigationViewItem.MenuItems>
        <NavigationViewItem Content="Recent" Tag="RecentPage" />
        <NavigationViewItem Content="Shared" Tag="SharedPage" />
        <NavigationViewItem Content="Archive" Tag="ArchivePage" />
    </NavigationViewItem.MenuItems>
</NavigationViewItem>
```

---

## Notes

- Always provide `AutomationProperties.Name` on `NavigationViewItem` for accessibility.
- `IsBackButtonVisible="Visible"` + `IsBackEnabled` bound to `Frame.CanGoBack` is the
  standard pattern for back-navigation.
- Handle `ContentFrame.Navigated` to keep the selected nav item in sync with the Frame.
- Use `PaneDisplayMode` to switch between `Left` (default), `Top`, `LeftCompact`,
  `LeftMinimal`, or `Auto` (responsive).
- Do **not** use `Pivot` or `TabView` for top-level navigation; `NavigationView` is the
  WinUI 3 standard.
- For a full shell with Mica backdrop, see `snippets/patterns/app-shell.md`.
