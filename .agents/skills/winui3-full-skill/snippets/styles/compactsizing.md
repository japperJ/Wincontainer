# CompactSizing

WinUI 3 supports a **Compact** density mode that reduces padding and spacing across all controls — useful for data-dense apps such as file managers, dashboards, and developer tools. Apply it via a `RequestedTheme`-aware `ResourceDictionary` merge.

---

## Enabling Compact Sizing App-Wide

```xaml
<!-- App.xaml -->
<Application>
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
                <!-- Merge the compact resource dictionary AFTER XamlControlsResources -->
                <ResourceDictionary Source="ms-appx:///Microsoft.UI.Xaml/DensityStyles/Compact.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

## Scoped to a Single Container

You can scope compact sizing to just part of the UI by merging the dictionary on an element's `Resources`:

```xaml
<StackPanel>
    <StackPanel.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="ms-appx:///Microsoft.UI.Xaml/DensityStyles/Compact.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </StackPanel.Resources>

    <!-- These controls render at compact density -->
    <Button Content="Compact Button" />
    <TextBox PlaceholderText="Compact TextBox" />
    <ComboBox>
        <x:String>Option A</x:String>
        <x:String>Option B</x:String>
    </ComboBox>
</StackPanel>
```

---

## Toggle Compact Sizing at Runtime

```csharp
// Views/SettingsPage.xaml.cs
private bool _isCompact = false;

private void ToggleCompactButton_Click(object sender,
    Microsoft.UI.Xaml.RoutedEventArgs e)
{
    _isCompact = !_isCompact;

    var appResources = Application.Current.Resources.MergedDictionaries;

    if (_isCompact)
    {
        appResources.Add(new Microsoft.UI.Xaml.ResourceDictionary
        {
            Source = new Uri("ms-appx:///Microsoft.UI.Xaml/DensityStyles/Compact.xaml")
        });
    }
    else
    {
        var compactDict = appResources.FirstOrDefault(d =>
            d.Source?.OriginalString.Contains("Compact.xaml") == true);

        if (compactDict is not null)
            appResources.Remove(compactDict);
    }
}
```

---

## What Changes in Compact Mode

| Control | Normal height | Compact height |
|---|---|---|
| `Button` | 32 px | 24 px |
| `TextBox` | 32 px | 28 px |
| `ComboBox` | 32 px | 28 px |
| `CheckBox` | 32 px | 24 px |
| `ListView` item | ~44 px | ~32 px |
| `NavigationViewItem` | 40 px | 32 px |

---

## Notes

- Compact sizing only reduces **control internal padding** — it does not change `Margin` you set explicitly.
- The `Compact.xaml` dictionary must be merged **after** `XamlControlsResources` to override the default templates correctly.
- Compact mode does not affect font sizes or icon sizes — only control chrome padding.
- Test with assistive technology after enabling compact mode; reduced touch targets may affect accessibility.
- There is no "Extra Compact" built-in; for even denser UIs, override `MinHeight` on individual controls or create custom styles.
