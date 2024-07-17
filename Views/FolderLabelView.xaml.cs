using System.Windows.Input;

namespace RevitServerViewer.Views;

public partial class FolderLabelView
{
    public FolderLabelView()
    {
        InitializeComponent();
        this.WhenAnyValue(x => x.ViewModel).BindTo(this, x => x.DataContext);
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DisplayName, v => v.NameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.IsSelected, v => v.SelectedBox.IsChecked));
            dr(this.OneWayBind(ViewModel, vm => vm.Children, v => v.FolderItems.ItemsSource));
            dr(this.Bind(ViewModel, vm => vm.IsExpanded, v => v.FolderItems.Visibility
                , b => b ? Visibility.Visible : Visibility.Collapsed, visibility => visibility switch
                {
                    Visibility.Visible => true, _ => false
                }));
        });
    }

    private void ChevronMouseDown(object sender, MouseButtonEventArgs e)
    {
        ViewModel.ToggleExpand.Execute().Subscribe();
    }
}