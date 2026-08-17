using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinContainers_App.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class DashboardPage : Page
{
    private ContainerDetailPage? _activeDetailPage;
    private string _selectedSection = "Overview";

    public DashboardPage()
    {
        InitializeComponent();
        if (MainWindow.Instance is { } main)
            main.DashboardPageInstance = this;

        TemplateCatalogContent.UseTemplateRequested += (_, _) => ShowSection("CreateContainer");
    }

    public string SelectedSection => _selectedSection;

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        ShowSection(_selectedSection);
        OverviewContent.UpdateServiceStatus();
    }

    public void ShowSection(string tag)
    {
        if (_activeDetailPage is not null && tag != "ContainerDetail")
        {
            DetailContent.Content = null;
            _activeDetailPage = null;
        }

        _selectedSection = tag;
        OverviewContent.Visibility = Visibility.Collapsed;
        ContainersContent.Visibility = Visibility.Collapsed;
        ImagesContent.Visibility = Visibility.Collapsed;
        QuickActionsContent.Visibility = Visibility.Collapsed;
        TemplateCatalogContent.Visibility = Visibility.Collapsed;
        ComposeContent.Visibility = Visibility.Collapsed;
        VolumesContent.Visibility = Visibility.Collapsed;
        NetworksContent.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Collapsed;

        var isDetail = tag == "ContainerDetail" && _activeDetailPage is not null;
        ContainerActionsBar.Visibility = isDetail ? Visibility.Visible : Visibility.Collapsed;
        if (isDetail)
        {
            DetailContent.Visibility = Visibility.Visible;
            if (_activeDetailPage?.ViewModel is { } vm)
                UpdateDetailBar(vm);
        }
        else
        {
            switch (tag)
            {
                case "Overview": OverviewContent.Visibility = Visibility.Visible; break;
                case "Containers": ContainersContent.Visibility = Visibility.Visible; break;
                case "Images": ImagesContent.Visibility = Visibility.Visible; break;
                case "CreateContainer": QuickActionsContent.Visibility = Visibility.Visible; break;
                case "TemplateCatalog": TemplateCatalogContent.Visibility = Visibility.Visible; break;
                case "Compose": ComposeContent.Visibility = Visibility.Visible; break;
                case "Volumes": VolumesContent.Visibility = Visibility.Visible; break;
                case "Networks": NetworksContent.Visibility = Visibility.Visible; break;
            }
        }
    }

    public void ShowContainerDetail(ContainerViewModel entry)
    {
        RemoveContainerDetail();

        _selectedSection = "ContainerDetail";
        _activeDetailPage = new ContainerDetailPage { IsEmbedded = true };
        _activeDetailPage.LoadContainer(entry);
        DetailContent.Content = _activeDetailPage;
        ShowSection("ContainerDetail");
    }

    public void RemoveContainerDetail()
    {
        DetailContent.Content = null;
        _activeDetailPage = null;
        ContainerActionsBar.Visibility = Visibility.Collapsed;
        ShowSection("Containers");
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
