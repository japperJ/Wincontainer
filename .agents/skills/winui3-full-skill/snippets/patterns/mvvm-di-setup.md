# App Patterns: MVVM + DI Setup

This file shows how to wire up a complete WinUI 3 application with:
- `Microsoft.Extensions.DependencyInjection` for DI
- `CommunityToolkit.Mvvm` for MVVM (`ObservableObject`, `[ObservableProperty]`, `RelayCommand`)
- `Microsoft.Extensions.Logging` for logging
- A navigation service for type-safe page navigation

---

## Project File (MyApp.csproj)

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows10.0.19041.0</TargetFramework>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
    <RootNamespace>MyApp</RootNamespace>
    <ApplicationIcon>Assets\StoreLogo.ico</ApplicationIcon>
    <UseWinUI>true</UseWinUI>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.*" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Logging" Version="8.*" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="8.*" />
  </ItemGroup>
</Project>
```

---

## GlobalUsings.cs

```csharp
global using System;
global using System.Collections.Generic;
global using System.Collections.ObjectModel;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
global using CommunityToolkit.Mvvm.ComponentModel;
global using CommunityToolkit.Mvvm.Input;
global using Microsoft.Extensions.Logging;
global using Microsoft.UI.Xaml;
global using Microsoft.UI.Xaml.Controls;
```

---

## INavigationService / NavigationService

```csharp
// Services/INavigationService.cs
namespace MyApp.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void NavigateTo<TPage>() where TPage : Page;
    void NavigateTo<TPage>(object parameter) where TPage : Page;
    void GoBack();
    void SetFrame(Frame frame);
}
```

```csharp
// Services/NavigationService.cs
using Microsoft.UI.Xaml.Navigation;

namespace MyApp.Services;

public class NavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void SetFrame(Frame frame) => _frame = frame;

    public void NavigateTo<TPage>() where TPage : Page
        => _frame?.Navigate(typeof(TPage));

    public void NavigateTo<TPage>(object parameter) where TPage : Page
        => _frame?.Navigate(typeof(TPage), parameter);

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
```

---

## App.xaml.cs — DI Registration

```csharp
// App.xaml.cs
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MyApp.Services;
using MyApp.ViewModels;
using MyApp.Views;

namespace MyApp;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    private Window? _mainWindow;

    public App()
    {
        InitializeComponent();

        var services = new ServiceCollection();

        // Logging
        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(LogLevel.Debug);
        });

        // Services (register as singletons or transient as appropriate)
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService>(sp =>
        {
            // DialogService needs the Window — provide it after window is created
            return new LazyDialogService(() => _mainWindow!);
        });

        // ViewModels — transient so each navigation creates a fresh instance
        services.AddTransient<ShellViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<SettingsViewModel>();

        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _mainWindow = new MainWindow();
        _mainWindow.Activate();
    }
}
```

---

## MainWindow.xaml.cs

```csharp
// MainWindow.xaml.cs
using MyApp.Views;

namespace MyApp;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Content = new ShellPage();
    }
}
```

---

## Base ViewModel Pattern

```csharp
// ViewModels/HomeViewModel.cs
namespace MyApp.ViewModels;

public partial class HomeViewModel : ObservableObject
{
    private readonly ILogger<HomeViewModel> _logger;
    private readonly INavigationService _navigation;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasItems))]
    private ObservableCollection<string> _items = new();

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    public bool HasItems => Items.Count > 0;

    public HomeViewModel(
        ILogger<HomeViewModel> logger,
        INavigationService navigation)
    {
        _logger = logger;
        _navigation = navigation;
    }

    [RelayCommand]
    private async Task LoadAsync(CancellationToken ct)
    {
        IsLoading = true;
        ErrorMessage = null;
        try
        {
            await Task.Delay(500, ct); // simulate async load
            Items = new ObservableCollection<string> { "Item A", "Item B", "Item C" };
            _logger.LogInformation("Loaded {Count} items", Items.Count);
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Load cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load items");
            ErrorMessage = "Failed to load items. Please try again.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoToDetail(string item)
    {
        _navigation.NavigateTo<DetailPage>(item);
    }
}
```

---

## Resolving ViewModels in Views

```csharp
// Views/HomePage.xaml.cs
using MyApp.ViewModels;

namespace MyApp.Views;

public sealed partial class HomePage : Page
{
    public HomeViewModel ViewModel { get; } =
        App.Services.GetRequiredService<HomeViewModel>();

    public HomePage()
    {
        InitializeComponent();
        Loaded += async (_, _) => await ViewModel.LoadCommand.ExecuteAsync(null);
    }
}
```

```xaml
<!-- Views/HomePage.xaml -->
<Page x:Class="MyApp.Views.HomePage"
      xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
      xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Grid Padding="24">
        <ProgressRing IsActive="{x:Bind ViewModel.IsLoading, Mode=OneWay}"
                      HorizontalAlignment="Center" VerticalAlignment="Center" />
        <InfoBar
            Title="Error"
            Message="{x:Bind ViewModel.ErrorMessage, Mode=OneWay}"
            Severity="Error"
            IsOpen="{x:Bind ViewModel.ErrorMessage, Mode=OneWay, Converter={StaticResource NullToBoolConverter}}"
            IsClosable="True" />
        <ListView
            ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}"
            Visibility="{x:Bind ViewModel.HasItems, Mode=OneWay}">
            <ListView.ItemTemplate>
                <DataTemplate x:DataType="x:String">
                    <TextBlock Text="{x:Bind}" />
                </DataTemplate>
            </ListView.ItemTemplate>
        </ListView>
    </Grid>
</Page>
```

---

## Notes

- Use `AddTransient<TViewModel>` for ViewModels — each page navigation gets a fresh instance.
- Use `AddSingleton<TService>` for services that hold global state (navigation, settings, auth).
- Prefer constructor injection; only use `App.Services.GetRequiredService<T>()` at the
  composition root (e.g., in `Page` constructors or `App.OnLaunched`).
- `[ObservableProperty]` generates the backing field, property, and `OnPropertyChanged`.
- `[RelayCommand]` generates an `ICommand` from an `async Task` or `void` method.
- Always use `CancellationToken` in `async Task` relay commands — CommunityToolkit passes
  it automatically when the command is cancelled.
