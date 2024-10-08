﻿using RevitServerViewer.ViewModels;

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