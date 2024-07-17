namespace RevitServerViewer.Views;

public partial class RevitServerTreeView
{
    public RevitServerTreeView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.OneWayBind(ViewModel, vm => vm.Folders, v => v.TreeRoot.ItemsSource));
        });
    }
}