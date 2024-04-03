using System.Diagnostics;
using System.IO;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using Autodesk.Revit.DB;
using DynamicData;
using IBS.IPC.DataTypes;
using IBS.RevitServerTool;
using Splat;

namespace RevitServerViewer;

public class RevitServerService
{
    private RevitServerDownloader _dl;

    public SourceCache<ProcessingState, string> Operations { get; set; } = new(x => x.SourcePathFile);

    public RevitServerService()
    {
        _ipcSvc = Locator.Current.GetService<IpcService>()!;
    }

    private IScheduler _queue = new EventLoopScheduler();
    private IObservable<DownloadResult> _downloads = Observable.Empty<DownloadResult>();
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

    public IObservable<OperationStage> AddDownload(string modelPath)
    {
        var outputFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\{_dl.Host}\";
        var paths = PathUtils.GetValidPaths(modelPath, outputFolder);
        var st = new Subject<OperationStage>();
        st.OnNext(OperationStage.Requested);
        _downloads = _downloads.Concat(
            Observable.Start(() => _dl.Download(paths.Source, paths.Destination, outputFolder, st), _queue));
        return st;
    }

    public void AddDownloads(string[] modelPaths, string selectedServer, bool makeLocal)
    {
        Debugger.Launch();
        var outputFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\{selectedServer}\";
        _downloads = Observable.Empty<DownloadResult>();

        foreach (var modelPath in modelPaths)
        {
            var filePaths = PathUtils.GetValidPaths(modelPath, outputFolder);
            var st = CreateStateObservable(filePaths);
            if (File.Exists(filePaths.Destination))
                _downloads = _downloads.Concat(Observable.Return(new DownloadResult(filePaths.Source
                        , filePaths.Destination
                        , outputFolder))
                    .Delay(TimeSpan.FromMilliseconds(100), _queue));
            else
                _downloads = _downloads.Concat(Observable.Start(
                    () => _dl.Download(filePaths.Source, filePaths.Destination, outputFolder, st), _queue));
        }

        var sub = _ipcSvc.RevitMessages
            .Subscribe(msg =>
                {
                    Debug.WriteLine("IPC: " + msg.ModelKey + " " + msg.OperationType + " " + msg.OperationStage);
                    Operations.AddOrUpdate(ProcessingState.FromMessage(msg));
                }
                , () => { Debug.WriteLine("msg completed. +check if it's disposed properly"); });
        _downloads.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
        {
            //OnDownloadCompleted
            Operations.AddOrUpdate(new ProcessingState(x.Src, x.Dst, ProcessingStage.DownloadComplete));
            Debug.WriteLine(x.Src + " Downloaded");
#if DEBUG
            // Observable.Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.Downloading))
            //     .Delay(TimeSpan.FromSeconds(0.5))
            //     .Concat(Observable
            //         .Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.ExportingFromRevit))
            //         .Delay(_debugDownloadTime))
            //     .Concat(Observable
            //         .Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.Completed))
            //         .Delay(_debugExportTime))
            //     .ObserveOn(RxApp.MainThreadScheduler)
            //     .Subscribe(y => { Operations.AddOrUpdate(y); });
#endif
            // _ipcSvc.RequestOperation(new DetachModelRequest(x.Dst, string.Empty, x.Src));
            //TODO: out path
            _ipcSvc.RequestOperation(new ExportModelRequest(x.Dst, string.Empty, x.Src, x.OutFolder));

            //send request as observable 
            //merge with others
            //TODO: detach 
            //TODO: export
            //TODO: +Discard
            //TODO: start processing there if only detach/discard
            //send msg
        }, ex => { Debug.WriteLine(ex.Message); }, () =>
        {
            //OnAllDownloadsCompleted
            //TODO: +Clean
            var dl = Operations.Items;
        });
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