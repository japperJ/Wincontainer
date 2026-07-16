using WinContainers_App.Services;

namespace WinContainers_App.ViewModels;

public partial class ShellViewModel : ViewModelBase
{
    private readonly INavigationService _navigation;
    private readonly IOutputService _output;

    public ShellViewModel(INavigationService navigation, IOutputService output)
    {
        _navigation = navigation;
        _output = output;
    }


    private bool _isBackEnabled;
    public bool IsBackEnabled
    {
        get => _isBackEnabled;
        set => SetProperty(ref _isBackEnabled, value);
    }

    private string? _outputPaneText;
    public string? OutputPaneText
    {
        get => _outputPaneText;
        set => SetProperty(ref _outputPaneText, value);
    }

    private string? _outputContent;
    public string? OutputContent
    {
        get => _outputContent;
        set => SetProperty(ref _outputContent, value);
    }

    private string? _selectedNavTag;
    public string? SelectedNavTag
    {
        get => _selectedNavTag;
        set => SetProperty(ref _selectedNavTag, value);
    }


    [RelayCommand]
    private void NavigateToPage(string tag)
    {
        SelectedNavTag = tag;
    }

    [RelayCommand]
    private void ClearOutput()
    {
        OutputContent = string.Empty;
        OutputPaneText = "Cleared";
        _output.Clear();
    }

    [RelayCommand]
    private void GoBack()
    {
        _navigation.GoBack();
    }
}
