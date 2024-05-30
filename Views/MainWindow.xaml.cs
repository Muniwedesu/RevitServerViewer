using RevitServerViewer.ViewModels;
using Splat;
using Wpf.Ui.Controls;

namespace RevitServerViewer.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        // this.WhenActivated(dr => { dr(this.OneWayBind(ViewModel, vm => vm.Router, v => v.ViewHost.Router)); });
    }

    private void FrameworkElement_OnLoaded(object sender, RoutedEventArgs e)
    {
        ((NavigationView)sender).Navigate("Export");
    }
}

public class MainWindowViewModel : ReactiveObject, IScreen
{
    // private IRoutableViewModel _exportViewModel;

    public MainWindowViewModel()
    {
        // _exportViewModel = new BulkExportViewModel(this);
        // Locator.CurrentMutable.Register(() => this, typeof(IScreen));
        // Router.Navigate.Execute(_exportViewModel).Subscribe();
    }

    public RoutingState Router { get; } = new RoutingState();
}