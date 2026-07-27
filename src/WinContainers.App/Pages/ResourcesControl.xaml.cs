using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class ResourcesControl : UserControl
{
    private readonly ResourceListViewModel _viewModel;

    public static readonly DependencyProperty ResourceTypeProperty =
        DependencyProperty.Register(nameof(ResourceType), typeof(string), typeof(ResourcesControl), new PropertyMetadata("Volumes"));

    public string ResourceType
    {
        get => (string)GetValue(ResourceTypeProperty);
        set => SetValue(ResourceTypeProperty, value);
    }

    public ResourceListViewModel ViewModel => _viewModel;

    public ResourcesControl()
    {
        InitializeComponent();
        _viewModel = new ResourceListViewModel(ViewModelLocator.OutputService, ViewModelLocator.ServiceClient);
        Loaded += ResourcesControl_Loaded;
    }

    private async void ResourcesControl_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= ResourcesControl_Loaded;
        _viewModel.ResourceType = ResourceType;
        await _viewModel.LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e)
        => await _viewModel.LoadAsync();

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ResourceEntryData resource)
            return;

        if (!resource.CanDelete)
        {
            var protectedDialog = new ContentDialog
            {
                Title = "Built-in network",
                Content = $"The '{resource.Name}' network is managed by WSLC and cannot be deleted.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await protectedDialog.ShowAsync();
            return;
        }

        var singular = ResourceType.EndsWith('s') ? ResourceType[..^1].ToLowerInvariant() : ResourceType.ToLowerInvariant();
        var dialog = new ContentDialog
        {
            Title = $"Delete {singular}",
            Content = $"Remove {singular} '{resource.Name}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = XamlRoot
        };

        if (await dialog.ShowAsync() != ContentDialogResult.Primary)
            return;

        try
        {
            await _viewModel.DeleteAsync(resource);
        }
        catch (Exception ex)
        {
            var error = new ContentDialog
            {
                Title = $"Unable to delete {singular}",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await error.ShowAsync();
        }
    }
}
