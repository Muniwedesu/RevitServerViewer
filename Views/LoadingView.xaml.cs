namespace RevitServerViewer.Views;

public partial class LoadingView
{
    public LoadingView()
    {
        InitializeComponent();
        this.WhenActivated(dr => { dr(this.Bind(ViewModel, vm => vm.StateText, v => v.StateBox.Text)); });
    }
}