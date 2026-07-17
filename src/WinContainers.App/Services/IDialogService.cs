namespace WinContainers_App.Services;

public interface IDialogService
{
    Task<ContentDialogResult> ShowMessageAsync(string title, string content, string closeButtonText = "OK");
    Task<ContentDialogResult> ShowConfirmAsync(string title, string content, string primaryButtonText = "Confirm", string closeButtonText = "Cancel");
    Task<ContentDialogResult> ShowYesNoCancelAsync(string title, string content, string primaryText = "Yes", string secondaryText = "No", string closeText = "Cancel");
    Task<string?> ShowInputAsync(string title, string defaultText, string placeholder);
}
