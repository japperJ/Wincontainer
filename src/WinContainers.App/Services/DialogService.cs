namespace WinContainers_App.Services;

public sealed class DialogService : IDialogService
{
    private readonly Func<XamlRoot?> _xamlRootProvider;

    public DialogService(Func<XamlRoot?> xamlRootProvider)
    {
        _xamlRootProvider = xamlRootProvider;
    }

    public async Task<ContentDialogResult> ShowMessageAsync(string title, string content, string closeButtonText = "OK")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _xamlRootProvider()
        };
        return await dialog.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowConfirmAsync(string title, string content, string primaryButtonText = "Confirm", string closeButtonText = "Cancel")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryButtonText,
            CloseButtonText = closeButtonText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _xamlRootProvider()
        };
        return await dialog.ShowAsync();
    }

    public async Task<ContentDialogResult> ShowYesNoCancelAsync(string title, string content, string primaryText = "Yes", string secondaryText = "No", string closeText = "Cancel")
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = content,
            PrimaryButtonText = primaryText,
            SecondaryButtonText = secondaryText,
            CloseButtonText = closeText,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _xamlRootProvider()
        };
        return await dialog.ShowAsync();
    }

    public async Task<string?> ShowInputAsync(string title, string defaultText, string placeholder)
    {
        var textBox = new TextBox
        {
            Text = defaultText,
            PlaceholderText = placeholder,
            MinWidth = 300
        };

        var dialog = new ContentDialog
        {
            Title = title,
            Content = textBox,
            PrimaryButtonText = "OK",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = _xamlRootProvider()
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            var text = textBox.Text.Trim();
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        return null;
    }
}
