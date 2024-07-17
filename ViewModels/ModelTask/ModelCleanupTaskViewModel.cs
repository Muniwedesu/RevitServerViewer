using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

class ModelCleanupTaskViewModel : ModelTaskViewModel
{
    ///<inheritdoc/>
    public ModelCleanupTaskViewModel(string key, string sourceFile, string outputFolder)
        : base(key, sourceFile, outputFolder)
    {
        OutputFile = sourceFile;
        OperationType = OperationType.Cleanup;
    }

    public override string OperationTypeString => "Удаление неиспользуемых";

    public override bool ExecuteCommand()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable
            = svc.RequestOperation(new CleanUpModelRequest(ModelKey, SourceFile, OutputFolder, new string[] { }));

        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateStage);
        return true;
    }
}