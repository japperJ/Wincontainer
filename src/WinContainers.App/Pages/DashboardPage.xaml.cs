using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using WinContainers_App.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

/// <summary>
/// Represents an item in the dashboard side navigation panel.
/// </summary>
public class DashboardNavItem
{
    public required string Header { get; init; }
    public required string Tag { get; init; }
}

public sealed partial class DashboardPage : Page
{
    private const int ContainersTabIndex = 1;
    private readonly ObservableCollection<DashboardNavItem> _navItems = [];
    private DashboardNavItem? _detailNavItem;
    private ContainerDetailPage? _activeDetailPage;

    public DashboardPage()
    {
        InitializeComponent();
        if (MainWindow.Instance is { } main)
            main.DashboardPageInstance = this;

        _navItems =
        [
            new() { Header = "Overview", Tag = "Overview" },
            new() { Header = "Containers", Tag = "Containers" },
            new() { Header = "Images", Tag = "Images" },
            new() { Header = "Create Container", Tag = "CreateContainer" },
            new() { Header = "Template Catalog", Tag = "TemplateCatalog" },
            new() { Header = "Compose", Tag = "Compose" },
            new() { Header = "Volumes", Tag = "Volumes" },
            new() { Header = "Networks", Tag = "Networks" },
        ];
        SideNavList.ItemsSource = _navItems;
        SideNavList.SelectedIndex = 0;

        TemplateCatalogContent.UseTemplateRequested += (_, _) => SelectNavItemByTag("CreateContainer");
    }

    private void SelectNavItemByTag(string tag)
    {
        var item = _navItems.FirstOrDefault(n => n.Tag == tag);
        if (item is not null)
            SideNavList.SelectedItem = item;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var index = MainWindow.ReturnToPivotIndex;
        if (index >= 0 && index < _navItems.Count)
        {
            SideNavList.SelectedIndex = index;
            MainWindow.ReturnToPivotIndex = -1;
        }

        OverviewContent.UpdateServiceStatus();
    }

    /// <summary>
    /// Gets the index of the currently selected content tab (excluding container detail).
    /// Used to return to the right tab after navigating away and back.
    /// </summary>
    public int SelectedTabIndex
    {
        get
        {
            if (SideNavList.SelectedItem is DashboardNavItem item)
            {
                var idx = _navItems.IndexOf(item);
                return idx >= 0 ? idx : 0;
            }
            return 0;
        }
    }

    private void SideNavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SideNavList.SelectedItem is DashboardNavItem item)
            ShowContent(item.Tag);
    }

    private void ShowContent(string tag)
    {
        // Hide all content panels
        OverviewContent.Visibility = Visibility.Collapsed;
        ContainersContent.Visibility = Visibility.Collapsed;
        ImagesContent.Visibility = Visibility.Collapsed;
        QuickActionsContent.Visibility = Visibility.Collapsed;
        TemplateCatalogContent.Visibility = Visibility.Collapsed;
        ComposeContent.Visibility = Visibility.Collapsed;
        VolumesContent.Visibility = Visibility.Collapsed;
        NetworksContent.Visibility = Visibility.Collapsed;
        DetailContent.Visibility = Visibility.Collapsed;

        var isDetailTab = tag == "ContainerDetail";
        ContainerActionsBar.Visibility = isDetailTab ? Visibility.Visible : Visibility.Collapsed;

        if (isDetailTab)
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

        _detailNavItem = new DashboardNavItem
        {
            Header = entry.Name,
            Tag = "ContainerDetail"
        };

        _activeDetailPage = new ContainerDetailPage { IsEmbedded = true };
        _activeDetailPage.LoadContainer(entry);
        DetailContent.Content = _activeDetailPage;

        _navItems.Add(_detailNavItem);
        SideNavList.SelectedItem = _detailNavItem;

        UpdateDetailBar(_activeDetailPage);
    }

    public void RemoveContainerDetail()
    {
        DetailContent.Content = null;
        _activeDetailPage = null;

        if (_detailNavItem is not null)
        {
            _navItems.Remove(_detailNavItem);
            _detailNavItem = null;
        }

        ContainerActionsBar.Visibility = Visibility.Collapsed;

        if (SideNavList.SelectedIndex < 0 ||
            SideNavList.SelectedItem is DashboardNavItem { Tag: "ContainerDetail" })
        {
            SelectNavItemByTag("Containers");
        }
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
