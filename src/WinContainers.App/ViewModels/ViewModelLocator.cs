using Microsoft.Extensions.DependencyInjection;
using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public static class ViewModelLocator
{
    public static ContainersViewModel ContainersViewModel =>
        App.Services.GetRequiredService<ContainersViewModel>();

    public static ContainerDetailViewModel ContainerDetailViewModel =>
        App.Services.GetRequiredService<ContainerDetailViewModel>();

    public static OverviewViewModel OverviewViewModel =>
        App.Services.GetRequiredService<OverviewViewModel>();

    public static QuickActionsViewModel QuickActionsViewModel =>
        App.Services.GetRequiredService<QuickActionsViewModel>();

    public static ImagesViewModel ImagesViewModel =>
        App.Services.GetRequiredService<ImagesViewModel>();

    public static SettingsViewModel SettingsViewModel =>
        App.Services.GetRequiredService<SettingsViewModel>();

    public static TerminalViewModel TerminalViewModel =>
        App.Services.GetRequiredService<TerminalViewModel>();

    public static INavigationService NavigationService =>
        App.Services.GetRequiredService<INavigationService>();

    public static IOutputService OutputService =>
        App.Services.GetRequiredService<IOutputService>();
}
