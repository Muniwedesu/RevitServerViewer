using System.Diagnostics;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDetachTaskViewModel : ModelTaskViewModel
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key">Path on the RS </param>
    /// <param name="sourceFile">Path to the .rvt file</param>
    public ModelDetachTaskViewModel(string key, string sourceFile) : base(key, sourceFile, sourceFile)
    {
        OutputFile = sourceFile;
        OperationType = OperationType.Detach;
    }

    //TODO: maybe determine what to do in service
    //TODO: maybe use base class for ipc operations with the same execute method, idk
    public override string OperationTypeString => "Отсоединение";

    public override bool ExecuteCommand()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable = svc.RequestOperation(new DetachModelRequest(srcFile: SourceFile
            , pathRoot: OutputFolder
            , modelKey: ModelKey));
        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateStage);
        return true;
    }
}