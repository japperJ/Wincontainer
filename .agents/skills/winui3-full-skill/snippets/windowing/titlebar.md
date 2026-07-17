# TitleBar

The `TitleBar` control (Windows App SDK 1.6+) provides a ready-made, customisable title bar
that replaces the system default. It supports a title, subtitle, icon, back button, pane-toggle
button, a centred content area, and a right-side header slot.

For low-level title bar customisation without the `TitleBar` control (e.g., full
`AppWindowTitleBar` drag-region management), see the **AppWindow** snippet.

---

## Basic TitleBar control

```xaml
<!-- MainWindow.xaml -->
<Window
    x:Class="MyApp.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />   <!-- TitleBar -->
            <RowDefinition Height="*" />      <!-- Content -->
        </Grid.RowDefinitions>

        <TitleBar
            x:Name="titleBar"
            Title="My WinUI 3 App"
            Subtitle="Preview">
            <TitleBar.IconSource>
                <ImageIconSource ImageSource="/Assets/AppIcon.ico" />
            </TitleBar.IconSource>
            <!-- Centred content slot: search box, tabs, etc. -->
            <TitleBar.Content>
                <AutoSuggestBox
                    Width="360"
                    VerticalAlignment="Center"
                    PlaceholderText="Search…"
                    QueryIcon="Find" />
            </TitleBar.Content>
            <!-- Right-side header: user avatar, account button, etc. -->
            <TitleBar.RightHeader>
                <PersonPicture Width="32" Height="32" Initials="JD" />
            </TitleBar.RightHeader>
        </TitleBar>

        <!-- Main content area -->
        <Frame x:Name="contentFrame" Grid.Row="1" />
    </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Xaml;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Extend XAML content behind the system title bar area
        ExtendsContentIntoTitleBar = true;

        // Tell the shell which element is the custom title bar
        // (defines the drag region and passes through pointer events)
        SetTitleBar(titleBar);
    }
}
```

---

## Back button and pane-toggle integration with NavigationView

```xaml
<!-- MainWindow.xaml -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="Auto" />
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <TitleBar
        x:Name="titleBar"
        Title="My App"
        BackRequested="TitleBar_BackRequested"
        IsBackButtonVisible="{x:Bind navFrame.CanGoBack, Mode=OneWay}"
        IsPaneToggleButtonVisible="True"
        PaneToggleRequested="TitleBar_PaneToggleRequested" />

    <NavigationView
        x:Name="navView"
        Grid.Row="1"
        IsBackButtonVisible="Collapsed"
        IsPaneToggleButtonVisible="False"
        SelectionChanged="NavView_SelectionChanged">
        <NavigationView.MenuItems>
            <NavigationViewItem Content="Home"  Icon="Home"     Tag="home" />
            <NavigationViewItem Content="Settings" Icon="Setting" Tag="settings" />
        </NavigationView.MenuItems>
        <Frame x:Name="navFrame" />
    </NavigationView>
</Grid>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(titleBar);

        // Navigate to the default page
        navFrame.Navigate(typeof(HomePage));
    }

    private void TitleBar_BackRequested(TitleBar sender, object args)
    {
        if (navFrame.CanGoBack)
            navFrame.GoBack();
    }

    private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
    {
        navView.IsPaneOpen = !navView.IsPaneOpen;
    }

    private void NavView_SelectionChanged(NavigationView sender,
        NavigationViewSelectionChangedEventArgs args)
    {
        var tag = (args.SelectedItem as NavigationViewItem)?.Tag as string;
        var page = tag switch
        {
            "home"     => typeof(HomePage),
            "settings" => typeof(SettingsPage),
            _          => null
        };
        if (page is not null)
            navFrame.Navigate(page);
    }
}
```

---

## Low-level AppWindowTitleBar (full control)

Use `AppWindowTitleBar` directly when you need full control over caption button colours,
drag regions, or the fallback title bar (before SDK 1.6).

```xaml
<!-- MainWindow.xaml — content grid must include a custom title bar element -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="48" />   <!-- Custom title bar row -->
        <RowDefinition Height="*" />
    </Grid.RowDefinitions>

    <!-- Custom drag region — must be set as the title bar via SetTitleBar() -->
    <Grid
        x:Name="customTitleBar"
        Background="Transparent"
        VerticalAlignment="Stretch">
        <TextBlock
            Text="My App"
            VerticalAlignment="Center"
            Margin="16,0,0,0"
            Style="{StaticResource CaptionTextBlockStyle}" />
    </Grid>

    <Frame x:Name="contentFrame" Grid.Row="1" />
</Grid>
```

```csharp
// MainWindow.xaml.cs
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Windows.UI;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(customTitleBar);

        CustomiseTitleBarColors();
    }

    private void CustomiseTitleBarColors()
    {
        var titleBar = AppWindow.TitleBar;

        // Transparent caption buttons so XAML background shows through
        titleBar.ButtonBackgroundColor         = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

        // Adapt foreground to current theme
        var fg = ActualTheme == ElementTheme.Dark ? Colors.White : Colors.Black;
        titleBar.ButtonForegroundColor         = fg;
        titleBar.ButtonInactiveForegroundColor = fg;

        // Keep in sync with theme changes
        ActualThemeChanged += (_, _) => CustomiseTitleBarColors();
    }
}
```

---

## Notes

- Call `ExtendsContentIntoTitleBar = true` **and** `SetTitleBar(element)` together; omitting
  either causes the window to fall back to the system title bar.
- `TitleBar.IsBackButtonVisible` accepts a `bool`, so bind it to `navFrame.CanGoBack`
  (`Mode=OneWay`) to automatically show/hide the button.
- The `TitleBar` control automatically manages the drag region and caption-button hit-testing;
  you do **not** need to call `AppWindowTitleBar.SetDragRectangles` when using it.
- For `AppWindowTitleBar`, always update button colours on `ActualThemeChanged` to stay in
  sync with light/dark mode.
- When `IsPaneToggleButtonVisible="True"`, wire `PaneToggleRequested` to toggle
  `NavigationView.IsPaneOpen`; the `NavigationView` must set its own
  `IsPaneToggleButtonVisible="False"` to avoid a duplicate hamburger button.
