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
    private IObservable<DownloadResult> _models = Observable.Empty<DownloadResult>();
    private IpcService _ipcSvc;
    private TimeSpan _debugExportTime = TimeSpan.FromMilliseconds(4000);
    private TimeSpan _debugDownloadTime = TimeSpan.FromMilliseconds(2000);

    public void SetServer(string address, string version)
    {
        ClearDownloads();
        _dl = new RevitServerDownloader(address, version);
    }

    public IObservable<IChangeSet<ProcessingState, string>> Connect() => Operations.Connect();

    public void ClearDownloads() => this.Operations.Clear();

    public void AddDownloads(string[] modelPaths, string selectedServer, bool makeLocal)
    {
        var dest = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\{selectedServer}";
        _models = Observable.Empty<DownloadResult>();


        var sub = _ipcSvc.RevitMessages.Subscribe(msg =>
            {
                Debug.WriteLine(msg.ModelKey + " " + msg.OperationStatus);
                Operations.AddOrUpdate(new ProcessingState(msg.ModelKey, msg.RvtLocation, msg.OperationType switch
                {
                    OperationType.Detach => msg.OperationStatus == OperationStatus.Error
                        ? ProcessingStage.DetachError
                        : ProcessingStage.Detaching
                    // , OperationType.Cleanup => msg.OperationStatus == OperationStatus.Error
                    //     ? ProcessingStage.DetachError
                    //     : ProcessingStage.Detaching
                    // , OperationType.DiscardLinks => msg.OperationStatus == OperationStatus.Error
                    //     ? ProcessingStage.DetachError
                    //     : ProcessingStage.Detaching
                    , OperationType.Export => msg.OperationStatus == OperationStatus.Error
                        ? ProcessingStage.ExportError
                        : ProcessingStage.ExportingFromRevit
                    , _ => throw new ArgumentOutOfRangeException()
                }));
            }
            , () => { Debug.WriteLine("msg completed. +check if it's disposed properly"); });
        foreach (var modelPath in modelPaths)
        {
            var paths = PathUtils.GetValidPaths(modelPath, dest);
            var st = CreateStateObservable(paths);
            if (File.Exists(paths.Destination))
            {
                _models = _models.Concat(Observable.Return(new DownloadResult(paths.Source, paths.Destination))
                    .Delay(TimeSpan.FromMilliseconds(500), _queue));
            }
            else
            {
                _models = _models.Concat(Observable.Start(() => _dl.Download(paths.Source, paths.Destination, st)
                    , _queue));
            }
            // _models.Concat(Observable.Return(new DownloadResult(paths.Source, paths.Destination)));
        }

        _models.ObserveOn(RxApp.MainThreadScheduler).Subscribe(x =>
        {
            //OnDownloadCompleted
            Operations.AddOrUpdate(new ProcessingState(x.Src, x.Dst, ProcessingStage.DownloadComplete));
            Observable.Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.Downloading))
                .Delay(TimeSpan.FromSeconds(0.5))
                .Concat(Observable
                    .Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.ExportingFromRevit))
                    .Delay(_debugDownloadTime))
                .Concat(Observable
                    .Return(new ProcessingState(x.Src, x.Dst, ProcessingStage.Completed))
                    .Delay(_debugExportTime))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(y => { Operations.AddOrUpdate(y); });
            // _ipcSvc.RequestOperation(new DetachModelRequest(x.Dst, string.Empty, x.Src));
            //TODO: out path
            // _ipcSvc.RequestOperation(new ExportModelRequest(x.Dst, string.Empty, x.Src, String.Empty));

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
        return st;
        st.ObserveOn(Scheduler.CurrentThread)
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