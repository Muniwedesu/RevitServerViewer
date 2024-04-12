using System.Globalization;
using System.Windows.Data;
using IBS.IPC.DataTypes;
using RevitServerViewer.ViewModels;
using Wpf.Ui.Controls;

namespace RevitServerViewer.Views;

public partial class ModelTaskView
{
    public ModelTaskView()
    {
        InitializeComponent();
        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.OperationTypeString, v => v.OperationTypeBox.Text));
            // dr(this.OneWayBind(ViewModel, vm => vm.Stage, v => v.OperationStageBox.Symbol
            //     , vmToViewConverterOverride: new StageIconConverter()));
            dr(this.Bind(ViewModel, vm => vm.StageDescription, v => v.OperationMessageBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.Elapsed, v => v.ElapsedBox.Text
                , vmToViewConverterOverride: new TimeSpanConverter()));
        });
    }
}

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