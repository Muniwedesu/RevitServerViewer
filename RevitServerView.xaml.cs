namespace RevitServerViewer;

public partial class RevitServerView
{
    public RevitServerView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.OneWayBind(ViewModel, vm => vm.Folders, v => v.TreeRoot.ItemsSource));
        });
    }
}