namespace RevitServerViewer.Views;

public partial class ModelView
{
    public ModelView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DisplayName, v => v.NameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.IsSelected, v => v.SelectedBox.IsChecked));
        });
    }
}