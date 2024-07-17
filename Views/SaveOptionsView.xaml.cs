using System;
using System.Reactive;
using System.Reactive.Disposables;
using ReactiveUI;
using RevitServerViewer.ViewModels;

namespace RevitServerViewer.Views;

public partial class SaveOptionsView
{
    public SaveOptionsView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DetachEnabled, v => v.DetachBox.IsEnabled));
            dr(this.Bind(ViewModel, vm => vm.CleanEnabled, v => v.CleanupBox.IsEnabled));

            dr(this.Bind(ViewModel, vm => vm.IsDetaching, v => v.DetachBox.IsChecked));
            // dr(this.Bind(ViewModel, vm => vm.IsGeneratingError, v => v.ErrorBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.IsCleaning, v => v.CleanupBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.IsExporting, v => v.ExportBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.IsDiscarding, v => v.DiscardBox.IsChecked));
        });
    }
}