# RichTextBlock

`RichTextBlock` is the read-only counterpart to `RichEditBox`.
It supports inline formatting elements (`Run`, `Bold`, `Italic`, `Hyperlink`, `InlineUIContainer`)
and column overflow with `RichTextBlockOverflow`.
Use it for formatted article text, help content, or any read-only document layout.

---

## Basic RichTextBlock

```xaml
<RichTextBlock TextWrapping="Wrap">
    <Paragraph>
        This is a paragraph with <Bold>bold</Bold>, <Italic>italic</Italic>,
        and <Underline>underlined</Underline> text.
    </Paragraph>
</RichTextBlock>
```

---

## Multiple Paragraphs with Inline Links

```xaml
<RichTextBlock MaxWidth="500" TextWrapping="Wrap">
    <Paragraph FontSize="18" FontWeight="SemiBold">
        Introduction
    </Paragraph>
    <Paragraph>
        WinUI 3 is the latest iteration of Microsoft's native UI platform for Windows.
        Learn more at
        <Hyperlink NavigateUri="https://learn.microsoft.com/windows/apps/winui/">
            Microsoft Docs
        </Hyperlink>.
    </Paragraph>
    <Paragraph Margin="0,8,0,0">
        <Run Foreground="{ThemeResource TextFillColorSecondaryBrush}">
            Last updated: January 2025
        </Run>
    </Paragraph>
</RichTextBlock>
```

---

## Inline Image (InlineUIContainer)

```xaml
<RichTextBlock TextWrapping="Wrap">
    <Paragraph>
        The WinUI logo:
        <InlineUIContainer>
            <Image Source="/Assets/logo.png" Width="32" Height="32"
                   VerticalAlignment="Center" />
        </InlineUIContainer>
        is shown inline with the text.
    </Paragraph>
</RichTextBlock>
```

---

## Column Overflow (Multi-Column Layout)

```xaml
<Grid>
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="*" />
    </Grid.ColumnDefinitions>

    <RichTextBlock
        x:Name="Column1"
        Grid.Column="0"
        Margin="0,0,12,0"
        OverflowContentTarget="{x:Bind Column2Overflow}"
        TextWrapping="Wrap">
        <Paragraph>
            Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod
            tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam,
            quis nostrud exercitation ullamco laboris. Duis aute irure dolor in reprehenderit
            in voluptate velit esse cillum dolore eu fugiat nulla pariatur.
        </Paragraph>
    </RichTextBlock>

    <RichTextBlockOverflow
        x:Name="Column2Overflow"
        Grid.Column="1" />
</Grid>
```

---

## Bound to ViewModel (Inline Runs)

```xaml
<!-- View — simple text binding -->
<RichTextBlock>
    <Paragraph>
        <Run Text="{x:Bind ViewModel.ArticleTitle, Mode=OneWay}"
             FontWeight="Bold" FontSize="20" />
    </Paragraph>
    <Paragraph>
        <Run Text="{x:Bind ViewModel.ArticleBody, Mode=OneWay}" />
    </Paragraph>
</RichTextBlock>
```

```csharp
// ViewModels/ArticleViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;

namespace MyApp.ViewModels;

public partial class ArticleViewModel : ObservableObject
{
    [ObservableProperty]
    private string _articleTitle = "My Article";

    [ObservableProperty]
    private string _articleBody = "Body text goes here.";
}
```

---

## Notes

- `RichTextBlock` is **display-only** — for editable rich text, use `RichEditBox`.
- `OverflowContentTarget` enables text to flow from one `RichTextBlock` into a
  `RichTextBlockOverflow` — useful for magazine-style column layouts.
- `IsTextSelectionEnabled="True"` allows users to copy text (default: `false`).
- Inline elements: `Run`, `Bold`, `Italic`, `Underline`, `Span`, `Hyperlink`,
  `LineBreak`, `InlineUIContainer`.
- For simple non-formatted text display, prefer `TextBlock` (lighter weight).
