using System.IO;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

class ModelCleanupTaskViewModel : ModelTaskViewModel
{
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
            = svc.RequestOperation(new CleanUpModelRequest(SourceFile, SourceFile, OutputFolder, new string[] { }));

        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateStage);
        return true;
    }
}

public class ModelDownloadTaskViewModel : ModelTaskViewModel
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceFile">Path to the model on RS</param>
    /// <param name="outputFolder">Path to the output folder root</param>
    public ModelDownloadTaskViewModel(string sourceFile, string outputFolder)
        : base(key: sourceFile, sourceFile: sourceFile, outputFolder: outputFolder)
    {
        var paths = PathUtils.GetValidPaths(sourceFile, outputFolder);
        if (paths.Source != sourceFile) SourceFile = paths.Source;
        OutputFile = paths.Destination;
        OperationType = OperationType.Download;
    }

    public override string OperationTypeString => "Загрузка модели";

    public override bool ExecuteCommand()
    {
        if (File.Exists(OutputFile))
        {
            this.Stage = OperationStage.Completed;
            return true;
        }

        var svc = Locator.Current.GetService<RevitServerService>()!;
        svc.AddDownload(SourceFile, OutputFile, OutputFolder)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateStage);
        return true;
    }
}