using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Velopack;
using WinContainers_App.Pages;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;
using WinContainers.Core.Models;
using WinContainers.Runtime;
using WinContainers.Service.Host;

namespace WinContainers_App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static DispatcherQueue? DispatcherQueue { get; private set; }

    public static WslcServiceClient ServiceClient { get; private set; } = null!;

    private Window? _window;

    public App()
    {
        VelopackApp.Build().Run();

        InitializeComponent();

        UnhandledException += OnUnhandledException;

        var services = new ServiceCollection();

        services.AddLogging(builder =>
        {
            builder.AddDebug();
            builder.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Debug);
        });

        services.AddSingleton<IOutputService>(_ => OutputService.Instance);
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IDialogService>(sp =>
            new DialogService(() => (_window as MainWindow)?.Content?.XamlRoot));

        services.AddSingleton<ContainerService>();
        services.AddSingleton<WslcDriver>();
        services.AddSingleton<TemplateCatalogService>();
        services.AddSingleton<WslcUpdateService>();

        services.AddTransient<ShellViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ContainersViewModel>();
        services.AddTransient<ContainerDetailViewModel>();
        services.AddTransient<ImagesViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<TerminalViewModel>();
        services.AddSingleton<QuickActionsViewModel>();
        services.AddTransient<OverviewViewModel>();
        services.AddTransient<OnboardingViewModel>();

        Services = services.BuildServiceProvider();
    }

    public Window? GetMainWindow() => _window;

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        DispatcherQueue = DispatcherQueue.GetForCurrentThread();

        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateService.CheckForUpdatesAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check failed: {ex}");
            }
        });

        _ = Task.Run(() =>
        {
            try
            {
                var settingsService = Services.GetRequiredService<AppSettingsService>();
                var settings = settingsService.Load();

                OutputService.Instance.ApiLoggingEnabled = settings.ApiLoggingEnabled;
                OutputService.Instance.RemoteApiLoggingEnabled = settings.RemoteApiLoggingEnabled;
                if (!string.IsNullOrWhiteSpace(settings.ApiToken))
                {
                    ServiceEndpointResolver.SetToken(settings.ApiToken);
                }

                ServiceClient = new WslcServiceClient(ServiceEndpointResolver.Resolve(), OutputService.Instance);
                ServiceHost.Build([], OutputService.Instance).Run();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ServiceHost failed: {ex}");
            }
        });

        if (OnboardingViewModel.IsFirstRun())
        {
            _window = new MainWindow();
            _window.Activate();
            if (_window is MainWindow mainWnd)
            {
                mainWnd.NavigateToPage(typeof(OnboardingPage));
            }
        }
        else
        {
            _window = new MainWindow();
            _window.Activate();
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UnhandledException: {e.Exception}");
            OutputService.Instance?.Write($"UnhandledException: {e.Exception}", WinContainers_App.Services.LogLevel.Error);
        }
        catch (Exception logEx)
        {
            System.Diagnostics.Debug.WriteLine($"UnhandledException logging failed: {logEx}");
        }
        e.Handled = true;
    }
}
