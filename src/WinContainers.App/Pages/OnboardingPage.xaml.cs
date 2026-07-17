using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class OnboardingPage : Page
{
    private readonly OnboardingViewModel _viewModel;

    public OnboardingPage()
    {
        InitializeComponent();
        _viewModel = App.Services.GetRequiredService<OnboardingViewModel>();
        DataContext = _viewModel;
        Loaded += OnboardingPage_Loaded;
    }

    private async void OnboardingPage_Loaded(object sender, RoutedEventArgs e)
    {
        await _viewModel.CheckAllAsync();
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Windows version
        WindowsVersionStatus.Text = _viewModel.WindowsVersionStatus;
        WindowsVersionIcon.Visibility = _viewModel.WindowsVersionOk ? Visibility.Visible : Visibility.Collapsed;

        // Virtualization
        VirtualizationStatus.Text = _viewModel.VirtualizationStatus;
        VirtualizationIcon.Visibility = _viewModel.VirtualizationAvailable ? Visibility.Visible : Visibility.Collapsed;

        // WSL2
        Wsl2Status.Text = _viewModel.Wsl2Status;
        if (_viewModel.Wsl2Available)
        {
            Wsl2Icon.Visibility = Visibility.Visible;
            InstallWsl2Button.Visibility = Visibility.Collapsed;
        }
        else
        {
            Wsl2Icon.Visibility = Visibility.Collapsed;
            InstallWsl2Button.Visibility = Visibility.Visible;
        }

        // WSLC
        WslcStatus.Text = _viewModel.WslcStatus;
        if (_viewModel.WslcAvailable)
        {
            WslcIcon.Visibility = Visibility.Visible;
            InstallWslcButton.Visibility = Visibility.Collapsed;
        }
        else
        {
            WslcIcon.Visibility = Visibility.Collapsed;
            InstallWslcButton.Visibility = Visibility.Visible;
        }

        // Continue button
        ContinueButton.IsEnabled = _viewModel.AllPrerequisitesMet;

        // Progress
        ProgressIndicator.IsActive = _viewModel.IsChecking || _viewModel.IsInstalling;
        ProgressIndicator.Visibility = _viewModel.IsChecking || _viewModel.IsInstalling ? Visibility.Visible : Visibility.Collapsed;

        InstallProgressText.Text = _viewModel.InstallProgress;
        InstallProgressText.Visibility = string.IsNullOrEmpty(_viewModel.InstallProgress) ? Visibility.Collapsed : Visibility.Visible;
    }

    private async void CheckAgain_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.InstallProgress = "";
        await _viewModel.CheckAllAsync();
        UpdateUI();
    }

    private async void InstallWsl2_Click(object sender, RoutedEventArgs e)
    {
        InstallWsl2Button.IsEnabled = false;
        InstallWslcButton.IsEnabled = false;
        await _viewModel.InstallWsl2Async();
        UpdateUI();
        InstallWsl2Button.IsEnabled = true;
        InstallWslcButton.IsEnabled = true;
    }

    private async void InstallWslc_Click(object sender, RoutedEventArgs e)
    {
        InstallWsl2Button.IsEnabled = false;
        InstallWslcButton.IsEnabled = false;
        await _viewModel.InstallWslcAsync();
        UpdateUI();
        InstallWsl2Button.IsEnabled = true;
        InstallWslcButton.IsEnabled = true;
    }

    private void Continue_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.MarkOnboardingComplete();

        MainWindow.Instance?.NavigateToMainContent();
    }
}
