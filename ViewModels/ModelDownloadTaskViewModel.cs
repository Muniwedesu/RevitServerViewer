using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Models.ServerContent;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDownloadTaskViewModel : ModelTaskViewModel
{
    [Reactive] public string CurrentFileName { get; set; } = string.Empty;
    [Reactive] public string DisplayFileIndex { get; set; } = string.Empty;
    [Reactive] public string DisplayFileCount { get; set; } = string.Empty;

    // /// <param name="key"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='key']"/></param>
    /// <param name="sourceFile"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='sourceFile']"/></param>
    /// <param name="outputFolder"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='outputFolder']"/></param>
    /// <param name="modifiedDate">Last time the RS rvt file was changed</param>
    public ModelDownloadTaskViewModel(string sourceFile, string outputFolder, DateTime modifiedDate)
        : base(key: sourceFile, sourceFile: sourceFile, outputFolder: outputFolder)
    {
        var paths = PathUtils.GetValidPaths(sourceFile, outputFolder);
        if (paths.Source != sourceFile) SourceFile = paths.Source;
        OutputFile = paths.Destination;
        OperationType = OperationType.Download;
        ModifiedDate = modifiedDate;
    }

    public DateTime ModifiedDate { get; set; }

    public override string OperationTypeString => "Загрузка модели";

    public override bool ExecuteCommand()
    {
        if (File.Exists(OutputFile))
        {
            //TODO: check dates or something (or move this to the downloader)
            var fi = new FileInfo(OutputFile);
            if (fi.LastWriteTimeUtc > ModifiedDate)
            {
                this.Stage = OperationStage.Completed;
                return true;
            }
        }

        var svc = Locator.Current.GetService<RevitServerService>()!;
        svc.AddDownload(SourceFile, OutputFile, OutputFolder)
            .ObserveOn(RxApp.MainThreadScheduler)
            .SubscribeSafe(Observer.Create<ModelDownloadStatusMessage>(UpdateStage, HandleException));
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