using System.IO;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelExportTaskViewModel : ModelTaskViewModel
{
    public ModelExportTaskViewModel(string key, string sourceFile, string outputFolder) : base(key, sourceFile
        , outputFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        OutputFile = outputFolder;
    }

    public override OperationType OperationType { get; } = OperationType.Export;
    public override string OperationTypeString { get; } = "Экспорт";
    public sealed override string OutputFile { get; set; }

    //TODO: subscription method and other stuff can be generalized
    public override bool Execute()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable
            = svc.RequestOperation(new ExportModelRequest(SourceFile, OutputFolder, ModelKey, OutputFolder));
        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
        {
            this.Stage = x.OperationStage;
            this.StageDescription = x.OperationMessage;
        });
        return true;
    }
}