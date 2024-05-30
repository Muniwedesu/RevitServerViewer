using System.IO;
using System.Reactive.Linq;
using IBS.IPC.DataTypes;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelExportTaskViewModel : ModelTaskViewModel
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="key"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='key']"/></param>
    /// <param name="sourceFile"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='sourceFile']"/></param>
    /// <param name="outputFolder"><inheritdoc cref="ModelTaskViewModel(string,string,string)"  path="/param[@name='outputFolder']"/></param>
    /// <param name="settings">Navisworks export settings</param>
    public ModelExportTaskViewModel(string key, string sourceFile, string outputFolder
        , NavisworksExportSettings settings)
        : base(key: key, sourceFile: sourceFile, outputFolder: outputFolder)
    {
        var filename = Path.GetFileName(sourceFile);
        ExportSettings = settings;
        OutputFile = outputFolder;
        OperationType = OperationType.Export;
    }

    public NavisworksExportSettings ExportSettings { get; set; }

    public override string OperationTypeString { get; } = "Экспорт";

    public override bool ExecuteCommand()
    {
        var svc = Locator.Current.GetService<IpcService>()!;
        var stageObservable
            = svc.RequestOperation(new ExportModelRequest(SourceFile, OutputFolder, ModelKey, OutputFolder
                , ExportSettings));
        stageObservable.ObserveOn(RxApp.MainThreadScheduler).Subscribe(UpdateStage);
        return true;
    }
}