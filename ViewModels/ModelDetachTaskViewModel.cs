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
    }

    public override OperationType OperationType => OperationType.Detach;
    public override string OperationTypeString => "Отсоединение";
    public sealed override string OutputFile { get; set; }

    //TODO: maybe determine what to do in service
    //TODO: maybe use base class for ipc operations with the same execute method, idk
    public override bool Execute()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable = svc.RequestOperation(new DetachModelRequest(SourceFile, OutputFolder, SourceFile));
        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
        {
            this.Stage = x.OperationStage;
            this.StageDescription = x.OperationMessage;
        });
        return true;
    }
}