using RevitServerViewer.ViewModels;

namespace RevitServerViewer.Views;

public partial class ModelDownloadTaskView
{
    public ModelDownloadTaskView()
    {
        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.OperationTypeString, v => v.OperationTypeBox.Text));
            // dr(this.OneWayBind(ViewModel, vm => vm.Stage, v => v.OperationStageBox.Symbol
            //     , vmToViewConverterOverride: new StageIconConverter()));
            dr(this.OneWayBind(ViewModel, vm => vm.DisplayFileCount, v => v.FileCountBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.DisplayFileIndex, v => v.FileIndexBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.CurrentFileName, v => v.FileNameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.StageDescription, v => v.OperationMessageBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.Elapsed, v => v.ElapsedBox.Text
                , vmToViewConverterOverride: new TimeSpanConverter()));
        });
    }
}