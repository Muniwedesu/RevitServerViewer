namespace RevitServerViewer;

public partial class OperationResultView
{
    public OperationResultView()
    {
        InitializeComponent();
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.DisplayStartupTime, v => v.StartupBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Name, v => v.NameBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Name, v => v.NameToolTip.Content));
            dr(this.Bind(ViewModel, vm => vm.State, v => v.StateBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Elapsed, v => v.ElapsedBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Message, v => v.MessageBox.Text));
            dr(this.Bind(ViewModel, vm => vm.Message, v => v.MessageToolTip.Content));
        });
    }
}