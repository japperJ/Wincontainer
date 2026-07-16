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
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(_viewModel.ShowMultiServiceSummary))
            MultiServicePanel.Visibility = _viewModel.ShowMultiServiceSummary ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ParseComposeButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _viewModel.ComposeYamlText = ComposeTextBox.Text;
            _viewModel.ParseComposeYaml();
            MultiServicePanel.Visibility = _viewModel.ShowMultiServiceSummary ? Visibility.Visible : Visibility.Collapsed;
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
            await _viewModel.CreateAllFromComposeAsync();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Create all failed: {ex}", ServiceLogLevel.Error);
        }
    }
}
