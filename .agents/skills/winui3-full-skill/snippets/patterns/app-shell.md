# App Patterns: Full App Shell

A complete WinUI 3 app shell with:
- `NavigationView` (left pane) + `Frame` for page navigation
- Mica system backdrop
- Custom title bar
- Back navigation wired up
- Selected nav item synced to current page

---

## MainWindow.xaml

```xaml
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    Title="MyApp">
    <Grid>
        <local:ShellPage />
    </Grid>
</Window>
```

---

## MainWindow.xaml.cs

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Apply Mica backdrop
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop
            {
                Kind = MicaKind.BaseAlt
            };
        }

        // Extend content into title bar
        ExtendsContentIntoTitleBar = true;
    }
}
```

---

## ShellPage.xaml

```xaml
<Page
    x:Class="MyApp.Views.ShellPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:local="using:MyApp.Views">

    <Grid>
        <Grid.RowDefinitions>
            <!-- Custom title bar drag region -->
            <RowDefinition Height="48" />
            <!-- Main content -->
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <!-- Title bar -->
        <Grid
            x:Name="AppTitleBar"
            Grid.Row="0"
            Padding="16,0">
            <StackPanel Orientation="Horizontal" VerticalAlignment="Center" Spacing="8">
                <Image Source="/Assets/Square44x44Logo.png" Width="20" Height="20" />
                <TextBlock Text="MyApp" Style="{ThemeResource CaptionTextBlockStyle}"
                           VerticalAlignment="Center" />
            </StackPanel>
        </Grid>

        <!-- Navigation -->
        <NavigationView
            Grid.Row="1"
            x:Name="NavView"
            IsBackButtonVisible="Visible"
            IsBackEnabled="{x:Bind ContentFrame.CanGoBack, Mode=OneWay}"
            BackRequested="NavView_BackRequested"
            ItemInvoked="NavView_ItemInvoked"
            Loaded="NavView_Loaded"
            PaneDisplayMode="Auto">

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
    </Grid>
</Page>
```

---

## ShellPage.xaml.cs

```csharp
// Views/ShellPage.xaml.cs
using MyApp.Services;
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

        // Register the frame with the navigation service
        var navService = App.Services.GetRequiredService<INavigationService>();
        navService.SetFrame(ContentFrame);
    }

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        ContentFrame.Navigated += ContentFrame_Navigated;
        Navigate("HomePage");

        // Set title bar drag region
        if (XamlRoot?.Content is FrameworkElement root)
        {
            var window = (App.Current as App)?.MainWindow;
            window?.SetTitleBar(AppTitleBar);
        }
    }

    private void NavView_ItemInvoked(NavigationView sender,
        NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            Navigate("SettingsPage");
            return;
        }
        if (args.InvokedItemContainer?.Tag is string tag)
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
        NavView.IsBackEnabled = ContentFrame.CanGoBack;
        var pageType = e.SourcePageType;
        var tag = _pages.FirstOrDefault(p => p.Value == pageType).Key;

        var allItems = NavView.MenuItems
            .OfType<NavigationViewItem>()
            .Concat(NavView.FooterMenuItems.OfType<NavigationViewItem>());

        NavView.SelectedItem = allItems.FirstOrDefault(i => i.Tag?.ToString() == tag);
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

## App.xaml.cs (minimal)

```csharp
// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using MyApp.Services;

namespace MyApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public Window? MainWindow { get; private set; }

    public App()
    {
        InitializeComponent();
        var services = new ServiceCollection();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService>(sp =>
            new DialogService(() => MainWindow!));
        // register ViewModels...
        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        MainWindow.Activate();
    }
}
```

---

## Notes

- `SystemBackdrop = new MicaBackdrop()` requires Windows 11; check `MicaController.IsSupported()`.
- `ExtendsContentIntoTitleBar = true` removes the default title bar so you can draw custom content.
- Call `window.SetTitleBar(element)` to designate the drag region.
- Always sync `NavView.SelectedItem` in `ContentFrame.Navigated` so the pane highlights the
  correct item after programmatic or back navigation.
- The `PaneDisplayMode="Auto"` adapts between left/compact based on window width.
