using System.Globalization;
using System.Windows.Data;
using RevitServerViewer.ViewModels;

namespace RevitServerViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow
{
    public MainWindow()
    {
        InitializeComponent();
        ViewModel = new MainWindowViewModel();
        this.WhenActivated(dr =>
        {
            dr(this.BindCommand(ViewModel, vm => vm.SaveModelsCommand, v => v.SaveButton));
            dr(this.OneWayBind(ViewModel, vm => vm.ServerList, v => v.ServerBox.ItemsSource));
            dr(this.OneWayBind(ViewModel, vm => vm.Downloads, v => v.DownloadBox.ItemsSource));
            dr(this.Bind(ViewModel, vm => vm.SelectedServer, v => v.ServerBox.SelectedItem));
            dr(this.Bind(ViewModel, vm => vm.DisplayModel, v => v.ViewHost.ViewModel));
            // dr(this.Bind(ViewModel, vm => vm.ConnectionString, v => v.ConnectionBox.Text));
            dr(this.Bind(ViewModel, vm => vm.SelectedVersion, v => v.VersionBox.SelectedItem));
            dr(this.Bind(ViewModel, vm => vm.MaxAppCount, v => v.MaxAppCountBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.ServerVersions, v => v.VersionBox.ItemsSource));
            dr(this.OneWayBind(ViewModel, vm => vm.IsStandalone, v => v.VersionBox.IsEnabled));
            dr(this.OneWayBind(ViewModel, vm => vm.SaveOptions, v => v.SaveOptionsHost.ViewModel));
        });
    }
}

public class ListWidthConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value - 15d;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return (double)value + 15d;
    }
}