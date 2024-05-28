using RevitServerViewer.ViewModels;

namespace RevitServerViewer.Views;

public partial class ModelProcessView
{
    public ModelProcessView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DisplayStartupTime, v => v.StartupBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Name, v => v.NameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Name, v => v.NameToolTip.Content));
            dr(this.Bind(ViewModel, vm => vm.CurrentTask, v => v.TaskHost.ViewModel));
            dr(this.OneWayBind(ViewModel, vm => vm.Elapsed, v => v.ElapsedBox.Text
                , vmToViewConverterOverride: new TimeSpanConverter()));
            // dr(this.Bind(ViewModel, vm => vm.Message, v => v.MessageBox.Text));
            // dr(this.Bind(ViewModel, vm => vm.Message, v => v.MessageToolTip.Content));
        });
    }

    private void ScrollViewer_OnSizeChanged(object? sender, EventArgs e)
    {
        ((ScrollViewer)sender).ScrollToEnd();
    }
}