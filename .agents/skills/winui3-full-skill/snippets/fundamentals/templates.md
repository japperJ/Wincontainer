# Data Templates

A `DataTemplate` defines the visual structure used to display a data object. Every
collection control (`ListView`, `GridView`, `ItemsRepeater`, etc.) and content control
(`ContentControl`, `ContentPresenter`) uses data templates to render its items.

**Always** set `x:DataType` on a `DataTemplate` to enable compiled `x:Bind` bindings.

---

## Basic ItemTemplate for a ListView

```xaml
<!-- ItemsPage.xaml -->
<Page
    x:Class="MyApp.ItemsPage"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:MyApp.Models">

    <ListView ItemsSource="{x:Bind ViewModel.Products, Mode=OneWay}">
        <ListView.ItemTemplate>
            <DataTemplate x:DataType="models:Product">
                <Grid ColumnSpacing="12" Padding="4">
                    <Grid.ColumnDefinitions>
                        <ColumnDefinition Width="48" />
                        <ColumnDefinition Width="*" />
                        <ColumnDefinition Width="Auto" />
                    </Grid.ColumnDefinitions>

                    <Image
                        Grid.Column="0"
                        Width="48" Height="48"
                        Source="{x:Bind ThumbnailUrl}"
                        Stretch="UniformToFill" />

                    <StackPanel Grid.Column="1" VerticalAlignment="Center">
                        <TextBlock Text="{x:Bind Name}" FontWeight="SemiBold" />
                        <TextBlock Text="{x:Bind Category}"
                                   Style="{StaticResource CaptionTextBlockStyle}"
                                   Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
                    </StackPanel>

                    <TextBlock
                        Grid.Column="2"
                        VerticalAlignment="Center"
                        Text="{x:Bind Price, Converter={StaticResource CurrencyConverter}}" />
                </Grid>
            </DataTemplate>
        </ListView.ItemTemplate>
    </ListView>
</Page>
```

---

## Reusable DataTemplate in a ResourceDictionary

Move a `DataTemplate` to a `ResourceDictionary` when it is shared across multiple pages.

```xaml
<!-- Themes/DataTemplates.xaml -->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:models="using:MyApp.Models">

    <DataTemplate x:Key="ProductCardTemplate" x:DataType="models:Product">
        <Border
            Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
            BorderBrush="{ThemeResource CardStrokeColorDefaultBrush}"
            BorderThickness="1"
            CornerRadius="8"
            Padding="16">
            <StackPanel Spacing="4">
                <TextBlock Text="{x:Bind Name}" Style="{StaticResource BodyStrongTextBlockStyle}" />
                <TextBlock Text="{x:Bind Description}" TextWrapping="WrapWholeWords"
                           Foreground="{ThemeResource TextFillColorSecondaryBrush}" />
            </StackPanel>
        </Border>
    </DataTemplate>
</ResourceDictionary>
```

Reference it in `App.xaml`:

```xaml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <ResourceDictionary Source="/Themes/DataTemplates.xaml" />
</ResourceDictionary.MergedDictionaries>
```

Usage:

```xaml
<GridView
    ItemsSource="{x:Bind ViewModel.Products, Mode=OneWay}"
    ItemTemplate="{StaticResource ProductCardTemplate}" />
```

---

## DataTemplateSelector — different templates per item type

```csharp
// Selectors/MessageTemplateSelector.cs
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyApp.Models;

namespace MyApp.Selectors;

public class MessageTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SentTemplate     { get; set; }
    public DataTemplate? ReceivedTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => item is Message { IsSent: true } ? SentTemplate : ReceivedTemplate;
}
```

```xaml
<!-- ChatPage.xaml -->
<Page
    xmlns:selectors="using:MyApp.Selectors"
    xmlns:models="using:MyApp.Models">

    <Page.Resources>
        <!-- Sent message bubble -->
        <DataTemplate x:Key="SentTemplate" x:DataType="models:Message">
            <Border
                HorizontalAlignment="Right"
                Background="{ThemeResource AccentFillColorDefaultBrush}"
                CornerRadius="12,12,2,12"
                Padding="12,8"
                MaxWidth="280">
                <TextBlock Text="{x:Bind Text}" Foreground="White" TextWrapping="Wrap" />
            </Border>
        </DataTemplate>

        <!-- Received message bubble -->
        <DataTemplate x:Key="ReceivedTemplate" x:DataType="models:Message">
            <Border
                HorizontalAlignment="Left"
                Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                CornerRadius="12,12,12,2"
                Padding="12,8"
                MaxWidth="280">
                <TextBlock Text="{x:Bind Text}" TextWrapping="Wrap" />
            </Border>
        </DataTemplate>

        <selectors:MessageTemplateSelector
            x:Key="MessageSelector"
            SentTemplate="{StaticResource SentTemplate}"
            ReceivedTemplate="{StaticResource ReceivedTemplate}" />
    </Page.Resources>

    <ListView
        ItemsSource="{x:Bind ViewModel.Messages, Mode=OneWay}"
        ItemTemplateSelector="{StaticResource MessageSelector}" />
</Page>
```

---

## ContentTemplate on a ContentControl

`ContentControl` and `ContentPresenter` use `ContentTemplate` to render a single object.

```xaml
<!-- Display a single Product object using a DataTemplate -->
<ContentControl
    Content="{x:Bind ViewModel.SelectedProduct, Mode=OneWay}">
    <ContentControl.ContentTemplate>
        <DataTemplate x:DataType="models:Product">
            <StackPanel Spacing="8">
                <TextBlock Text="{x:Bind Name}" Style="{StaticResource SubtitleTextBlockStyle}" />
                <TextBlock Text="{x:Bind Description}" TextWrapping="WrapWholeWords" />
                <TextBlock Text="{x:Bind Price, Converter={StaticResource CurrencyConverter}}"
                           FontWeight="SemiBold" />
            </StackPanel>
        </DataTemplate>
    </ContentControl.ContentTemplate>
</ContentControl>
```

---

## ItemContainerStyle — styling the item wrapper

```xaml
<!-- Remove the default ListView item highlight / selection indicator -->
<ListView ItemsSource="{x:Bind ViewModel.Items, Mode=OneWay}">
    <ListView.ItemContainerStyle>
        <Style TargetType="ListViewItem">
            <Setter Property="HorizontalContentAlignment" Value="Stretch" />
            <Setter Property="Padding" Value="0" />
            <!-- Remove background on hover/select for a card-list look -->
            <Setter Property="Background"              Value="Transparent" />
            <Setter Property="BackgroundSizing"        Value="OuterBorderEdge" />
        </Style>
    </ListView.ItemContainerStyle>
    <ListView.ItemTemplate>
        <DataTemplate x:DataType="models:Product">
            <!-- Each card handles its own visual appearance -->
            <Border Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"
                    CornerRadius="8" Padding="16" Margin="0,4">
                <TextBlock Text="{x:Bind Name}" />
            </Border>
        </DataTemplate>
    </ListView.ItemTemplate>
</ListView>
```

---

## Notes

- Always set `x:DataType` on every `DataTemplate`; omitting it forces runtime reflection and
  loses compile-time safety.
- Prefer reusable templates in a `ResourceDictionary` file over inline templates duplicated
  across pages.
- `DataTemplateSelector` is the correct solution for heterogeneous item types; avoid
  `Visibility` toggling tricks inside a single template.
- Keep `DataTemplate` XAML flat and light — complex nested panels inside list templates slow
  down item realisation. Move heavy content to a custom `UserControl`.
- `ItemContainerStyle` controls the **wrapper** element (`ListViewItem`, `GridViewItem`),
  not the item itself; set `HorizontalContentAlignment="Stretch"` there when your template
  needs full width.
