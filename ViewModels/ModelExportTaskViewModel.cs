using System.IO;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelExportTaskViewModel : ModelTaskViewModel
{
    public ModelExportTaskViewModel(string key, string sourceFile, string outputFolder)
        : base(key: key, sourceFile: sourceFile, outputFolder: outputFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        OutputFile = outputFolder;
        OperationType = OperationType.Export;
    }

    public override string OperationTypeString { get; } = "Экспорт";

    public override bool ExecuteCommand()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable
            = svc.RequestOperation(new ExportModelRequest(SourceFile, OutputFolder, ModelKey, OutputFolder));
        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateStage);
        return true;
    }
}