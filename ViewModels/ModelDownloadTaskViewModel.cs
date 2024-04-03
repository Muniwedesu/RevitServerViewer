using IBS.IPC.DataTypes;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDownloadTaskViewModel : ModelTaskViewModel
{
    public ModelDownloadTaskViewModel(string sourceFile, string destinationFile)
        : base(sourceFile, sourceFile, destinationFile) { }

    public override OperationType OperationType => OperationType.Download;
    public override string OperationTypeString => "Загрузка";

    public override bool Execute()
    {
        var svc = Locator.Current.GetService<RevitServerService>()!;
        svc.AddDownload(SourceFile)
            .Subscribe(x =>
            {
                this.Stage = x;
                IsFinished = x is OperationStage.Error or OperationStage.Completed;
                IsActive = x == OperationStage.Started;
            });
        return true;
    }
}