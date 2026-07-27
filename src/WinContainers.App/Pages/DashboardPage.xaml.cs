using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinContainers_App.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class DashboardPage : Page
{
    private const int ContainersTabIndex = 1;
    private PivotItem? _detailPivotItem;
    private ContainerDetailPage? _activeDetailPage;

    public DashboardPage()
    {
        InitializeComponent();
        if (MainWindow.Instance is { } main)
            main.DashboardPageInstance = this;
        TemplateCatalogContent.UseTemplateRequested += (_, _) => DashboardPivot.SelectedIndex = 3;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var index = MainWindow.ReturnToPivotIndex;
        if (index >= 0 && index < DashboardPivot.Items.Count)
        {
            DashboardPivot.SelectedIndex = index;
            MainWindow.ReturnToPivotIndex = -1;
        }
    }

    public void ShowContainerDetail(ContainerViewModel entry)
    {
        RemoveContainerDetail();

        _detailPivotItem = new PivotItem
        {
            Header = entry.Name,
            Tag = "ContainerDetail"
        };

        _activeDetailPage = new ContainerDetailPage { IsEmbedded = true };
        _activeDetailPage.LoadContainer(entry);
        _detailPivotItem.Content = _activeDetailPage;

        DashboardPivot.Items.Add(_detailPivotItem);
        DashboardPivot.SelectedItem = _detailPivotItem;

        UpdateDetailBar(_activeDetailPage);
    }

    public void RemoveContainerDetail()
    {
        if (_activeDetailPage is not null)
        {
            var frame = Microsoft.UI.Xaml.Window.Current?.Content as Frame;
            if (_activeDetailPage.Parent is null)
            {
                // Page was already removed from tree, just clean up reference
            }

            _activeDetailPage = null;
        }

        if (_detailPivotItem is not null)
        {
            DashboardPivot.Items.Remove(_detailPivotItem);
            _detailPivotItem = null;
        }

        ContainerActionsBar.Visibility = Visibility.Collapsed;

        if (DashboardPivot.SelectedIndex < 0 || DashboardPivot.SelectedItem is PivotItem { Tag: "ContainerDetail" })
            DashboardPivot.SelectedIndex = ContainersTabIndex;
    }

    private void DashboardPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isDetailTab = DashboardPivot.SelectedItem is PivotItem { Tag: "ContainerDetail" };
        ContainerActionsBar.Visibility = isDetailTab ? Visibility.Visible : Visibility.Collapsed;

        if (isDetailTab && _activeDetailPage?.ViewModel is { } vm)
            UpdateDetailBar(vm);
    }

    private void UpdateDetailBar(ContainerDetailViewModel vm)
    {
        DetailContainerName.Text = vm.ContainerName ?? "";
        DetailStatusText.Text = vm.ContainerStatus ?? "";
        DetailStartButton.IsEnabled = vm.IsStartEnabled;
        DetailStopButton.IsEnabled = vm.IsStopEnabled;
        DetailRestartButton.IsEnabled = vm.IsRestartEnabled;
        DetailDeleteButton.IsEnabled = vm.IsDeleteEnabled;
    }

    private void UpdateDetailBar(ContainerDetailPage page)
    {
        if (page.ViewModel is { } vm)
            UpdateDetailBar(vm);
    }

    private void DetailBackButton_Click(object sender, RoutedEventArgs e)
        => RemoveContainerDetail();

    private async void DetailStartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDetailPage?.ViewModel is { } vm)
            await vm.RunActionAsync("Start");
    }

    private async void DetailStopButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDetailPage?.ViewModel is { } vm)
            await vm.RunActionAsync("Stop");
    }

    private async void DetailRestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDetailPage?.ViewModel is { } vm)
            await vm.RunActionAsync("Restart");
    }

    private async void DetailDeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (_activeDetailPage?.ViewModel is { } vm)
        {
            await vm.RunActionAsync("Delete");
            RemoveContainerDetail();
        }
    }
}
