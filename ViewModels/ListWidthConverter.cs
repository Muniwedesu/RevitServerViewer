using System.Globalization;
using System.Windows.Data;

namespace RevitServerViewer.Views;

public class ListWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value - 15d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value + 15d;
    }
}