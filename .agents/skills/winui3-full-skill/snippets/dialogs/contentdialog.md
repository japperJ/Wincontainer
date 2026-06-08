# ContentDialog

`ContentDialog` is a modal dialog that blocks the parent window until dismissed.
Use it for confirmations, form inputs, warnings, and any action that requires a decision.

---

## Basic Confirmation Dialog

```xaml
<!-- No XAML needed for programmatic dialogs; shown from code -->
```

```csharp
// Called from code-behind
private async Task<bool> ConfirmDeleteAsync()
{
    var dialog = new ContentDialog
    {
        Title = "Delete item?",
        Content = "This action cannot be undone.",
        PrimaryButtonText = "Delete",
        CloseButtonText = "Cancel",
        DefaultButton = ContentDialogButton.Close,
        XamlRoot = this.XamlRoot  // Required in WinUI 3
    };

    var result = await dialog.ShowAsync();
    return result == ContentDialogResult.Primary;
}
```

---

## Dialog with Form Content

```xaml
<!-- ContentDialog defined as a resource or inline in XAML -->
<ContentDialog
    x:Name="AddItemDialog"
    Title="Add new item"
    PrimaryButtonText="Add"
    CloseButtonText="Cancel"
    DefaultButton="Primary"
    PrimaryButtonClick="AddItemDialog_PrimaryButtonClick">
    <StackPanel Spacing="12" Width="280">
        <TextBox
            x:Name="ItemNameBox"
            Header="Name"
            PlaceholderText="Enter name"
            AutomationProperties.Name="Item name" />
        <TextBox
            x:Name="ItemDescBox"
            Header="Description"
            PlaceholderText="Optional description"
            AutomationProperties.Name="Item description" />
    </StackPanel>
</ContentDialog>
```

```csharp
// MainPage.xaml.cs
private async void ShowAddDialog_Click(object sender, RoutedEventArgs e)
{
    AddItemDialog.XamlRoot = this.XamlRoot;
    var result = await AddItemDialog.ShowAsync();
    // result handled in AddItemDialog_PrimaryButtonClick
}

private void AddItemDialog_PrimaryButtonClick(ContentDialog sender,
    ContentDialogButtonClickEventArgs args)
{
    if (string.IsNullOrWhiteSpace(ItemNameBox.Text))
    {
        args.Cancel = true;  // Prevent dialog from closing
        ItemNameBox.Focus(FocusState.Programmatic);
        return;
    }
    ViewModel.AddItem(ItemNameBox.Text, ItemDescBox.Text);
}
```

---

## MVVM Pattern — Async Dialog from ViewModel via Service

```xaml
<!-- View — nothing special, just a button -->
<Button
    Content="Delete"
    Command="{x:Bind ViewModel.DeleteCommand}"
    AutomationProperties.Name="Delete item" />
```

```csharp
// Services/IDialogService.cs
namespace MyApp.Services;

public interface IDialogService
{
    Task<bool> ConfirmAsync(string title, string message);
    Task ShowErrorAsync(string title, string message);
}
```

```csharp
// Services/DialogService.cs
using Microsoft.UI.Xaml.Controls;

namespace MyApp.Services;

public class DialogService : IDialogService
{
    private readonly Window _window;

    public DialogService(Window window)
    {
        _window = window;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            PrimaryButtonText = "Yes",
            CloseButtonText = "No",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _window.Content.XamlRoot
        };
        var result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }

    public async Task ShowErrorAsync(string title, string message)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = message,
            CloseButtonText = "OK",
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = _window.Content.XamlRoot
        };
        await dialog.ShowAsync();
    }
}
```

```csharp
// ViewModels/ItemsViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MyApp.ViewModels;

public partial class ItemsViewModel : ObservableObject
{
    private readonly IDialogService _dialog;
    private readonly IItemService _items;

    [ObservableProperty]
    private MyItem? _selectedItem;

    public ItemsViewModel(IDialogService dialog, IItemService items)
    {
        _dialog = dialog;
        _items = items;
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private async Task DeleteAsync(CancellationToken ct)
    {
        if (SelectedItem is null) return;
        var confirmed = await _dialog.ConfirmAsync(
            "Delete item?",
            $"Are you sure you want to delete \"{SelectedItem.Name}\"?");

        if (confirmed)
            await _items.DeleteAsync(SelectedItem.Id, ct);
    }

    private bool HasSelection() => SelectedItem is not null;
}
```

---

## Error / Warning Dialog

```csharp
// With InfoBar-style severity icon via custom content
var dialog = new ContentDialog
{
    Title = "Connection failed",
    Content = new StackPanel
    {
        Spacing = 8,
        Children =
        {
            new InfoBar
            {
                Severity = InfoBarSeverity.Error,
                Title = "Network error",
                Message = "Could not reach the server. Check your connection.",
                IsOpen = true,
                IsClosable = false
            }
        }
    },
    CloseButtonText = "OK",
    XamlRoot = this.XamlRoot
};
await dialog.ShowAsync();
```

---

## Notes

- **Always** set `XamlRoot = this.XamlRoot` (or `window.Content.XamlRoot`) before calling
  `ShowAsync()` — this is required in WinUI 3 (unlike UWP).
- Only one `ContentDialog` can be open at a time. Queue them if needed.
- `args.Cancel = true` inside a button click handler prevents the dialog from closing —
  use this for form validation.
- `DefaultButton` sets which button responds to Enter; `CloseButtonText` always maps to Escape.
- For non-blocking notifications, use `InfoBar` or `TeachingTip` instead.
- `ContentDialogResult`: `None` (close button), `Primary`, `Secondary`.
