# WinUI 3: Nested `ContentControl` Star-Column Collapse

## Symptom

When a `Grid` with star columns (`*`, `2*`, `1.5*`, etc.) is placed inside a `ContentControl` that lives in a nested `ItemsControl` DataTemplate (e.g., grouped containers inside a group header), the star columns collapse to zero width — or distribute differently than the same `Grid` used as a standalone `ListView` item.

The outer `ListView` items render correctly because `ListViewItemPresenter` forces `HorizontalContentAlignment="Stretch"`, but `ContentControl` defaults to `HorizontalContentAlignment="Left"`.

## Root Cause

`ContentControl` inherits `HorizontalContentAlignment="Left"` from `Control`. This causes its internal `ContentPresenter` to size to content rather than fill the available width. A `Grid` with star columns needs a defined constraint width to distribute star-column space; without it, all star columns get 0 width.

`ListViewItemPresenter` (used by `ListViewItem` for standalone items) always stretches content, so the same `DataTemplate` works correctly at the top level.

## Fix

On every `ContentControl` used inside a nested `ItemsControl.ItemTemplate`, explicitly set both properties:

```xml
<ContentControl
    Content="{Binding}"
    ContentTemplate="{StaticResource SomeTemplate}"
    HorizontalContentAlignment="Stretch"
    HorizontalAlignment="Stretch" />
```

Without these, star columns in any `Grid` inside the template will not distribute correctly.

## Relevant Files

- `src/WinContainers.App/Pages/ContainersControl.xaml` — `ContainerRowTemplate` used in both standalone (`ListView`) and grouped (`ItemsControl` → `ContentControl`) contexts; the `ContentControl` wrapper in the group's `ItemTemplate` needs the stretch properties
- `src/WinContainers.App/Models/ContainerCardData.cs` — data model
- `src/WinContainers.App/ViewModels/ContainersViewModel.cs` — grouping logic
