# Theme Transitions

Theme transitions animate elements as they **enter**, **exit**, or **reposition** in the visual tree. Unlike implicit transitions (which animate property changes), theme transitions are triggered by visibility changes, layout changes, and collection mutations.

---

## EntranceThemeTransition — Fade/Slide in on Page Load

```xaml
<StackPanel>
    <StackPanel.ChildrenTransitions>
        <TransitionCollection>
            <EntranceThemeTransition />
        </TransitionCollection>
    </StackPanel.ChildrenTransitions>

    <TextBlock Text="I slide in from below on page load" />
    <Button Content="And so do I" />
</StackPanel>
```

---

## RepositionThemeTransition — Animate Layout Changes

```xaml
<StackPanel>
    <StackPanel.ChildrenTransitions>
        <TransitionCollection>
            <RepositionThemeTransition />
        </TransitionCollection>
    </StackPanel.ChildrenTransitions>

    <!-- Items that are added/removed will cause others to reposition with animation -->
</StackPanel>
```

---

## AddDeleteThemeTransition — Animate Collection Changes

```xaml
<ListView ItemsSource="{x:Bind ViewModel.Items}">
    <ListView.ItemContainerTransitions>
        <TransitionCollection>
            <AddDeleteThemeTransition />
            <ReorderThemeTransition />
        </TransitionCollection>
    </ListView.ItemContainerTransitions>
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:MyItem">
            <TextBlock Text="{x:Bind Name}" />
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

```csharp
// ViewModels/ListViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace MyApp.ViewModels;

public partial class ListViewModel : ObservableObject
{
    public ObservableCollection<MyItem> Items { get; } = new();

    [RelayCommand]
    private void AddItem()
    {
        // Adding to ObservableCollection triggers AddDeleteThemeTransition
        Items.Insert(0, new MyItem { Name = $"Item {Items.Count + 1}" });
    }

    [RelayCommand]
    private void RemoveItem()
    {
        if (Items.Count > 0)
            Items.RemoveAt(0);
    }
}
```

---

## PopupThemeTransition — Fly-in for Popups / Flyouts

```xaml
<Popup x:Name="MyPopup">
    <Popup.ChildTransitions>
        <TransitionCollection>
            <PopupThemeTransition />
        </TransitionCollection>
    </Popup.ChildTransitions>
    <Border
        Width="200"
        Padding="16"
        Background="{ThemeResource AcrylicInAppFillColorDefaultBrush}"
        CornerRadius="8">
        <TextBlock Text="Popup content" />
    </Border>
</Popup>
```

---

## ContentThemeTransition — Animate Content Changes

```xaml
<ContentControl x:Name="ContentArea">
    <ContentControl.ContentTransitions>
        <TransitionCollection>
            <ContentThemeTransition />
        </TransitionCollection>
    </ContentControl.ContentTransitions>
</ContentControl>
```

```csharp
// Changing Content triggers a cross-fade animation
ContentArea.Content = newView;
```

---

## Transition Types Reference

| Transition | Where to apply | Trigger |
|---|---|---|
| `EntranceThemeTransition` | `ChildrenTransitions` | Element appears in visual tree |
| `RepositionThemeTransition` | `ChildrenTransitions` | Layout position changes |
| `AddDeleteThemeTransition` | `ItemContainerTransitions` | Item added/removed in collection |
| `ReorderThemeTransition` | `ItemContainerTransitions` | Item moves within collection |
| `PopupThemeTransition` | `Popup.ChildTransitions` | Popup opens |
| `ContentThemeTransition` | `ContentTransitions` | Content property changes |
| `PaneThemeTransition` | Panel enter/exit | Side pane shows/hides |

---

## Notes

- Theme transitions apply to the **container** (`StackPanel.ChildrenTransitions`), not to individual elements.
- For `ListView`/`GridView`, use `ItemContainerTransitions` for per-item enter/exit and `AddDeleteThemeTransition` + `ReorderThemeTransition` together for smooth drag-reorder.
- Transitions are disabled when the user has turned off animations in Windows Settings (Ease of Access → Display → Show animations). Design layouts that work without them.
- `EntranceThemeTransition.IsStaggeringEnabled = true` staggers entrance animations across sibling items for a cascade effect.
- Avoid applying `EntranceThemeTransition` to elements that are frequently toggled — the entrance fires every time the element is re-added to the tree.
