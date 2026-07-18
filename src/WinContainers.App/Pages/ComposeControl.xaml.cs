using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.Pages;

public sealed partial class ComposeControl : UserControl
{
    private readonly QuickActionsViewModel _viewModel;

    public ComposeControl()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.QuickActionsViewModel;
        DataContext = _viewModel;
        _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        UpdatePreviewState();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.ShowComposePreview) ||
            e.PropertyName == nameof(_viewModel.ParsedServices))
        {
            UpdatePreviewState();
        }
    }

    private async void ParseComposeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ComposeYamlText = ComposeTextBox.Text;
            await _viewModel.ParseComposeYamlAsync();
            UpdatePreviewState();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Parse compose failed: {ex}", ServiceLogLevel.Error);
        }
    }

    private async void CreateAllButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MainWindow.Instance?.EnsureOutputPaneVisible();
            await _viewModel.CreateAllFromComposeAsync();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Create all failed: {ex}", ServiceLogLevel.Error);
        }
    }

    private void UpdatePreviewState()
    {
        var showPreview = _viewModel.ShowComposePreview && _viewModel.ParsedServices.Count > 0;
        ComposePreviewPanel.Visibility = showPreview ? Visibility.Visible : Visibility.Collapsed;
        EmptyPreviewPanel.Visibility = showPreview ? Visibility.Collapsed : Visibility.Visible;
    }
}
