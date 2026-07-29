using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class ImagesPage : Page
{
    private readonly ImagesViewModel _viewModel;
    private static readonly SemaphoreSlim _dialogSemaphore = new(1, 1);

    public ImagesViewModel ViewModel => _viewModel;

    public ImagesPage()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.ImagesViewModel;
        Loaded += async (_, _) => await _viewModel.LoadImagesAsync();
    }

    private async Task<ContentDialogResult> ShowDialogAsync(ContentDialog dialog)
    {
        await _dialogSemaphore.WaitAsync();
        try
        {
            return await dialog.ShowAsync();
        }
        finally
        {
            _dialogSemaphore.Release();
        }
    }

    private async void ImageListView_ItemClick(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is ImageEntryData image)
            await _viewModel.LoadImageDetailAsync(image);
    }

    private void BackToImageList_Click(object sender, RoutedEventArgs e)
        => _viewModel.CloseDetail();

    private async void DeleteImageFromList_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ImageEntryData image) return;

        if (image.InUse)
        {
            var warnDlg = new ContentDialog
            {
                Title = "Image in use",
                Content = $"'{image.FullTag}' is currently used by one or more containers. Stop and remove those containers first.",
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await ShowDialogAsync(warnDlg);
            return;
        }

        var dlg = new ContentDialog
        {
            Title = "Delete image",
            Content = $"Remove image '{image.FullTag}'?",
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        var result = await ShowDialogAsync(dlg);
        if (result == ContentDialogResult.Primary)
            await _viewModel.DeleteImageAsync(image);
    }

    private async void DeleteSelectedImage_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedImage is not null)
        {
            var dlg = new ContentDialog
            {
                Title = "Delete image",
                Content = $"Remove image '{_viewModel.SelectedImage.FullTag}'?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };

            var result = await ShowDialogAsync(dlg);
            if (result == ContentDialogResult.Primary)
                await _viewModel.DeleteImageAsync(_viewModel.SelectedImage);
        }
    }

    private async void UpdateImageFromList_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.DataContext is not ImageEntryData image) return;

        var dlg = new ContentDialog
        {
            Title = "Update image",
            Content = $"Pull the latest version of '{image.FullTag}'? Containers using this image may need to be recreated to use the new version.",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await ShowDialogAsync(dlg) != ContentDialogResult.Primary) return;

        try
        {
            await _viewModel.UpdateImageAsync(image);

            if (image.InUse)
            {
                var recreateDlg = new ContentDialog
                {
                    Title = "Recreate containers?",
                    Content = $"'{image.FullTag}' has been updated. It is used by running containers. Recreate them now? Stopped containers will be recreated too. The original name, ports, and volume mounts are preserved.",
                    PrimaryButtonText = "Recreate",
                    CloseButtonText = "Later",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                if (await ShowDialogAsync(recreateDlg) == ContentDialogResult.Primary)
                    await _viewModel.RecreateContainersForImageAsync(image);
            }
        }
        catch (Exception ex)
        {
            var errDlg = new ContentDialog
            {
                Title = "Update failed",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await ShowDialogAsync(errDlg);
        }
    }

    private async void UpdateSelectedImage_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedImage is null) return;

        var dlg = new ContentDialog
        {
            Title = "Update image",
            Content = $"Pull the latest version of '{_viewModel.SelectedImage.FullTag}'? Containers using this image may need to be recreated to use the new version.",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot
        };

        if (await ShowDialogAsync(dlg) != ContentDialogResult.Primary) return;

        try
        {
            await _viewModel.UpdateImageAsync(_viewModel.SelectedImage);

            if (_viewModel.SelectedImage.InUse)
            {
                var recreateDlg = new ContentDialog
                {
                    Title = "Recreate containers?",
                    Content = $"'{_viewModel.SelectedImage.FullTag}' has been updated. It is used by running containers. Recreate them now? Stopped containers will be recreated too. The original name, ports, and volume mounts are preserved.",
                    PrimaryButtonText = "Recreate",
                    CloseButtonText = "Later",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = XamlRoot
                };

                if (await ShowDialogAsync(recreateDlg) == ContentDialogResult.Primary)
                    await _viewModel.RecreateContainersForImageAsync(_viewModel.SelectedImage);
            }
        }
        catch (Exception ex)
        {
            var errDlg = new ContentDialog
            {
                Title = "Update failed",
                Content = ex.Message,
                CloseButtonText = "OK",
                XamlRoot = XamlRoot
            };
            await ShowDialogAsync(errDlg);
        }
    }

    private async void RefreshImages_Click(object sender, RoutedEventArgs e)
        => await _viewModel.LoadImagesAsync();
}
