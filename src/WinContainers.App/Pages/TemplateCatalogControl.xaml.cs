using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
}
