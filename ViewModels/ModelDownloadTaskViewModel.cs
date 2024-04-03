using System.IO;
using IBS.IPC.DataTypes;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDownloadTaskViewModel : ModelTaskViewModel
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="sourceFile">Path to the model on RS</param>
    /// <param name="outputFolder">Path to the output folder</param>
    public ModelDownloadTaskViewModel(string sourceFile, string outputFolder)
        : base(sourceFile, sourceFile, outputFolder)
    {
        var paths = PathUtils.GetValidPaths(sourceFile, outputFolder);
        if (paths.Source != sourceFile) SourceFile = paths.Source;
        OutputFile = paths.Destination;
    }

    public override OperationType OperationType => OperationType.Download;
    public override string OperationTypeString => "Загрузка";
    public sealed override string OutputFile { get; set; }

    public override bool Execute()
    {
        if (File.Exists(OutputFile))
        {
            this.Stage = OperationStage.Completed;
            return true;
        }

        var svc = Locator.Current.GetService<RevitServerService>()!;
        svc.AddDownload(SourceFile, OutputFile, OutputFolder)
            .Subscribe(x => { this.Stage = x; });
        return true;
    }
}