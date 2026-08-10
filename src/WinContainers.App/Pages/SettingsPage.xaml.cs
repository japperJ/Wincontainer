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
        await _viewModel.LoadAsync();
        PortBox.Text = _viewModel.PortText;
        TokenBox.Text = _viewModel.TokenText;
        ApiLoggingToggle.IsOn = _viewModel.ApiLoggingEnabled;
        RemoteApiLoggingToggle.IsOn = _viewModel.RemoteApiLoggingEnabled;
        AllowRemoteApiToggle.IsOn = _viewModel.AllowRemoteApiAccess;
        McpEnabledToggle.IsOn = _viewModel.McpEnabled;
        McpLoggingToggle.IsOn = _viewModel.McpLoggingEnabled;
        AppVersionText.Text = _viewModel.AppVersion;
        UpdateChannelBox.SelectedValue = _viewModel.UpdateChannel;
        AiProviderBox.SelectedValue = _viewModel.AiProviderKind;
        AiEndpointBox.Text = _viewModel.AiEndpoint;
        AiModelBox.Text = _viewModel.AiModel;
        AiKeyBox.Password = _viewModel.AiApiKey ?? string.Empty;
        AiConfirmToggle.IsOn = _viewModel.AiConfirmDestructiveActions;
        AiStatusText.Text = _viewModel.AiStatusText;
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
        AppUpdateStatusText.Text = _viewModel.AppUpdateStatus;
        CheckAppUpdateButton.IsEnabled = !_viewModel.IsCheckingAppUpdate;
        InstallAppUpdateButton.IsEnabled = _viewModel.AppUpdateAvailable && !_viewModel.IsCheckingAppUpdate;
        DeferAppUpdateButton.IsEnabled = _viewModel.AppUpdateAvailable && !_viewModel.IsCheckingAppUpdate;
        WslcUpdateStatusText.Text = _viewModel.WslcUpdateStatus;
        UpdateWslcButton.IsEnabled = _viewModel.WslcUpdateAvailable && !_viewModel.IsCheckingWslcUpdate;
        CheckWslcUpdateButton.IsEnabled = !_viewModel.IsCheckingWslcUpdate;
    }

    private void ApplyPortButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.PortText = PortBox.Text;
        _viewModel.ApplyPort();
        EndpointText.Text = _viewModel.StatusText;
    }

    private void ApplyTokenButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TokenText = TokenBox.Text;
        _viewModel.ApplyToken();
        EndpointText.Text = _viewModel.StatusText;
    }

    private void ApiLoggingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.ApiLoggingEnabled = ApiLoggingToggle.IsOn;
        _viewModel.SaveLoggingSettings();
    }

    private void RemoteApiLoggingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.RemoteApiLoggingEnabled = RemoteApiLoggingToggle.IsOn;
        _viewModel.SaveLoggingSettings();
    }

    private void AllowRemoteApiToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.AllowRemoteApiAccess = AllowRemoteApiToggle.IsOn;
        _viewModel.SaveLoggingSettings();
    }

    private void McpEnabledToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.McpEnabled = McpEnabledToggle.IsOn;
        _viewModel.SaveLoggingSettings();
    }

    private void McpLoggingToggle_Toggled(object sender, RoutedEventArgs e)
    {
        _viewModel.McpLoggingEnabled = McpLoggingToggle.IsOn;
        _viewModel.SaveLoggingSettings();
    }

    private async void CheckWslcUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        CheckWslcUpdateButton.IsEnabled = false;
        await _viewModel.CheckWslcUpdateAsync();
        UpdateStatusDisplay();
    }

    private async void CheckAppUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.CheckAppUpdateAsync();
        UpdateStatusDisplay();
    }

    private async void InstallAppUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.InstallAppUpdateAsync();
        UpdateStatusDisplay();
    }

    private void DeferAppUpdateButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.DeferAppUpdate();
        UpdateStatusDisplay();
    }

    private void UpdateChannelBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (UpdateChannelBox.SelectedValue is string channel)
        {
            _viewModel.UpdateChannel = channel;
        }
    }

    private async void UpdateWslcButton_Click(object sender, RoutedEventArgs e)
    {
        CheckWslcUpdateButton.IsEnabled = false;
        UpdateWslcButton.IsEnabled = false;
        await _viewModel.UpdateWslcAsync();
        UpdateStatusDisplay();
    }

    private void SaveAiSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AiProviderKind = (AiProviderBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "OpenAiCompatible";
        _viewModel.AiEndpoint = AiEndpointBox.Text;
        _viewModel.AiModel = AiModelBox.Text;
        _viewModel.AiApiKey = string.IsNullOrWhiteSpace(AiKeyBox.Password) ? null : AiKeyBox.Password;
        _viewModel.AiConfirmDestructiveActions = AiConfirmToggle.IsOn;
        _viewModel.SaveAiSettings();
        AiStatusText.Text = _viewModel.AiStatusText;
    }
}
