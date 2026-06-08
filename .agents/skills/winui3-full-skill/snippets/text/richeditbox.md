# RichEditBox

`RichEditBox` is a rich-text editor that supports formatted text (bold, italic, colors,
paragraph styles). Use it for notes, email composers, or any scenario needing styled text.

---

## Basic RichEditBox

```xaml
<RichEditBox
    Width="400"
    MinHeight="120"
    PlaceholderText="Start typing…"
    AutomationProperties.Name="Rich text editor" />
```

---

## With Spell Check and Wrapping

```xaml
<RichEditBox
    Width="400"
    MinHeight="200"
    PlaceholderText="Enter your message…"
    IsSpellCheckEnabled="True"
    TextWrapping="Wrap"
    AcceptsReturn="True"
    AutomationProperties.Name="Message body" />
```

---

## Reading and Writing Text Programmatically

```xaml
<StackPanel Spacing="8">
    <RichEditBox
        x:Name="Editor"
        Width="400"
        MinHeight="120"
        AutomationProperties.Name="Editor" />
    <StackPanel Orientation="Horizontal" Spacing="4">
        <Button Content="Get text" Click="GetText_Click" />
        <Button Content="Set text" Click="SetText_Click" />
        <Button Content="Bold selection" Click="Bold_Click" />
    </StackPanel>
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private void GetText_Click(object sender, RoutedEventArgs e)
{
    Editor.Document.GetText(Microsoft.UI.Text.TextGetOptions.None, out string text);
    System.Diagnostics.Debug.WriteLine(text);
}

private void SetText_Click(object sender, RoutedEventArgs e)
{
    Editor.Document.SetText(Microsoft.UI.Text.TextSetOptions.None, "Hello, **World**!");
}

private void Bold_Click(object sender, RoutedEventArgs e)
{
    var selection = Editor.Document.Selection;
    if (selection.Length != 0)
    {
        var charFormat = selection.CharacterFormat;
        charFormat.Bold = charFormat.Bold == Microsoft.UI.Text.FormatEffect.On
            ? Microsoft.UI.Text.FormatEffect.Off
            : Microsoft.UI.Text.FormatEffect.On;
    }
}
```

---

## Formatting Toolbar + RichEditBox

```xaml
<StackPanel>
    <CommandBar DefaultLabelPosition="Collapsed">
        <AppBarToggleButton
            x:Name="BoldButton"
            Icon="Bold"
            Label="Bold"
            Click="Format_Click"
            AutomationProperties.Name="Bold" />
        <AppBarToggleButton
            x:Name="ItalicButton"
            Icon="Italic"
            Label="Italic"
            Click="Format_Click"
            AutomationProperties.Name="Italic" />
        <AppBarToggleButton
            x:Name="UnderlineButton"
            Label="Underline"
            Click="Format_Click"
            AutomationProperties.Name="Underline">
            <AppBarToggleButton.Icon>
                <FontIcon Glyph="&#xE8DC;" />
            </AppBarToggleButton.Icon>
        </AppBarToggleButton>
    </CommandBar>
    <RichEditBox
        x:Name="RichEditor"
        MinHeight="200"
        PlaceholderText="Type here…"
        AutomationProperties.Name="Document editor" />
</StackPanel>
```

```csharp
// MainPage.xaml.cs
private void Format_Click(object sender, RoutedEventArgs e)
{
    var selection = RichEditor.Document.Selection;
    if (selection.Length == 0) return;

    var cf = selection.CharacterFormat;
    if (sender == BoldButton)
        cf.Bold = BoldButton.IsChecked == true
            ? Microsoft.UI.Text.FormatEffect.On
            : Microsoft.UI.Text.FormatEffect.Off;
    else if (sender == ItalicButton)
        cf.Italic = ItalicButton.IsChecked == true
            ? Microsoft.UI.Text.FormatEffect.On
            : Microsoft.UI.Text.FormatEffect.Off;
    else if (sender == UnderlineButton)
        cf.Underline = UnderlineButton.IsChecked == true
            ? Microsoft.UI.Text.UnderlineType.Single
            : Microsoft.UI.Text.UnderlineType.None;
}
```

---

## Notes

- Use `Document.GetText(TextGetOptions.None, out string)` to read plain text.
- Use `Document.GetText(TextGetOptions.FormatRtf, out string)` to read RTF (preserves formatting).
- `Document.SetText(TextSetOptions.FormatRtf, rtfString)` to load RTF content.
- `RichEditBox` is heavier than `TextBox` — only use it when rich formatting is required.
- For display-only styled text, use `RichTextBlock` instead.
- Saving content: store the RTF string retrieved via `TextGetOptions.FormatRtf`.
