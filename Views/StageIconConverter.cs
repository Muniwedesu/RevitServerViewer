using System.Globalization;
using System.Windows.Data;
using IBS.IPC.DataTypes;
using Wpf.Ui.Controls;

namespace RevitServerViewer.Views;

public class StageIconConverter : IBindingTypeConverter, IValueConverter
{
    public int GetAffinityForObjects(Type fromType, Type toType)
    {
        return 1;
    }

    public bool TryConvert(object? from, Type toType, object? conversionHint, out object? result)
    {
        result = from switch
        {
            OperationStage.Requested => SymbolRegular.PauseCircle24
            , OperationStage.Started => SymbolRegular.PlayCircle24
            , OperationStage.Completed => SymbolRegular.CheckmarkCircle24
            , OperationStage.Error => SymbolRegular.ErrorCircle24
        };
        return true;
    }

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value switch
        {
            OperationStage.Requested => SymbolRegular.PauseCircle24
            , OperationStage.Started => SymbolRegular.PlayCircle24
            , OperationStage.Completed => SymbolRegular.CheckmarkCircle24
            , OperationStage.Error => SymbolRegular.ErrorCircle24
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}