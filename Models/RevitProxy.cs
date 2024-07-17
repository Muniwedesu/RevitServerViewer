using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using H.Pipes;
using H.Pipes.Args;
using IBS.IPC;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace RevitServerViewer.Services;

// TODO: handle app closing unexpectedly 

public class RevitProxy : ReactiveObject
{
    [Reactive] public bool Exited { get; set; } = true;
    [Reactive] public bool IsIdle { get; set; }
    private PipeClient<SerializableMessage>? RvtPipe { get; set; }

    /// <summary>
    /// Posts request as soon as it is added
    /// S
    /// <remarks>Order of execution is controlled from outside</remarks>
    /// </summary>
    public SourceList<ModelOperationRequest> RevitRequests { get; } = new();

    private readonly List<IDisposable> _pipeSubs = new();
    private static int _runningAppCount;
    public string ModelKey { get; set; }
    private readonly object _lock = new();
    private readonly IpcService _ipcSvc;
    private ObservableRevitProcess? _revit;
    private IObserver<ModelOperationStatusMessage>? _currentTaskObserver;
    private Serilog.ILogger? _log;

    public RevitProxy(string modelKey, string revitVersion)
    {
        _log = Locator.Current.GetService<Serilog.ILogger>();
        //TODO: implement retrying when app crashed
        ModelKey = modelKey;
        _ipcSvc = Locator.Current.GetService<IpcService>()!;
        Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(UpdateConnectionStatus);

        RevitRequests.Connect()
            .OnItemAdded(x =>
                Observable.FromAsync(async () =>
                        await (
                            _revit?.Exited ?? true
                                ? CreateRevitProcess(revitVersion)
                                : Task.FromResult(_revit))
                    )
                    .Subscribe(_ => SendMessage(x)))
            .Subscribe(x => { _log?.Information("{@Current}", x.FirstOrDefault()?.Item.Current); });
    }

    private void SendMessage(ModelOperationRequest x)
    {
        lock (_lock)
        {
            Observable.FromAsync(async () =>
                    await RvtPipe!.WriteAsync(SerializableMessage.Create(x, x.GetType())))
                .Subscribe(_ => { _log?.Information("Request {@X} sent", x); });
        }
    }

    private async Task<ObservableRevitProcess> CreateRevitProcess(string versionString)
    {
        while (_runningAppCount >= _ipcSvc.MaxAppCount) await Task.Delay(1000);
        ++_runningAppCount;
        lock (_lock)
        {
            _revit = new ObservableRevitProcess(versionString);
            _log?.Information("Process {Id} started", _revit.Id);
// #if DEBUG
//             Process.Start("vsjitdebugger.exe", $"-p {_revit.Id}");
// #endif
            Exited = false;
            _revit.OnExit(() => OnProcessExited(_revit.Id));
            // created here because process may exit or smth
            RvtPipe = new PipeClient<SerializableMessage>(PipeNames.RevitServerModelDownloader
                                                          + $"_{_revit.Id}_{_revit.SessionId}");
            RvtPipe.AutoReconnect = true;
            _pipeSubs.ForEach(ps => ps.Dispose());
            _pipeSubs.Clear();
            _pipeSubs.Add(RvtPipe.OnConnected()
                .Subscribe(a => _log?.Information(a.EventArgs.Connection.PipeName + " connected")));
            _pipeSubs.Add(RvtPipe.OnMessage().Subscribe(ProcessMessage));
        }

        return _revit;
    }

    private void OnProcessExited(int pId)
    {
        _log?.Information("Process {PId} exited", pId);
        _runningAppCount--;
        //TODO: determine if we have any running tasks
        if (!IsIdle)
        {
            var last = RevitRequests.Items.Last();
            OnStatusMessage(
                new ModelOperationStatusMessage(last.ModelKey, last.SrcFile, last.Kind, OperationStage.Requested)
                    .Error("Revit закрылся"));
        }
        //if we do - restart 
        // if (RevitRequests.Count > 0)
    }

    private void UpdateConnectionStatus(long _)
    {
        try
        {
#if DEBUG
            // H.Pipes.PipeWatcher.CreateAndStart().SubscribeToCreated((w, args) =>
            // {
            //     args.Name;
            // });
            var pipes = PipeWatcher.GetActivePipes().Where(p => p.Contains("IBS")).ToArray();
#endif
            if (RvtPipe is { IsConnected: false, IsConnecting: false }) RvtPipe.ConnectAsync().Wait(500);
        }
        catch { }
    }

    private void OnStatusMessage(ModelOperationStatusMessage statusMessage)
    {
        _log?.Information("Task status: "
                          + statusMessage.OperationType + " "
                          + statusMessage.OperationStage);
        if (statusMessage.OperationStage is OperationStage.Completed or OperationStage.Error)
        {
            _currentTaskObserver!.OnNext(statusMessage);
            _currentTaskObserver!.OnCompleted();
            RevitRequests.RemoveAt(0);
            IsIdle = true;
            if (statusMessage.OperationType is OperationType.Save) _revit?.Kill();
        }
        else
        {
            _currentTaskObserver!.OnNext(statusMessage);
        }
    }

    private void ProcessMessage(ConnectionMessageEventArgs<SerializableMessage> msg)
    {
        try
        {
            var message = System.Text.Json.JsonSerializer
                .Deserialize(msg.Message.SerializedString, Type.GetType(msg.Message.TypeName)!);

            if (message is ModelOperationStatusMessage statusMessage) OnStatusMessage(statusMessage);
        }
        catch (Exception e)
        {
            _log?.Error(e, nameof(IpcService) + " EX: " + msg.Message.SerializedString);
        }
    }

    /// <summary>
    /// Createas an Observable which posts execution stages
    /// </summary>
    /// <param name="request">Information about the task</param>
    /// <returns></returns>
    public IObservable<ModelOperationStatusMessage> Enqueue(ModelOperationRequest request)
    {
        var stageObservable = new Subject<ModelOperationStatusMessage>();
        _currentTaskObserver = stageObservable;
        RevitRequests.Add(request);
        IsIdle = false;
        return stageObservable;
    }
}