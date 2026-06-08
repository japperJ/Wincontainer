# SwipeControl

`SwipeControl` reveals hidden action items when the user swipes horizontally over the control. It supports **Reveal** mode (actions stay open until dismissed) and **Execute** mode (action triggers immediately on full swipe).

## Swipe right to reveal actions

```xaml
<Border>
    <Border.Resources>
        <FontIconSource x:Key="AcceptIcon" Glyph="&#xE8FB;" />
        <FontIconSource x:Key="FlagIcon" Glyph="&#xE7C1;" />

        <SwipeItems x:Key="LeftItems" Mode="Reveal">
            <SwipeItem
                Background="{ThemeResource ButtonBackgroundThemeBrush}"
                Foreground="{ThemeResource AppBarItemForegroundThemeBrush}"
                IconSource="{StaticResource AcceptIcon}"
                Invoked="Accept_Invoked"
                Text="Accept" />
            <SwipeItem
                Background="{ThemeResource ButtonBackgroundThemeBrush}"
                Foreground="{ThemeResource AppBarItemForegroundThemeBrush}"
                IconSource="{StaticResource FlagIcon}"
                Invoked="Flag_Invoked"
                Text="Flag" />
        </SwipeItems>
    </Border.Resources>

    <SwipeControl
        Width="500"
        Height="68"
        BorderBrush="{ThemeResource ButtonBackground}"
        BorderThickness="1"
        LeftItems="{StaticResource LeftItems}">
        <TextBlock
            Margin="12"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Text="Swipe Right to reveal actions" />
    </SwipeControl>
</Border>
```

```csharp
private void Accept_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs e)
{
    // Handle accept action
}

private void Flag_Invoked(SwipeItem sender, SwipeItemInvokedEventArgs e)
{
    // Handle flag action
}
```

## Swipe left to execute (archive/delete)

```xaml
<Border>
    <Border.Resources>
        <FontIconSource x:Key="ArchiveIcon" Glyph="&#xE7B8;" />

        <SwipeItems x:Key="RightItems" Mode="Execute">
            <SwipeItem
                Background="IndianRed"
                BehaviorOnInvoked="Close"
                IconSource="{StaticResource ArchiveIcon}"
                Invoked="Archive_Invoked"
                Text="Archive" />
        </SwipeItems>
    </Border.Resources>

    <SwipeControl
        Width="500"
        Height="68"
        BorderBrush="{ThemeResource ButtonBackground}"
        BorderThickness="1"
        RightItems="{StaticResource RightItems}">
        <TextBlock
            Margin="12"
            HorizontalAlignment="Center"
            VerticalAlignment="Center"
            Text="Swipe Left to archive" />
    </SwipeControl>
</Border>
```

## SwipeControl in ListView (email-style)

```xaml
<ListView x:Name="MailList" ItemsSource="{x:Bind ViewModel.Messages}">
    <ListView.Resources>
        <FontIconSource x:Key="ReplyIcon" Glyph="&#xE8C2;" />
        <FontIconSource x:Key="DeleteIcon" Glyph="&#xE74D;" />

        <SwipeItems x:Key="RevealLeft" Mode="Reveal">
            <SwipeItem
                Background="#3E6FA7"
                Foreground="White"
                IconSource="{StaticResource ReplyIcon}"
                Text="Reply" />
        </SwipeItems>
        <SwipeItems x:Key="ExecuteRight" Mode="Execute">
            <SwipeItem
                Background="Crimson"
                IconSource="{StaticResource DeleteIcon}"
                Invoked="Delete_Invoked"
                Text="Delete" />
        </SwipeItems>
    </ListView.Resources>

    <ListView.ItemTemplate>
        <DataTemplate x:DataType="local:MailMessage">
            <SwipeControl
                Height="72"
                BorderBrush="{ThemeResource DividerStrokeColorDefaultBrush}"
                BorderThickness="0,0,0,1"
                LeftItems="{StaticResource RevealLeft}"
                RightItems="{StaticResource ExecuteRight}">
                <StackPanel Padding="12" Spacing="4">
                    <TextBlock
                        Style="{ThemeResource BodyStrongTextBlockStyle}"
                        Text="{x:Bind Subject}" />
                    <TextBlock
                        Foreground="{ThemeResource TextFillColorSecondaryBrush}"
                        Text="{x:Bind Preview}"
                        TextTrimming="WordEllipsis" />
                </StackPanel>
            </SwipeControl>
        </DataTemplate>
    </ListView.ItemTemplate>

    <ListView.ItemContainerStyle>
        <Style TargetType="ListViewItem">
            <Setter Property="Padding" Value="0" />
            <Setter Property="HorizontalContentAlignment" Value="Stretch" />
        </Style>
    </ListView.ItemContainerStyle>
</ListView>
```

## Notes

- `LeftItems` reveal when swiping **right**; `RightItems` reveal when swiping **left**.
- `Mode="Reveal"` — items stay visible; user must tap an item or swipe back to dismiss.
- `Mode="Execute"` — triggers the single item's action on full swipe; use `BehaviorOnInvoked="Close"` to auto-close.
- `SwipeItem.Background` accepts any brush including `LinearGradientBrush`.
- Inside a `ListView`, set `ListViewItem.Padding="0"` and `HorizontalContentAlignment="Stretch"` to prevent gaps.
- Always provide `Text` on `SwipeItem` for accessibility even when `Mode="Execute"`.
