using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Win32;
using RevitServerViewer.ViewModels;
using Splat;

namespace RevitServerViewer.Views;

public partial class BulkExportView
{
    public BulkExportView()
    {
        InitializeComponent();

        this.WhenActivated(dr =>
        {
            var viewModel = Locator.Current.GetService<BulkExportViewModel>();
            if (viewModel is null)
            {
                viewModel = new BulkExportViewModel();
                Locator.CurrentMutable.RegisterConstant(viewModel, typeof(BulkExportViewModel));
            }

            var obs = this.VersionBox.WhenAnyValue(x => x.SelectedItem).WhereNotNull();
            ViewModel = viewModel;
            ViewModel.SavePathInteraction.RegisterHandler(SetSavePathHandler);
            dr(this.OneWayBind(ViewModel, vm => vm.ServerList, v => v.ServerBox.ItemsSource));
            dr(this.Bind(ViewModel, vm => vm.SelectedServer, v => v.ServerBox.SelectedItem));

            dr(this.BindCommand(ViewModel, vm => vm.SaveModelsCommand, v => v.SaveButton));
            dr(this.Bind(ViewModel, vm => vm.DisplayedViewModel, v => v.ViewHost.ViewModel));
            // dr(this.Bind(ViewModel, vm => vm.ConnectionString, v => v.ConnectionBox.Text));
            dr(this.Bind(ViewModel, vm => vm.SelectedVersion, v => v.VersionBox.SelectedItem
                , obs, triggerUpdate: TriggerUpdate.ViewToViewModel));
            dr(this.Bind(ViewModel, vm => vm.MaxAppCount, v => v.MaxAppCountBox.Text));
            dr(this.OneWayBind(ViewModel, vm => vm.ServerVersions, v => v.VersionBox.ItemsSource));
            dr(this.OneWayBind(ViewModel, vm => vm.IsStandalone, v => v.VersionBox.IsEnabled));
            dr(this.OneWayBind(ViewModel, vm => vm.SaveOptions, v => v.SaveOptionsHost.ViewModel));
            dr(this.OneWayBind(ViewModel, vm => vm.ProcessesViewModel, v => v.ProcessesHost.ViewModel));
            dr(this.Bind(ViewModel, vm => vm.SavePath, v => v.SavePathBox.Text));
            // dr(this.BindCommand(ViewModel, vm => vm.GoToSettingsCommand, v => v.GoToSettingsButton));
        });
    }

    private void SetSavePathHandler(IInteractionContext<string, string> interaction)
    {
        var dlg = new FolderPicker
        {
            InputPath = interaction.Input
            , ForceFileSystem = false
            , Multiselect = false
            , Title = "Выбор папки для сохранения"
            , OkButtonLabel = "Выбрать"
            , FileNameLabel = null
        };
        if (dlg.ShowDialog(Application.Current.MainWindow, false) == true)
        {
            interaction.SetOutput(dlg.ResultPath);
        }
        else interaction.SetOutput(interaction.Input);
    }

    private void PathTextBoxMouseUp(object sender, MouseButtonEventArgs e)
    {
        ViewModel.SetPathCommand.Execute().Subscribe();
    }
}