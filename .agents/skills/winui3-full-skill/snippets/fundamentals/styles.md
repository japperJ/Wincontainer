# Styles

Styles in WinUI 3 are `Style` objects stored in a `ResourceDictionary`. They group property
setters so you can apply a consistent look to many controls without repeating markup.
Styles can be **explicit** (referenced by key) or **implicit** (applied to all controls of a
`TargetType` in scope).

---

## Basic explicit style

```xaml
<!-- MyPage.xaml -->
<Page.Resources>
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="Background"   Value="{ThemeResource AccentFillColorDefaultBrush}" />
        <Setter Property="Foreground"   Value="{ThemeResource TextOnAccentFillColorPrimaryBrush}" />
        <Setter Property="FontWeight"   Value="SemiBold" />
        <Setter Property="Padding"      Value="24,8" />
        <Setter Property="CornerRadius" Value="4" />
    </Style>
</Page.Resources>

<!-- Apply by key -->
<Button Content="Save" Style="{StaticResource PrimaryButtonStyle}" />
<Button Content="Submit" Style="{StaticResource PrimaryButtonStyle}" />
```

---

## Implicit style (applies to all matching controls in scope)

An implicit style has **no** `x:Key`. Every control of `TargetType` in the dictionary's
scope automatically gets the style.

```xaml
<Page.Resources>
    <!-- Every TextBlock on this page gets this style -->
    <Style TargetType="TextBlock">
        <Setter Property="FontFamily"   Value="Segoe UI Variable" />
        <Setter Property="FontSize"     Value="14" />
        <Setter Property="LineHeight"   Value="20" />
    </Style>
</Page.Resources>

<TextBlock Text="This text uses the implicit style automatically." />
```

---

## Basing a style on an existing style (BasedOn)

```xaml
<Page.Resources>
    <!-- Base style -->
    <Style x:Key="BaseCardStyle" TargetType="Border">
        <Setter Property="CornerRadius"  Value="8" />
        <Setter Property="Padding"       Value="16" />
        <Setter Property="Background"    Value="{ThemeResource CardBackgroundFillColorDefaultBrush}" />
        <Setter Property="BorderBrush"   Value="{ThemeResource CardStrokeColorDefaultBrush}" />
        <Setter Property="BorderThickness" Value="1" />
    </Style>

    <!-- Extended style — inherits all setters from BaseCardStyle -->
    <Style x:Key="ElevatedCardStyle"
           TargetType="Border"
           BasedOn="{StaticResource BaseCardStyle}">
        <!-- Override just the padding -->
        <Setter Property="Padding" Value="24" />
    </Style>
</Page.Resources>
```

---

## Extending a built-in WinUI 3 style

Use `BasedOn="{StaticResource DefaultButtonStyle}"` (or the WinUI key for the target control)
to add setters on top of the platform default without rewriting the full control template.

```xaml
<Page.Resources>
    <Style x:Key="IconButtonStyle"
           TargetType="Button"
           BasedOn="{StaticResource DefaultButtonStyle}">
        <Setter Property="Width"        Value="40" />
        <Setter Property="Height"       Value="40" />
        <Setter Property="Padding"      Value="0" />
        <Setter Property="CornerRadius" Value="20" />
    </Style>
</Page.Resources>

<Button Style="{StaticResource IconButtonStyle}">
    <FontIcon Glyph="&#xE713;" FontSize="16" />
</Button>
```

---

## App-wide styles in App.xaml

```xaml
<!-- App.xaml -->
<Application.Resources>
    <ResourceDictionary>
        <ResourceDictionary.MergedDictionaries>
            <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
        </ResourceDictionary.MergedDictionaries>

        <!-- These styles are available everywhere in the app -->
        <Style x:Key="SectionHeaderStyle" TargetType="TextBlock"
               BasedOn="{StaticResource SubtitleTextBlockStyle}">
            <Setter Property="Margin" Value="0,24,0,8" />
        </Style>
    </ResourceDictionary>
</Application.Resources>
```

---

## Styles in a separate ResourceDictionary file

```xaml
<!-- Themes/ButtonStyles.xaml -->
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">

    <Style x:Key="DestructiveButtonStyle" TargetType="Button"
           BasedOn="{StaticResource DefaultButtonStyle}">
        <Setter Property="Background"   Value="{ThemeResource SystemFillColorCriticalBackgroundBrush}" />
        <Setter Property="Foreground"   Value="{ThemeResource TextOnCriticalFillColorPrimaryBrush}" />
        <Setter Property="BorderBrush"  Value="Transparent" />
    </Style>
</ResourceDictionary>
```

Merge it in `App.xaml`:

```xaml
<ResourceDictionary.MergedDictionaries>
    <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />
    <ResourceDictionary Source="/Themes/ButtonStyles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

---

## Clearing a style (reverting to default)

```xaml
<!-- Set Style="{x:Null}" to clear an inherited implicit style -->
<TextBlock Text="No style applied" Style="{x:Null}" />
```

---

## ControlTemplate (advanced)

A `ControlTemplate` replaces the entire visual tree of a control. Use it only when `Style`
setters are insufficient (e.g., adding a new visual state or changing the layout).

```xaml
<Page.Resources>
    <Style x:Key="PillButtonStyle" TargetType="Button">
        <Setter Property="Template">
            <Setter.Value>
                <ControlTemplate TargetType="Button">
                    <Border
                        x:Name="Root"
                        Background="{TemplateBinding Background}"
                        CornerRadius="999"
                        Padding="{TemplateBinding Padding}">
                        <VisualStateManager.VisualStateGroups>
                            <VisualStateGroup x:Name="CommonStates">
                                <VisualState x:Name="Normal" />
                                <VisualState x:Name="PointerOver">
                                    <Storyboard>
                                        <ObjectAnimationUsingKeyFrames
                                            Storyboard.TargetName="Root"
                                            Storyboard.TargetProperty="Opacity">
                                            <DiscreteObjectKeyFrame KeyTime="0" Value="0.85" />
                                        </ObjectAnimationUsingKeyFrames>
                                    </Storyboard>
                                </VisualState>
                                <VisualState x:Name="Pressed">
                                    <Storyboard>
                                        <ObjectAnimationUsingKeyFrames
                                            Storyboard.TargetName="Root"
                                            Storyboard.TargetProperty="Opacity">
                                            <DiscreteObjectKeyFrame KeyTime="0" Value="0.65" />
                                        </ObjectAnimationUsingKeyFrames>
                                    </Storyboard>
                                </VisualState>
                            </VisualStateGroup>
                        </VisualStateManager.VisualStateGroups>
                        <ContentPresenter
                            HorizontalAlignment="Center"
                            VerticalAlignment="Center"
                            Content="{TemplateBinding Content}"
                            Foreground="{TemplateBinding Foreground}" />
                    </Border>
                </ControlTemplate>
            </Setter.Value>
        </Setter>
    </Style>
</Page.Resources>
```

---

## Notes

- Prefer `BasedOn` over copying the full WinUI 3 default template — it keeps your style
  smaller and automatically inherits future platform updates.
- Use `ThemeResource` (not `StaticResource`) for all colour/brush setters inside styles so
  they adapt to light/dark theme changes.
- Implicit styles (no `x:Key`) apply to the `TargetType` only within the scope of the
  `ResourceDictionary` they are declared in. Declaring them in `App.xaml` makes them global.
- Always merge `XamlControlsResources` **before** your own styles so your overrides win.
- Avoid `ControlTemplate` unless required; a mismatch with the expected visual states can
  break accessibility, animations, and theme transitions.
