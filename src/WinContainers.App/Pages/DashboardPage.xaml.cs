using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace WinContainers_App.Pages;

public sealed partial class DashboardPage : Page
{
    public DashboardPage()
    {
        InitializeComponent();
        TemplateCatalogContent.UseTemplateRequested += (_, _) => DashboardPivot.SelectedIndex = 3;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        // Select the requested pivot tab (set by container detail back-navigation)
        var index = MainWindow.ReturnToPivotIndex;
        if (index >= 0 && index < DashboardPivot.Items.Count)
        {
            DashboardPivot.SelectedIndex = index;
            MainWindow.ReturnToPivotIndex = -1;
        }
    }
}
