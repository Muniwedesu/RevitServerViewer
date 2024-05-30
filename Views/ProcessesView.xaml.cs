using RevitServerViewer.ViewModels;
using Splat;

namespace RevitServerViewer.Views;

public partial class ProcessesView
{
    public ProcessesView()
    {
        InitializeComponent();
        ViewModel ??= Locator.Current.GetService<ProcessesViewModel>();
        this.WhenActivated(dr =>
        {
            dr(this.OneWayBind(ViewModel, vm => vm.Downloads, v => v.DownloadBox.ItemsSource));
        });
    }
}