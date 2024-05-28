using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using IBS.IPC.DataTypes;
using IBS.RevitServerTool;
using Splat;

namespace RevitServerViewer.Services;

public class RevitServerService
{
    private RevitServerDownloader _dl;

    public SourceCache<ProcessingState, string> Operations { get; set; } = new(x => x.SourcePathFile);

    public RevitServerService()
    {
        _ipcSvc = Locator.Current.GetService<IpcService>()!;
    }

    private IScheduler _queue = new EventLoopScheduler();
    private IObservable<ModelOperationStatusMessage> _downloads = Observable.Empty<ModelOperationStatusMessage>();
    private IpcService _ipcSvc;
    private TimeSpan _debugExportTime = TimeSpan.FromMilliseconds(4000);
    private TimeSpan _debugDownloadTime = TimeSpan.FromMilliseconds(2000);

    public void SetServer(string address, string version)
    {
        // ClearDownloads();
        _dl = new RevitServerDownloader(address, version);
    }

    public IObservable<IChangeSet<ProcessingState, string>> Connect() => Operations.Connect();

    public void ClearDownloads() => this.Operations.Clear();

    public IObservable<ModelDownloadStatusMessage> AddDownload(string srcFile, string outFile, string outFolder)
    {
        var st = _dl.Download(srcFile, outFile, outFolder)
            .ObserveOn(TaskPoolScheduler.Default)
            .SubscribeOn(TaskPoolScheduler.Default);
        //todo shouldn't I use merge?
        _downloads = _downloads.Merge(st);
        return st;
    }

    private ISubject<ProcessingStage> CreateStateObservable((string Source, string Destination) paths)
    {
        var st = new Subject<ProcessingStage>();
        // return st;
        st.ObserveOn(Scheduler.CurrentThread)
            .SubscribeOn(RxApp.MainThreadScheduler)
            .Subscribe(state =>
                Operations.AddOrUpdate(new ProcessingState(paths.Source, paths.Destination, state)));
        st.OnNext(ProcessingStage.Idle);

        return st;
    }

    private string GetDownloadStateMessage(ProcessingStage stage)
    {
        return stage switch
        {
            ProcessingStage.Idle => "Ожидание начала"
            , ProcessingStage.Started => "Операция начата"
            , ProcessingStage.Downloading => "Загрузка"
            , ProcessingStage.Saving => "Сохранение в .rvt"
            , ProcessingStage.DownloadComplete => "Загрузка завершена"
            , ProcessingStage.DownloadError => "Ошибка при загрузке"
            , ProcessingStage.SaveError => "Ошибка при сохранении"
            , ProcessingStage.Detaching => "Отсоединение"
            , ProcessingStage.DetachError => "Ошибка при отсоединении"
            , ProcessingStage.OpeningInRevit => "Открытие в Revit"
            , ProcessingStage.OpenError => "Ошибка при открытии"
            , ProcessingStage.ExportingFromRevit => "Экспорт из Revit"
            , ProcessingStage.ExportError => "Ошибка при экспорте"
            , ProcessingStage.Completed => "Завершено"
            , _ => throw new ArgumentOutOfRangeException(nameof(stage), stage, null)
        };
    }
}