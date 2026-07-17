# Expander

`Expander` displays a header that the user can click to expand or collapse a content section. Use it for progressive disclosure of optional or advanced settings.

## Basic Expander

```xaml
<Expander
    Header="Advanced settings"
    Content="Content revealed when expanded."
    ExpandDirection="Down"
    IsExpanded="False" />
```

## Expander with rich content

```xaml
<Expander IsExpanded="False">
    <Expander.Header>
        <StackPanel Orientation="Horizontal" Spacing="8">
            <FontIcon Glyph="&#xE713;" />
            <TextBlock VerticalAlignment="Center" Text="Accessibility options" />
        </StackPanel>
    </Expander.Header>
    <Expander.Content>
        <StackPanel Padding="0,8,0,0" Spacing="12">
            <ToggleSwitch Header="High contrast" />
            <ToggleSwitch Header="Screen reader support" />
            <Slider Header="Text size" Maximum="200" Minimum="80" Value="100" />
        </StackPanel>
    </Expander.Content>
</Expander>
```

## Expander expanding upward

```xaml
<Expander
    VerticalAlignment="Bottom"
    ExpandDirection="Up"
    Header="Show details"
    IsExpanded="False">
    <TextBlock Text="Details content shown above the header." />
</Expander>
```

## Expander bound to ViewModel

```xaml
<Expander
    Header="Filters"
    IsExpanded="{x:Bind ViewModel.IsFiltersExpanded, Mode=TwoWay}">
    <StackPanel Spacing="8">
        <ComboBox
            Header="Category"
            ItemsSource="{x:Bind ViewModel.Categories}"
            SelectedItem="{x:Bind ViewModel.SelectedCategory, Mode=TwoWay}" />
        <DatePicker Header="From date" />
        <DatePicker Header="To date" />
        <Button Command="{x:Bind ViewModel.ApplyFiltersCommand}" Content="Apply" />
    </StackPanel>
</Expander>
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class SearchViewModel : ObservableObject
{
    [ObservableProperty]
    private bool _isFiltersExpanded;
}
```

## Custom header alignment

```xaml
<Expander
    Width="500"
    Padding="0"
    HorizontalContentAlignment="Left">
    <Expander.Header>
        <TextBlock HorizontalAlignment="Center" Text="Centered header text" />
    </Expander.Header>
    <Expander.Content>
        <TextBlock Margin="4" Text="Left-aligned content" />
    </Expander.Content>
</Expander>
```

## Notes

- `ExpandDirection` is `Down` (default) or `Up`. `Left`/`Right` are not supported.
- `IsExpanded` is two-way bindable — bind `TwoWay` to persist or track expand state in ViewModel.
- `HorizontalContentAlignment` controls alignment of the content area (default `Stretch`).
- The header area always remains visible; only the content area shows/hides.
- Avoid putting very large content inside `Expander` on the main UI thread without virtualisation.
- For a panel that can be fully hidden, use `Visibility` binding instead.
