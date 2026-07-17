using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IOutputService _output;
    private readonly ContainerService _containerService;

    public DashboardViewModel(IOutputService output, ContainerService containerService)
    {
        _output = output;
        _containerService = containerService;
    }
}
