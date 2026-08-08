using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinContainers.AI;
using WinContainers_App.Models;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed partial class AiPage : Page
{
    private readonly AiViewModel _viewModel;
    private readonly MarkdownFormatter _markdown = new();

    public AiPage()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.AiViewModel;
        MessageList.ItemsSource = _viewModel.Messages;

        _viewModel.Messages.CollectionChanged += (_, _) => ScrollToBottom();
        _viewModel.PropertyChanged += (_, _) => UpdateUiState();

        Loaded += AiPage_Loaded;
    }

    private async void AiPage_Loaded(object sender, RoutedEventArgs e)
    {
        _viewModel.Initialize();
        UpdateUiState();
        if (!_viewModel.HasConfiguredProvider)
        {
            await ShowSetupDialogAsync();
        }
    }

    private void UpdateUiState()
    {
        SendButton.IsEnabled = _viewModel.CanSend;
        CancelButton.Visibility = _viewModel.IsCancellable ? Visibility.Visible : Visibility.Collapsed;
        ClearButton.IsEnabled = _viewModel.CanClear;
        CopyButton.IsEnabled = _viewModel.Messages.Count > 0;
        ProviderStatusText.Text = _viewModel.ProviderStatus;
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var text = InputBox.Text;
        InputBox.Text = string.Empty;
        _viewModel.Input = text;
        await _viewModel.SendAsync();
        UpdateUiState();
        RefreshMarkdown();
    }

    private void InputBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key != Windows.System.VirtualKey.Enter)
        {
            return;
        }

        e.Handled = true;
        if (!_viewModel.CanSend)
        {
            return;
        }

        _viewModel.Input = InputBox.Text;
        _ = SendAsyncCore();
    }

    private async Task SendAsyncCore()
    {
        var text = InputBox.Text;
        InputBox.Text = string.Empty;
        _viewModel.Input = text;
        await _viewModel.SendAsync();
        UpdateUiState();
        RefreshMarkdown();
    }

    private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _viewModel.Input = InputBox.Text;
        UpdateUiState();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => _viewModel.CancelTurn();

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearConversation();
        UpdateUiState();
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e) => _viewModel.CopyAsMarkdown();

    private async void SettingsButton_Click(object sender, RoutedEventArgs e) => await ShowSetupDialogAsync();

    private void AssistantText_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock
            && textBlock.DataContext is AssistantChatMessage message
            && message.IsComplete)
        {
            RenderMarkdown(textBlock, message);
        }
    }

    private void RefreshMarkdown()
    {
        for (var i = 0; i < _viewModel.Messages.Count; i++)
        {
            if (_viewModel.Messages[i] is not AssistantChatMessage message
                || !message.IsComplete
                || message.MarkdownRendered)
            {
                continue;
            }

            if (MessageList.ContainerFromIndex(i) is ListViewItem item
                && FindDescendant<TextBlock>(item) is TextBlock textBlock
                && ReferenceEquals(textBlock.DataContext, message))
            {
                RenderMarkdown(textBlock, message);
            }
        }
    }

    private void RenderMarkdown(TextBlock textBlock, AssistantChatMessage message)
    {
        // Setting Text clears the Inlines collection in WinUI, so clear Text
        // first and then populate Inlines for the formatted markdown.
        textBlock.Text = string.Empty;
        textBlock.Inlines.Clear();
        try
        {
            foreach (var inline in _markdown.Format(message.Text))
            {
                textBlock.Inlines.Add(inline);
            }
        }
        catch
        {
            // Never let a formatting failure hide the reply. Show plain text.
            textBlock.Inlines.Clear();
            textBlock.Text = message.Text;
        }

        message.MarkdownRendered = true;
    }

    private void ScrollToBottom()
    {
        if (MessageList.Items.Count == 0)
        {
            return;
        }

        MessageList.ScrollIntoView(MessageList.Items[^1]);
    }

    private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is T nested)
            {
                return nested;
            }
        }

        return null;
    }

    private async Task ShowSetupDialogAsync()
    {
        var providerCombo = new ComboBox { Header = "Provider", Width = 320 };
        providerCombo.Items.Add(new ComboBoxItem { Content = "OpenAI-compatible endpoint", Tag = "external" });
        providerCombo.Items.Add(new ComboBoxItem { Content = "Local Ollama", Tag = "ollama" });
        providerCombo.SelectedIndex = 0;

        var endpointBox = new TextBox
        {
            Header = "Endpoint",
            Text = "https://api.openai.com/v1",
            PlaceholderText = "https://.../v1",
            Width = 320
        };
        var modelBox = new TextBox { Header = "Model", Text = "gpt-4o-mini", Width = 320 };
        var keyBox = new PasswordBox
        {
            Header = "API key",
            PlaceholderText = "sk-... (leave empty for local Ollama)",
            Width = 320
        };

        var ollamaStatus = new TextBlock
        {
            Text = "Select a provider to begin.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = (Brush)Application.Current.Resources["TextFillColorSecondaryBrush"]
        };
        var detectButton = new Button { Content = "Detect local Ollama", IsEnabled = false };
        var installButton = new Button { Content = "Install Ollama container", IsEnabled = false };
        var installProgress = new ProgressBar { IsIndeterminate = true, Visibility = Visibility.Collapsed };

        var externalPanel = new StackPanel { Spacing = 8, Visibility = Visibility.Visible };
        externalPanel.Children.Add(endpointBox);
        externalPanel.Children.Add(modelBox);
        externalPanel.Children.Add(keyBox);

        var ollamaPanel = new StackPanel { Spacing = 8, Visibility = Visibility.Collapsed };
        ollamaPanel.Children.Add(ollamaStatus);
        ollamaPanel.Children.Add(detectButton);
        ollamaPanel.Children.Add(installButton);
        ollamaPanel.Children.Add(installProgress);

        var content = new StackPanel { Spacing = 12, Width = 360 };
        content.Children.Add(new TextBlock
        {
            Text = "Connect the AI assistant to a model provider. You can change this later in Settings.",
            TextWrapping = TextWrapping.Wrap
        });
        content.Children.Add(providerCombo);
        content.Children.Add(externalPanel);
        content.Children.Add(ollamaPanel);

        providerCombo.SelectionChanged += (_, _) =>
        {
            var isOllama = (providerCombo.SelectedItem as ComboBoxItem)?.Tag as string == "ollama";
            externalPanel.Visibility = isOllama ? Visibility.Collapsed : Visibility.Visible;
            ollamaPanel.Visibility = isOllama ? Visibility.Visible : Visibility.Collapsed;
            if (isOllama)
            {
                _ = ProbeOllamaAsync(ollamaStatus, detectButton, installButton);
            }
        };

        detectButton.Click += async (_, _) => await ProbeOllamaAsync(ollamaStatus, detectButton, installButton);

        installButton.Click += async (_, _) =>
        {
            installButton.IsEnabled = false;
            detectButton.IsEnabled = false;
            installProgress.Visibility = Visibility.Visible;
            ollamaStatus.Text = "Installing the Ollama container and pulling the default model. This can take a few minutes.";
            try
            {
                await _viewModel.InstallOllamaAsync(CancellationToken.None);
                _viewModel.ApplyConfig(new AiProviderConfig
                {
                    Kind = AiProviderKind.Ollama,
                    Endpoint = "http://localhost:11434/v1",
                    Model = AiChatService.DefaultOllamaModel,
                });
                ollamaStatus.Text = "Ollama installed. You can now chat with the local model.";
                UpdateUiState();
            }
            catch (Exception ex)
            {
                ollamaStatus.Text = $"Installation failed: {ex.Message}";
            }
            finally
            {
                installProgress.Visibility = Visibility.Collapsed;
            }
        };

        var dialog = new ContentDialog
        {
            Title = "AI assistant setup",
            Content = content,
            PrimaryButtonText = "Save",
            CloseButtonText = "Later",
            DefaultButton = ContentDialogButton.Primary,
            XamlRoot = XamlRoot,
        };

        var result = await dialog.ShowAsync();
        if (result != ContentDialogResult.Primary)
        {
            UpdateUiState();
            return;
        }

        var isOllamaSelected = (providerCombo.SelectedItem as ComboBoxItem)?.Tag as string == "ollama";
        var config = isOllamaSelected
            ? new AiProviderConfig
            {
                Kind = AiProviderKind.Ollama,
                Endpoint = "http://localhost:11434/v1",
                Model = AiChatService.DefaultOllamaModel,
            }
            : new AiProviderConfig
            {
                Kind = AiProviderKind.OpenAiCompatible,
                Endpoint = string.IsNullOrWhiteSpace(endpointBox.Text) ? "https://api.openai.com/v1" : endpointBox.Text.Trim(),
                Model = string.IsNullOrWhiteSpace(modelBox.Text) ? "gpt-4o-mini" : modelBox.Text.Trim(),
                ApiKey = string.IsNullOrWhiteSpace(keyBox.Password) ? null : keyBox.Password,
            };

        _viewModel.ApplyConfig(config);
        UpdateUiState();
    }

    private static async Task ProbeOllamaAsync(TextBlock status, Button detectButton, Button installButton)
    {
        detectButton.IsEnabled = false;
        status.Text = "Checking for a local Ollama server...";
        var running = await ViewModelLocator.AiViewModel.DetectOllamaAsync();
        installButton.IsEnabled = !running;
        status.Text = running
            ? "Local Ollama server found. The assistant will use qwen2.5:3b."
            : "No local Ollama server found. Install it as a container with the button below.";
        detectButton.IsEnabled = true;
    }
}
