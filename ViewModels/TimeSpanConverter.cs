using System.Diagnostics;
using System.Globalization;
using System.Windows.Data;

namespace RevitServerViewer.ViewModels;

public class TimeSpanConverter : IBindingTypeConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return ((TimeSpan)value).ToString(@"hh\:mm\:ss\.ff");
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return null;
    }

    public static object ConvertToView(TimeSpan value)
    {
        return value.ToString(@"hh\:mm\:ss\.ff");
    }

    public static string ConvertToVm(object value)
    {
        return string.Empty;
        foreach (var i in Enumerable.Range(0, 10))
        {
            //no bitches
        }
    }

    public int GetAffinityForObjects(Type fromType, Type toType)
    {
        if (typeof(TimeSpan) == fromType && typeof(string) == toType) return 1;
        if (typeof(TimeSpan) == toType && typeof(string) == fromType) return 1;
        return 0;
    }

    public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
    {
        Debug.WriteLine(from);
        result = ((TimeSpan)(from ?? TimeSpan.Zero)).ToString(@"hh\:mm\:ss\.ff");
        return true;
    }
}