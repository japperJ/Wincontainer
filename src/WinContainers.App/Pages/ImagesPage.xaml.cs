using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class ImagesPage : Page
{
    private readonly ImagesViewModel _viewModel;

    public ImagesViewModel ViewModel => _viewModel;

    public ImagesPage()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.ImagesViewModel;
        Loaded += async (_, _) => await _viewModel.LoadImagesAsync();
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
            await warnDlg.ShowAsync();
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

        var result = await dlg.ShowAsync();
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

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
                await _viewModel.DeleteImageAsync(_viewModel.SelectedImage);
        }
    }

    private async void RefreshImages_Click(object sender, RoutedEventArgs e)
        => await _viewModel.LoadImagesAsync();
}
