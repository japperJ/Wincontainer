using Windows.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace WinContainers_App.Converters;

public sealed class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var status = value as string ?? string.Empty;
        var (r, g, b) = status switch
        {
            var s when s.StartsWith("Up", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Running", StringComparison.OrdinalIgnoreCase)
                => ((byte)0, (byte)200, (byte)83),
            var s when s.StartsWith("Exited", StringComparison.OrdinalIgnoreCase) || s.StartsWith("Stopped", StringComparison.OrdinalIgnoreCase)
                => ((byte)220, (byte)20, (byte)60),
            var s when s.StartsWith("Paused", StringComparison.OrdinalIgnoreCase) || s == "Partial"
                => ((byte)255, (byte)179, (byte)0),
            _ => ((byte)158, (byte)158, (byte)158)
        };
        return new SolidColorBrush(Color.FromArgb(255, r, g, b));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
