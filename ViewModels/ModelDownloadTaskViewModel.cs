using System.IO;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDownloadTaskViewModel : ModelTaskViewModel
{
    [Reactive] public string CurrentFileName { get; set; }
    [Reactive] public string DisplayFileIndex { get; set; }
    [Reactive] public string DisplayFileCount { get; set; }

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
        // if (File.Exists(OutputFile))
        // {
        //     //TODO: check dates or something (or move this to the downloader)s
        //     this.Stage = OperationStage.Completed;
        //     return true;
        // }

        var svc = Locator.Current.GetService<RevitServerService>()!;
        svc.AddDownload(SourceFile, OutputFile, OutputFolder)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(UpdateStage);
        return true;
    }

    public void UpdateStage(ModelDownloadStatusMessage msg)
    {
        this.Stage = msg.OperationStage;
        this.StageDescription = msg.OperationMessage;
        this.DisplayFileIndex = msg.FileIndex.ToString();
        this.DisplayFileCount = msg.FileCount == 0 ? string.Empty : "/" + msg.FileCount.ToString();
        this.CurrentFileName = msg.FileName ?? string.Empty;
    }
}