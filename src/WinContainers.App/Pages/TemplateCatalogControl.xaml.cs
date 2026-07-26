using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using WinContainers_App.Services;
using WinContainers_App.ViewModels;
using ServiceLogLevel = WinContainers_App.Services.LogLevel;

namespace WinContainers_App.Pages;

public sealed partial class TemplateCatalogControl : UserControl
{
    private readonly QuickActionsViewModel _viewModel;

    public event EventHandler? UseTemplateRequested;

    public TemplateCatalogControl()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.QuickActionsViewModel;
        DataContext = _viewModel;
    }

    private void CategoryChip_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Content is string category)
            _viewModel.SelectedCategory = category;
    }

    private void TemplateCatalogList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TemplateCatalogList.SelectedItem is not TemplateCatalogItem template)
            return;

        _viewModel.SelectedTemplate = template;
        TemplateNameText.Text = template.Name;
        TemplateDescriptionText.Text = template.Description;
        TemplateSummaryText.Text = $"{template.Image} • {template.Category}";
        WebsiteButton.NavigateUri = new Uri(template.Website);
        TemplateDetailsPanel.Visibility = Visibility.Visible;

        if (_viewModel.MetadataLoadStatus is { } status)
        {
            MetadataInfoBar.Message = status;
            MetadataInfoBar.IsOpen = true;
        }
        else
        {
            MetadataInfoBar.IsOpen = false;
        }
    }

    private async void UseTemplateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_viewModel.SelectedTemplate is null)
            return;

        await _viewModel.ApplyTemplateAsync(_viewModel.SelectedTemplate);
        OutputService.Instance.Write($"Template '{_viewModel.SelectedTemplate.Name}' loaded into Create Container.");
        UseTemplateRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void RefreshCatalogButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshCatalogButton.IsEnabled = false;
            RefreshCatalogButton.Content = "↻ Refreshing...";
            await _viewModel.RefreshCatalogAsync();
        }
        catch (Exception ex)
        {
            OutputService.Instance.Write($"Refresh catalog failed: {ex}", ServiceLogLevel.Error);
        }
        finally
        {
            RefreshCatalogButton.IsEnabled = true;
            RefreshCatalogButton.Content = "↻ Refresh";
        }
    }

    private async void MetadataInfoButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is TemplateCatalogItem template && template.Metadata is { } metadata)
        {
            PopulateMetadataDialog(metadata);
            MetadataDialog.XamlRoot = btn.XamlRoot;
            await MetadataDialog.ShowAsync();
        }
    }

    private void PopulateMetadataDialog(TemplateMetadataEntry metadata)
    {
        MetadataDialogContent.Children.Clear();

        AddAccessSection(metadata);
        AddCredentialsSection(metadata);
        AddVolumesSection(metadata);
        AddSetupNotesSection(metadata);
        AddDocumentationSection(metadata);
        AddVerificationSection(metadata);
    }

    private void AddAccessSection(TemplateMetadataEntry metadata)
    {
        var hasUrls = metadata.Access.Urls.Count > 0;
        var hasPorts = metadata.Access.Ports.Count > 0;
        if (!hasUrls && !hasPorts) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Access"));

        if (hasUrls)
        {
            foreach (var url in metadata.Access.Urls)
            {
                var link = new HyperlinkButton { Content = url, NavigateUri = new Uri(url) };
                MetadataDialogContent.Children.Add(link);
            }
        }

        if (hasPorts)
        {
            foreach (var port in metadata.Access.Ports)
            {
                var text = new TextBlock
                {
                    Text = $"{port.Service}: {port.Host}:{port.Container}/{port.Protocol}",
                    FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                    FontSize = 13,
                    Margin = new Thickness(0, 2, 0, 2)
                };
                MetadataDialogContent.Children.Add(text);
            }
        }
    }

    private void AddCredentialsSection(TemplateMetadataEntry metadata)
    {
        if (metadata.Credentials.Count == 0) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Credentials"));

        foreach (var cred in metadata.Credentials)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 2, 0, 2) };

            panel.Children.Add(new TextBlock
            {
                Text = $"{cred.Service}.{cred.Name}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 13,
                VerticalAlignment = VerticalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = $"({cred.Provenance})",
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Gray),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            });

            if (cred.InsecureDemoDefault)
            {
                panel.Children.Add(new TextBlock
                {
                    Text = "⚠ Insecure demo default",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.Red),
                    FontSize = 12,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            MetadataDialogContent.Children.Add(panel);
        }
    }

    private void AddVolumesSection(TemplateMetadataEntry metadata)
    {
        if (metadata.Volumes.Count == 0) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Volumes"));

        foreach (var vol in metadata.Volumes)
        {
            var text = new TextBlock
            {
                Text = $"{vol.Service}: {vol.Source} → {vol.Target}{(vol.ReadOnly ? " (read-only)" : "")}",
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Consolas"),
                FontSize = 13,
                Margin = new Thickness(0, 2, 0, 2)
            };
            MetadataDialogContent.Children.Add(text);

            if (vol.Source.StartsWith('/') || vol.Source.StartsWith('.'))
            {
                MetadataDialogContent.Children.Add(new TextBlock
                {
                    Text = "⚠ Host path mount — review for security",
                    Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.DarkOrange),
                    FontSize = 12,
                    FontStyle = Windows.UI.Text.FontStyle.Italic,
                    Margin = new Thickness(16, 0, 0, 4)
                });
            }
        }
    }

    private void AddSetupNotesSection(TemplateMetadataEntry metadata)
    {
        if (metadata.SetupNotes.Count == 0) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Setup Notes"));

        for (var i = 0; i < metadata.SetupNotes.Count; i++)
        {
            var note = metadata.SetupNotes[i];
            var isWarning = note.StartsWith("⚠") || note.Contains("demo credential", StringComparison.OrdinalIgnoreCase) || note.Contains("host path", StringComparison.OrdinalIgnoreCase);

            MetadataDialogContent.Children.Add(new TextBlock
            {
                Text = $"{i + 1}. {note}",
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                Foreground = isWarning
                    ? new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.DarkOrange)
                    : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 2, 0, 2)
            });
        }
    }

    private void AddDocumentationSection(TemplateMetadataEntry metadata)
    {
        if (metadata.DocumentationUrls.Count == 0) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Documentation"));

        foreach (var url in metadata.DocumentationUrls)
        {
            MetadataDialogContent.Children.Add(new HyperlinkButton
            {
                Content = url,
                NavigateUri = new Uri(url)
            });
        }
    }

    private void AddVerificationSection(TemplateMetadataEntry metadata)
    {
        var status = metadata.Verification.Status;
        if (string.IsNullOrWhiteSpace(status)) return;

        MetadataDialogContent.Children.Add(CreateSectionHeader("Verification"));

        var statusColor = status switch
        {
            "verified" => Colors.Green,
            "partially_verified" => Colors.DarkOrange,
            _ => Colors.Gray
        };

        var statusLabel = status switch
        {
            "verified" => "✓ Verified",
            "partially_verified" => "◐ Partially verified",
            "unknown" => "? Unknown",
            _ => status
        };

        var badge = new Border
        {
            Background = new Microsoft.UI.Xaml.Media.SolidColorBrush(statusColor),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(8, 4, 8, 4),
            HorizontalAlignment = HorizontalAlignment.Left,
            Child = new TextBlock
            {
                Text = statusLabel,
                Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Colors.White),
                FontSize = 12,
                FontWeight = Microsoft.UI.Text.FontWeights.SemiBold
            }
        };
        MetadataDialogContent.Children.Add(badge);

        if (!string.IsNullOrWhiteSpace(metadata.Verification.Source))
        {
            MetadataDialogContent.Children.Add(new TextBlock
            {
                Text = $"Source: {metadata.Verification.Source}",
                FontSize = 12,
                Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["TextFillColorSecondaryBrush"],
                Margin = new Thickness(0, 4, 0, 0)
            });
        }
    }

    private static TextBlock CreateSectionHeader(string text)
    {
        return new TextBlock
        {
            Text = text,
            Style = (Style)Application.Current.Resources["SubtitleTextBlockStyle"],
            Margin = new Thickness(0, 8, 0, 4)
        };
    }
}
