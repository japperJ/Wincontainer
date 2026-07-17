# AutoSuggestBox

`AutoSuggestBox` is a text input that shows a drop-down suggestion list as the user types.
Use it for search boxes, address bars, tag pickers, and autocomplete fields.

---

## Basic AutoSuggestBox

```xaml
<AutoSuggestBox
    Width="300"
    PlaceholderText="Search…"
    QueryIcon="Find"
    TextChanged="AutoSuggestBox_TextChanged"
    SuggestionChosen="AutoSuggestBox_SuggestionChosen"
    QuerySubmitted="AutoSuggestBox_QuerySubmitted"
    AutomationProperties.Name="Search" />
```

```csharp
// MainPage.xaml.cs
private static readonly List<string> _allItems = new()
{
    "Apple", "Apricot", "Avocado", "Banana", "Blueberry",
    "Cherry", "Coconut", "Date", "Elderberry", "Fig"
};

private void AutoSuggestBox_TextChanged(AutoSuggestBox sender,
    AutoSuggestBoxTextChangedEventArgs args)
{
    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
    {
        var query = sender.Text.ToLowerInvariant();
        sender.ItemsSource = string.IsNullOrEmpty(query)
            ? _allItems
            : _allItems.Where(i => i.ToLowerInvariant().Contains(query)).ToList();
    }
}

private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender,
    AutoSuggestBoxSuggestionChosenEventArgs args)
{
    sender.Text = args.SelectedItem?.ToString() ?? string.Empty;
}

private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender,
    AutoSuggestBoxQuerySubmittedEventArgs args)
{
    var chosen = args.ChosenSuggestion?.ToString() ?? args.QueryText;
    // Navigate or filter based on 'chosen'
}
```

---

## With Custom Suggestion Template

```xaml
<AutoSuggestBox
    Width="320"
    PlaceholderText="Search contacts…"
    DisplayMemberPath="Name"
    TextMemberPath="Name"
    TextChanged="ContactSearch_TextChanged"
    AutomationProperties.Name="Contact search">
    <AutoSuggestBox.ItemTemplate>
        <DataTemplate x:DataType="local:Contact">
            <StackPanel Orientation="Horizontal" Spacing="8" Padding="4">
                <PersonPicture
                    Width="32"
                    Height="32"
                    DisplayName="{x:Bind Name}" />
                <StackPanel>
                    <TextBlock Text="{x:Bind Name}" Style="{ThemeResource BodyStrongTextBlockStyle}" />
                    <TextBlock Text="{x:Bind Email}" Style="{ThemeResource CaptionTextBlockStyle}"
                               Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                </StackPanel>
            </StackPanel>
        </DataTemplate>
    </AutoSuggestBox.ItemTemplate>
</AutoSuggestBox>
```

---

## MVVM — Full ViewModel Implementation

```xaml
<!-- View -->
<AutoSuggestBox
    Width="300"
    PlaceholderText="Search…"
    QueryIcon="Find"
    ItemsSource="{x:Bind ViewModel.Suggestions, Mode=OneWay}"
    Text="{x:Bind ViewModel.SearchText, Mode=TwoWay}"
    TextChanged="Search_TextChanged"
    SuggestionChosen="Search_SuggestionChosen"
    AutomationProperties.Name="Search" />
```

```csharp
// MainPage.xaml.cs — minimal event bridge
private void Search_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
{
    if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
        ViewModel.UpdateSuggestions(sender.Text);
}

private void Search_SuggestionChosen(AutoSuggestBox sender,
    AutoSuggestBoxSuggestionChosenEventArgs args)
{
    ViewModel.SelectSuggestion(args.SelectedItem?.ToString() ?? string.Empty);
}
```

```csharp
// ViewModels/SearchViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    private readonly ISearchService _searchService;

    [ObservableProperty]
    private string _searchText = string.Empty;

    public ObservableCollection<string> Suggestions { get; } = new();

    public SearchViewModel(ISearchService searchService)
    {
        _searchService = searchService;
    }

    public void UpdateSuggestions(string query)
    {
        Suggestions.Clear();
        foreach (var item in _searchService.Search(query))
            Suggestions.Add(item);
    }

    public void SelectSuggestion(string item)
    {
        SearchText = item;
        // Trigger navigation or filtering
    }
}
```

---

## Notes

- Handle `TextChanged` only when `args.Reason == AutoSuggestionBoxTextChangeReason.UserInput`
  to avoid re-querying when a suggestion is selected programmatically.
- `DisplayMemberPath` and `TextMemberPath` let you bind to complex objects instead of strings.
- `QueryIcon` accepts `"Find"`, `"Go"`, `"Arrow"`, or `null` (no icon).
- For single-query search (no live suggestions), use only `QuerySubmitted`.
- `AutoSuggestBox` does **not** support `x:Bind` for `ItemsSource` directly with `TextChanged`
  events; the minimal event bridge pattern shown above is the recommended approach.
