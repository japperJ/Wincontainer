namespace WinContainers_App.Services;

public sealed class NavigationService : INavigationService
{
    private Frame? _frame;

    public bool CanGoBack => _frame?.CanGoBack ?? false;

    public void SetFrame(Frame frame) => _frame = frame;

    public void NavigateTo<TPage>() where TPage : Page
        => _frame?.Navigate(typeof(TPage));

    public void NavigateTo<TPage>(object parameter) where TPage : Page
        => _frame?.Navigate(typeof(TPage), parameter);

    public void GoBack()
    {
        if (_frame?.CanGoBack == true)
            _frame.GoBack();
    }
}
