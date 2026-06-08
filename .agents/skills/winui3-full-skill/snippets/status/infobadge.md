# InfoBadge

`InfoBadge` is a small notification indicator displayed on or near another control (typically a `NavigationViewItem`). It supports dot, icon, and numeric value styles across four semantic colours (Attention, Informational, Success, Critical).

## InfoBadge in NavigationViewItem

```xaml
<NavigationView PaneDisplayMode="Left">
    <NavigationView.MenuItems>
        <NavigationViewItem Content="Home" Icon="Home" />
        <NavigationViewItem
            AutomationProperties.Name="Inbox, 5 unread"
            Content="Inbox"
            Icon="Mail">
            <NavigationViewItem.InfoBadge>
                <InfoBadge Value="5" />
            </NavigationViewItem.InfoBadge>
        </NavigationViewItem>
    </NavigationView.MenuItems>
</NavigationView>
```

## Severity style variants

```xaml
<StackPanel Orientation="Horizontal" Spacing="20">
    <!-- Icon badge -->
    <InfoBadge Style="{StaticResource AttentionIconInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource InformationalIconInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource SuccessIconInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource CriticalIconInfoBadgeStyle}" />

    <!-- Value badge -->
    <InfoBadge Style="{StaticResource AttentionValueInfoBadgeStyle}" Value="3" />
    <InfoBadge Style="{StaticResource InformationalValueInfoBadgeStyle}" Value="10" />
    <InfoBadge Style="{StaticResource SuccessValueInfoBadgeStyle}" Value="1" />
    <InfoBadge Style="{StaticResource CriticalValueInfoBadgeStyle}" Value="99" />

    <!-- Dot badge -->
    <InfoBadge Style="{StaticResource AttentionDotInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource InformationalDotInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource SuccessDotInfoBadgeStyle}" />
    <InfoBadge Style="{StaticResource CriticalDotInfoBadgeStyle}" />
</StackPanel>
```

## InfoBadge overlaid on a Button

```xaml
<Button
    Width="60"
    Height="60"
    Padding="0"
    HorizontalContentAlignment="Stretch"
    VerticalContentAlignment="Stretch"
    ToolTipService.ToolTip="Refresh required"
    AutomationProperties.Name="Refresh, action required">
    <Grid HorizontalAlignment="Stretch" VerticalAlignment="Stretch">
        <SymbolIcon HorizontalAlignment="Center" Symbol="Sync" />
        <InfoBadge
            HorizontalAlignment="Right"
            VerticalAlignment="Top"
            Background="#C42B1C">
            <InfoBadge.IconSource>
                <FontIconSource
                    FontFamily="{StaticResource SymbolThemeFontFamily}"
                    Glyph="&#xF13C;" />
            </InfoBadge.IconSource>
        </InfoBadge>
    </Grid>
</Button>
```

## Dynamic value bound to ViewModel

```xaml
<InfoBadge Value="{x:Bind ViewModel.UnreadCount, Mode=OneWay}" />
```

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class InboxViewModel : ObservableObject
{
    [ObservableProperty]
    private int _unreadCount;

    public async Task LoadAsync(CancellationToken ct)
    {
        // Set Value to -1 to hide the badge automatically
        UnreadCount = await _mailService.GetUnreadCountAsync(ct);
    }
}
```

## Toggling visibility

```xaml
<NavigationViewItem Content="Inbox" Icon="Mail">
    <NavigationViewItem.InfoBadge>
        <InfoBadge
            Opacity="{x:Bind ViewModel.ShowBadge, Mode=OneWay, Converter={StaticResource BoolToOpacityConverter}}"
            Value="{x:Bind ViewModel.UnreadCount, Mode=OneWay}" />
    </NavigationViewItem.InfoBadge>
</NavigationViewItem>
```

## Style name convention

`{Severity}{Type}InfoBadgeStyle` where:
- **Severity**: `Attention`, `Informational`, `Success`, `Critical`
- **Type**: `Icon`, `Value`, `Dot`

Example: `AttentionValueInfoBadgeStyle`, `SuccessDotInfoBadgeStyle`.

## Notes

- Setting `Value="-1"` hides the badge (no number shown, dot style shown instead).
- Always set `AutomationProperties.Name` on the parent `NavigationViewItem` to include badge info for screen readers (e.g. `"Inbox, 5 notifications"`).
- `InfoBadge` inside a `NavigationViewItem.InfoBadge` is automatically positioned by the control; no manual positioning needed.
- For custom positioning on non-NavigationView controls, place `InfoBadge` in a `Grid` and set `HorizontalAlignment="Right"` / `VerticalAlignment="Top"`.
