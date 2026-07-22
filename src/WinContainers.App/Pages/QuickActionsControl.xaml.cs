using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class QuickActionsControl : UserControl
{
    private readonly QuickActionsViewModel _viewModel;

    public QuickActionsControl()
    {
        InitializeComponent();

        _viewModel = ViewModelLocator.QuickActionsViewModel;
        DataContext = _viewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.ShowImageResults):
                    ImageResultsList.Visibility = _viewModel.ShowImageResults ? Visibility.Visible : Visibility.Collapsed;
                    break;
                case nameof(_viewModel.ImageResults):
                    ImageResultsList.ItemsSource = _viewModel.ImageResults;
                    break;
                case nameof(_viewModel.Ports):
                    UpdatePortsHeader();
                    break;
                case nameof(_viewModel.Volumes):
                    UpdateVolumesHeader();
                    break;
                case nameof(_viewModel.EnvVars):
                    UpdateEnvVarsHeader();
                    break;
            }
        };

        _viewModel.Ports.CollectionChanged += (_, _) => UpdatePortsHeader();
        _viewModel.Volumes.CollectionChanged += (_, _) => UpdateVolumesHeader();
        _viewModel.EnvVars.CollectionChanged += (_, _) => UpdateEnvVarsHeader();

    }

    private void UpdatePortsHeader()
    {
        PortsHeader.Text = $"\u25BC Ports ({_viewModel.Ports.Count})";
    }

    private void UpdateVolumesHeader()
    {
        VolumesHeader.Text = $"\u25BC Volumes ({_viewModel.Volumes.Count})";
    }

    private void UpdateEnvVarsHeader()
    {
        EnvVarsHeader.Text = $"\u25BC Environment ({_viewModel.EnvVars.Count})";
    }

    private void ToggleCollapsible(TextBlock header, StackPanel panel)
    {
        var isVisible = panel.Visibility == Visibility.Collapsed;
        panel.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        header.Text = (isVisible ? "\u25BC " : "\u25B6 ") + header.Text[2..];
    }

    private void PortsHeader_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ToggleCollapsible(PortsHeader, PortsPanel);
    }

    private void VolumesHeader_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ToggleCollapsible(VolumesHeader, VolumesPanel);
    }

    private void EnvVarsHeader_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ToggleCollapsible(EnvVarsHeader, EnvVarsPanel);
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.SearchDockerHubAsync(_viewModel.ImageSearchText ?? "");
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Search failed: {ex}", WinContainers_App.Services.LogLevel.Error);
        }
    }

    private void ImageSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        try
        {
            _viewModel.DebounceSearch(_viewModel.ImageSearchText ?? "");
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Search debounce failed: {ex}", WinContainers_App.Services.LogLevel.Error);
        }
    }

    private void ImageResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ImageResultsList.SelectedItem is ImageResult result)
        {
            ImageSearchBox.Text = result.Name;
            _viewModel.ImageSearchText = result.Name;
            ImageResultsList.Visibility = Visibility.Collapsed;
        }
    }

    private void AddPortButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddPort();
    }

    private void RemovePortButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is PortEntry entry)
        {
            _viewModel.RemovePort(entry);
        }
    }

    private void AddVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddVolume();
    }

    private void RemoveVolumeButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is VolumeEntry entry)
        {
            _viewModel.RemoveVolume(entry);
        }
    }

    private void AddEnvVarButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.AddEnvVar();
    }

    private void RemoveEnvVarButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is EnvVarEntry entry)
        {
            _viewModel.RemoveEnvVar(entry);
        }
    }

    private async void CreateAndStartButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            MainWindow.Instance?.EnsureOutputPaneVisible();
            await _viewModel.CreateAndStartContainerAsync();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Create + Start failed: {ex}", WinContainers_App.Services.LogLevel.Error);
        }
    }

    private async void PullImageButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _viewModel.PullImageAsync();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Pull image failed: {ex}", WinContainers_App.Services.LogLevel.Error);
        }
    }

}
