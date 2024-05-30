using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace RevitServerViewer.ViewModels;

public class TimeSpanConverter : IBindingTypeConverter
{
    public const string Format = @"hh\:mm\:ss";

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((TimeSpan)value).ToString(Format);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }

    public static object ConvertToView(TimeSpan value)
    {
        return value.ToString(Format);
    }

    public static string ConvertToVm(object value)
    {
        return string.Empty;
    }

    public int GetAffinityForObjects(Type fromType, Type toType)
    {
        if (typeof(TimeSpan) == fromType && typeof(string) == toType) return 1;
        if (typeof(TimeSpan) == toType && typeof(string) == fromType) return 1;
        return 0;
    }

    public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
    {
        result = ((TimeSpan)(from ?? TimeSpan.Zero)).ToString(Format);
        return true;
    }
}