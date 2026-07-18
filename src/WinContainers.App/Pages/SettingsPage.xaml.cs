using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();

        _viewModel = ViewModelLocator.SettingsViewModel;
        Loaded += SettingsPage_Loaded;
    }

    private async void SettingsPage_Loaded(object sender, RoutedEventArgs e)
    {
        PortBox.Text = _viewModel.PortText;
        await _viewModel.LoadAsync();
        ApiLoggingToggle.IsOn = _viewModel.ApiLoggingEnabled;
        UpdateStatusDisplay();
    }

    private void UpdateStatusDisplay()
    {
        EndpointText.Text = _viewModel.StatusText;
        ServiceStatusText.Text = _viewModel.ServiceStatusText;
        ServiceDot.Fill = new SolidColorBrush(_viewModel.ServiceHealthy
            ? Color.FromArgb(255, 0, 200, 83)
            : Color.FromArgb(255, 255, 179, 0));
        VersionText.Text = _viewModel.VersionText;
    }

    private void ApplyPortButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PortText = PortBox.Text;
        _viewModel.ApplyPort();
        EndpointText.Text = _viewModel.StatusText;
    }

    private void ApiLoggingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiLoggingEnabled = ApiLoggingToggle.IsOn;
    }
}
