using System.Diagnostics;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDetachTaskViewModel : ModelTaskViewModel
{
    public ModelDetachTaskViewModel(string key, string sourceFile) : base(key, sourceFile, sourceFile) { }
    public override OperationType OperationType => OperationType.Detach;
    public override string OperationTypeString => "Отсоединение";
    public override string OutputFile { get; set; }

    //TODO: maybe determine what to do in service
    //TODO: maybe use base class for ipc operations with the same execute method, idk
    public override bool Execute()
    {
        var svc = Locator.Current.GetService<RevitServerService>();
        Observable.Timer(TimeSpan.FromSeconds(2)).Subscribe(_ =>
        {
            this.Stage = OperationStage.Started;
            Observable.Timer(TimeSpan.FromSeconds(4)).Subscribe(_ => { this.Stage = OperationStage.Completed; });
        });
        return true;
    }
}