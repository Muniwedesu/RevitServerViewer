namespace RevitServerViewer;

public partial class FolderView
{
    public FolderView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DisplayName, v => v.NameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.IsSelected, v => v.SelectedBox.IsChecked));
        });
    }
}