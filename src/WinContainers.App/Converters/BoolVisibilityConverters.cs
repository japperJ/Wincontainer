using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace WinContainers_App.Converters;

/// <summary>
/// Converts bool (IsExpanded) to expand/collapse glyph string.
/// True -> "▼", False -> "▶".
/// </summary>
public sealed class ExpandCollapseConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool isExpanded)
            return isExpanded ? "▼" : "▶";
        return "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return value is string s && s == "▼";
    }
}

/// <summary>
/// Converts bool to Visibility.
/// True -> Visible, False -> Collapsed.
/// </summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
            return v == Visibility.Visible;
        return false;
    }
}

/// <summary>
/// Converts bool to Visibility (inverse).
/// True -> Collapsed, False -> Visible.
/// </summary>
public sealed class InverseBoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b ? Visibility.Visible : Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
            return v != Visibility.Visible;
        return true;
    }
}

/// <summary>
/// Inverts a bool value. True -> False, False -> True.
/// </summary>
public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return true;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
            return !b;
        return true;
    }
}

/// <summary>
/// Converts a non-empty string to Visibility.
/// </summary>
public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
            return v == Visibility.Visible ? string.Empty : string.Empty;
        return string.Empty;
    }
}

public sealed class PermissionsToNumericConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is string s ? WinContainers_App.ViewModels.ContainerDetailViewModel.ConvertPermissionsToNumeric(s) : "?";
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value ?? string.Empty;
}
