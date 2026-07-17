using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using WinContainers.Runtime.Models;
using WinContainers_App.ViewModels;

namespace WinContainers_App.Pages;

public sealed class TreeViewItemWrapper
{
    public string DisplayText { get; set; }
    public TerminalCommand? Command { get; set; }

    public TreeViewItemWrapper(string text, TerminalCommand? cmd = null)
    {
        DisplayText = text;
        Command = cmd;
    }

    public override string ToString() => DisplayText;
}

public sealed partial class TerminalPage : Page
{
    private readonly TerminalViewModel _viewModel;

    public TerminalViewModel ViewModel => _viewModel;

    public TerminalPage()
    {
        InitializeComponent();
        _viewModel = ViewModelLocator.TerminalViewModel;
        _viewModel.PropertyChanged += async (_, e) =>
        {
            if (e.PropertyName == nameof(TerminalViewModel.HasSelectedCommand) && _viewModel.HasSelectedCommand)
            {
                await _viewModel.InitializeAsync();
                BuildParamFields();
            }
        };
        _viewModel.History.CollectionChanged += (_, _) => RebuildHistory();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        BuildCommandTree();
        _viewModel.LoadHistory();
    }

    private void BuildCommandTree()
    {
        CommandTree.RootNodes.Clear();

        foreach (var category in _viewModel.Categories)
        {
            var catNode = new TreeViewNode
            {
                Content = new TreeViewItemWrapper(category.Name),
                IsExpanded = true
            };

            foreach (var command in category.Commands)
            {
                var cmdNode = new TreeViewNode
                {
                    Content = new TreeViewItemWrapper(command.DisplayName, command)
                };
                catNode.Children.Add(cmdNode);
            }

            CommandTree.RootNodes.Add(catNode);
        }
    }

    private void CommandTree_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (sender.SelectedNode?.Content is TreeViewItemWrapper wrapper && wrapper.Command is not null)
            _viewModel.SelectedCommand = wrapper.Command;
    }

    private void BuildParamFields()
    {
        ParamsPanel.Children.Clear();

        foreach (var param in _viewModel.ParameterValues)
        {
            var label = new TextBlock
            {
                Text = param.DisplayName,
                Style = Application.Current.Resources["BodyTextBlockStyle"] as Style
            };

            if (param.IsDropdown)
            {
                var combo = new ComboBox
                {
                    ItemsSource = param.Options,
                    PlaceholderText = "Select...",
                    MinWidth = 200,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                AutomationProperties.SetName(combo, param.DisplayName);
                combo.SelectionChanged += (_, _) =>
                {
                    if (combo.SelectedItem is string s)
                        param.Value = s;
                };
                if (param.Value is not null)
                    combo.SelectedItem = param.Value;
                ParamsPanel.Children.Add(label);
                ParamsPanel.Children.Add(combo);
            }
            else
            {
                var textBox = new TextBox
                {
                    Text = param.Value ?? "",
                    PlaceholderText = param.DisplayName,
                    MinWidth = 200,
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                textBox.TextChanged += (_, _) => param.Value = textBox.Text;
                ParamsPanel.Children.Add(label);
                ParamsPanel.Children.Add(textBox);
            }
        }
    }

    private void RebuildHistory()
    {
        try { HistoryList.Items.Clear(); } catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }

        foreach (var entry in _viewModel.History)
        {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var reRunBtn = new Button
            {
                Content = "Re-run",
                Margin = new Thickness(4, 0, 4, 0),
                Tag = entry
            };
            reRunBtn.Click += (_, _) => _viewModel.ReRunCommand.Execute(entry);

            var favBtn = new Button
            {
                Margin = new Thickness(4, 0, 0, 0),
                Tag = entry
            };
            var favText = new TextBlock();
            UpdateFavText(favText, entry.IsFavorite);
            favBtn.Content = favText;
            favBtn.Click += (_, _) =>
            {
                _viewModel.ToggleFavoriteCommand.Execute(entry);
                UpdateFavText(favText, entry.IsFavorite);
            };

            var nameText = new TextBlock
            {
                Text = entry.ScriptName,
                Style = Application.Current.Resources["BodyStrongTextBlockStyle"] as Style,
                VerticalAlignment = VerticalAlignment.Center
            };
            var timeText = new TextBlock
            {
                Text = entry.Timestamp.ToString("g"),
                Style = Application.Current.Resources["CaptionTextBlockStyle"] as Style,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };

            Grid.SetColumn(nameText, 0);
            Grid.SetColumn(timeText, 1);
            Grid.SetColumn(reRunBtn, 2);
            Grid.SetColumn(favBtn, 3);

            grid.Children.Add(nameText);
            grid.Children.Add(timeText);
            grid.Children.Add(reRunBtn);
            grid.Children.Add(favBtn);

            HistoryList.Items.Add(grid);
        }
    }

    private static void UpdateFavText(TextBlock tb, bool isFav)
    {
        tb.Text = isFav ? "\u2605" : "\u2606";
    }

    private void CopyCommandButton_Click(object sender, RoutedEventArgs e)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(_viewModel.CommandPreview))
            lines.Add(_viewModel.CommandPreview);
        if (!string.IsNullOrWhiteSpace(_viewModel.WslcCommandPreview))
            lines.Add(_viewModel.WslcCommandPreview);

        if (lines.Count > 0)
        {
            var pkg = new Windows.ApplicationModel.DataTransfer.DataPackage();
            pkg.SetText(string.Join(Environment.NewLine, lines));
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pkg);
        }
    }
}
