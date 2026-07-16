using System.ComponentModel;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class GroupLogPage : Page, INotifyPropertyChanged
{
    private GroupLogViewModel? _viewModel;

    public event PropertyChangedEventHandler? PropertyChanged;

    public GroupLogViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (_viewModel != value)
            {
                _viewModel = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ViewModel)));
            }
        }
    }

    public GroupLogPage()
    {
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is not ContainerGroup group)
            return;

        _viewModel = ViewModelLocator.GroupLogViewModel;
        ViewModel = _viewModel;
        _viewModel.LoadGroup(group);

        _ = _viewModel.LoadLogsAsync();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
        => _viewModel?.NavigateBack();

    private async void RefreshLogsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel is not null)
            await _viewModel.LoadLogsAsync();
    }
}
