using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class ContainersControl : UserControl
{
    private readonly ContainersViewModel _viewModel;

    public ContainersControl()
    {
        InitializeComponent();

        _viewModel = ViewModelLocator.ContainersViewModel;

        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ContainersViewModel.ContainerItems))
                ContainerListView.ItemsSource = _viewModel.ContainerItems;
        };
        ContainerListView.ItemsSource = _viewModel.ContainerItems;

        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _viewModel.StartPolling();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        _viewModel.StopPolling();
    }

    private void GroupHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is ContainerGroup group)
            _viewModel.ToggleGroupExpanded(group);
    }

    private async void StartContainer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerCardData c } btn) return;
        using var guard = new ButtonGuard(btn);
        await _viewModel.RunContainerActionAsync("Start", c.Id);
    }

    private async void StopContainer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerCardData c } btn) return;
        using var guard = new ButtonGuard(btn);
        await _viewModel.RunContainerActionAsync("Stop", c.Id);
    }

    private async void RemoveContainer_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerCardData c } btn) return;
        using var guard = new ButtonGuard(btn);

        var volumeNames = await GetContainerVolumeNamesAsync(c.Id);
        var removeVolumes = false;

        if (volumeNames.Count > 0)
        {
            var volList = string.Join("\n  • ", volumeNames);
            var dialog = new ContentDialog
            {
                Title = "Remove volumes?",
                Content = $"This container has {volumeNames.Count} attached volume(s):\n  • {volList}\n\nRemove them along with the container?",
                PrimaryButtonText = "Remove both",
                SecondaryButtonText = "Container only",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.None)
                return;
            removeVolumes = result == ContentDialogResult.Primary;
        }

        await _viewModel.RunContainerActionAsync("Remove", c.Id,
            volumesToRemove: removeVolumes ? volumeNames : null);
    }

    private async Task<List<string>> GetContainerVolumeNamesAsync(string id)
    {
        try
        {
            var json = await App.ServiceClient.InspectContainerAsync(id);
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
                return [];

            var mounts = doc.RootElement[0].TryGetProperty("Mounts", out var m) ? m : default;
            if (mounts.ValueKind != JsonValueKind.Array)
                return [];

            var names = new List<string>();
            foreach (var mount in mounts.EnumerateArray())
            {
                if (mount.TryGetProperty("Type", out var type) &&
                    type.GetString() == "volume" &&
                    mount.TryGetProperty("Name", out var name))
                {
                    names.Add(name.GetString()!);
                }
            }
            return names;
        }
        catch
        {
            return [];
        }
    }

    private async void StartGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerGroup group } btn) return;
        using var guard = new GroupButtonGuard(btn, group);
        await _viewModel.RunGroupActionAsync("Start", group);
    }

    private async void StopGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerGroup group } btn) return;
        using var guard = new GroupButtonGuard(btn, group);
        await _viewModel.RunGroupActionAsync("Stop", group);
    }

    private async void RemoveGroup_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ContainerGroup group } btn) return;
        using var guard = new GroupButtonGuard(btn, group);
        await _viewModel.RunGroupActionAsync("Remove", group);
    }

    private void ContainerRow_Tapped(object sender, TappedRoutedEventArgs e)
    {
        var original = e.OriginalSource as DependencyObject;
        while (original != null)
        {
            if (original is Button)
                return;
            original = VisualTreeHelper.GetParent(original);
        }

        if (sender is Border { DataContext: ContainerCardData entry })
            _viewModel.NavigateToDetail(entry);
    }

    private async void ContainerRename_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string id) return;
        var container = _viewModel.FindContainer(id);
        if (container is null) return;

        var (confirmed, newName) = await ShowRenameDialogAsync("Rename container", "New container name:", container.Name);
        if (!confirmed || string.IsNullOrWhiteSpace(newName)) return;

        using var guard = new ButtonGuard(button);
        await _viewModel.RunContainerActionAsync("Rename", id, newName);
    }

    private async void GroupRename_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.DataContext is not ContainerGroup group) return;

        var (confirmed, newName) = await ShowRenameDialogAsync("Rename project", "New project name:", group.DisplayName);
        if (!confirmed || string.IsNullOrWhiteSpace(newName)) return;

        using var guard = new GroupButtonGuard(button, group);
        await _viewModel.RunGroupRenameAsync(newName, group);
    }

    private async Task<(bool confirmed, string? text)> ShowRenameDialogAsync(string title, string label, string currentValue)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
            Content = new StackPanel
            {
                Spacing = 8,
                Children =
                {
                    new TextBlock { Text = label },
                    new TextBox { Text = currentValue }
                }
            }
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
            return (false, null);

        var textBox = (dialog.Content as StackPanel)?.Children.OfType<TextBox>().FirstOrDefault();
        var newName = textBox?.Text?.Trim();
        return (true, newName);
    }

    private void PortLink_Click(object sender, RoutedEventArgs e)
    {
        if (sender is HyperlinkButton button && button.Content is string portStr)
        {
            var url = $"http://{portStr}/";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
    }

    private sealed class ButtonGuard : IDisposable
    {
        private readonly Button _btn;
        private readonly object _origContent;

        public ButtonGuard(Button btn)
        {
            _btn = btn;
            _origContent = btn.Content;
            btn.Content = "◎";
        }

        public void Dispose()
        {
            _btn.Content = _origContent;
        }
    }

    private sealed class GroupButtonGuard : IDisposable
    {
        private readonly Button _btn;
        private readonly object _origContent;

        public GroupButtonGuard(Button btn, ContainerGroup group)
        {
            _btn = btn;
            _origContent = btn.Content;
            btn.Content = "◎";
        }

        public void Dispose()
        {
            _btn.Content = _origContent;
        }
    }
}
