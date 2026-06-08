# TextBlock

`TextBlock` is the primary control for displaying read-only text.
It supports inline formatting, text wrapping, selection, and rich typography.

---

## Basic TextBlock

```xaml
<TextBlock Text="Hello, WinUI 3!" />
```

---

## With Typography Properties

```xaml
<TextBlock
    Text="I am super excited to be here!"
    FontFamily="Arial"
    FontSize="24"
    FontStyle="Italic"
    Foreground="CornflowerBlue"
    CharacterSpacing="200"
    TextWrapping="WrapWholeWords" />
```

---

## With Applied Style (from ResourceDictionary)

```xaml
<!-- In Page.Resources -->
<Style x:Key="SubtitleStyle" TargetType="TextBlock">
    <Setter Property="FontSize" Value="18" />
    <Setter Property="FontWeight" Value="SemiBold" />
    <Setter Property="Foreground" Value="{ThemeResource TextFillColorSecondaryBrush}" />
</Style>

<!-- Usage -->
<TextBlock
    Text="Section subtitle"
    Style="{StaticResource SubtitleStyle}" />
```

---

## Inline Formatting (Run, Bold, Italic, Hyperlink)

```xaml
<TextBlock TextWrapping="Wrap">
    <Run FontFamily="Segoe UI" Foreground="{ThemeResource TextFillColorSecondaryBrush}">
        Text in a TextBlock doesn't have to be a plain string.
    </Run>
    <LineBreak />
    <Span>
        It can be <Bold>bold</Bold>, <Italic>italic</Italic>,
        or <Underline>underlined</Underline>.
    </Span>
    <LineBreak />
    <Hyperlink NavigateUri="https://learn.microsoft.com">Learn more</Hyperlink>
</TextBlock>
```

---

## Selectable TextBlock

```xaml
<TextBlock
    Text="Users can select and copy this text."
    IsTextSelectionEnabled="True"
    SelectionHighlightColor="{ThemeResource SystemAccentColor}"
    TextWrapping="Wrap" />
```

---

## Bound to ViewModel

```xaml
<!-- View -->
<TextBlock
    Text="{x:Bind ViewModel.StatusMessage, Mode=OneWay}"
    TextWrapping="WrapWholeWords"
    Style="{ThemeResource BodyTextBlockStyle}" />
```

```csharp
// ViewModels/StatusViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class StatusViewModel : ObservableObject
{
    [ObservableProperty]
    private string _statusMessage = "Ready";
}
```

---

## Text Trimming with Tooltip

```xaml
<!-- Trim long text with ellipsis; show full text in a tooltip -->
<TextBlock
    MaxWidth="200"
    Text="{x:Bind ViewModel.LongTitle, Mode=OneWay}"
    TextTrimming="CharacterEllipsis"
    TextWrapping="NoWrap">
    <ToolTipService.ToolTip>
        <ToolTip Content="{x:Bind ViewModel.LongTitle, Mode=OneWay}" />
    </ToolTipService.ToolTip>
</TextBlock>
```

---

## Notes

- Use `Style="{ThemeResource BodyTextBlockStyle}"` (or `CaptionTextBlockStyle`,
  `SubtitleTextBlockStyle`, `TitleTextBlockStyle`) for consistent typography.
- `TextWrapping="WrapWholeWords"` is preferred over `Wrap` for prose text.
- `TextTrimming="CharacterEllipsis"` clips overflowing text with `…`.
- `IsTextSelectionEnabled` is `false` by default — enable it for copyable content.
- For editable text, use `TextBox` or `RichEditBox`.
