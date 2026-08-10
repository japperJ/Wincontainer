using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class OverviewControl : UserControl
{
    private readonly OverviewViewModel _viewModel;
    private DispatcherTimer? _pollTimer;

    public OverviewControl()
    {
        InitializeComponent();

        _viewModel = ViewModelLocator.OverviewViewModel;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public void UpdateServiceStatus()
    {
        var output = OutputService.Instance;
        var enabledBrush = new SolidColorBrush(Color.FromArgb(255, 0, 200, 83));
        var disabledBrush = new SolidColorBrush(Color.FromArgb(255, 255, 179, 0));

        McpStatusText.Text = output.McpEnabled ? "Enabled" : "Disabled";
        McpStatusDot.Fill = output.McpEnabled ? enabledBrush : disabledBrush;
        ApiStatusText.Text = output.AllowRemoteApiAccess ? "Allowed" : "Blocked";
        ApiStatusDot.Fill = output.AllowRemoteApiAccess ? enabledBrush : disabledBrush;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateServiceStatus();
        _ = RefreshUiAsync();
        _pollTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _pollTimer.Tick += async (_, _) =>
        {
            UpdateServiceStatus();
            await RefreshUiAsync();
        };
        _pollTimer.Start();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _pollTimer?.Stop();
        _pollTimer = null;
    }

    private async Task RefreshUiAsync()
    {
        await _viewModel.RefreshAsync();

        DispatcherQueue.TryEnqueue(() =>
        {
            StatusText.Text = _viewModel.StatusText;
            StatusDot.Fill = new SolidColorBrush(_viewModel.IsRuntimeAvailable
                ? Color.FromArgb(255, 0, 200, 83)
                : Color.FromArgb(255, 255, 179, 0));
            ContainerCountText.Text = _viewModel.ContainerCountText;
            RunningCountText.Text = _viewModel.RunningCountText;
            ImageCountText.Text = _viewModel.ImageCountText;
            WslcVersionText.Text = _viewModel.WslcVersionText;
            SetupHintCard.Visibility = _viewModel.ShowSetupHint ? Visibility.Visible : Visibility.Collapsed;
            SetupHintText.Text = _viewModel.SetupHintText;
        });
    }
}
