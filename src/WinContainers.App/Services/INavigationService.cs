namespace WinContainers_App.Services;

public interface INavigationService
{
    bool CanGoBack { get; }
    void SetFrame(Frame frame);
    void NavigateTo<TPage>() where TPage : Page;
    void NavigateTo<TPage>(object parameter) where TPage : Page;
    void GoBack();
}
