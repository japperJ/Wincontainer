using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly IOutputService _output;

    public DashboardViewModel(IOutputService output)
    {
        _output = output;
    }
}
