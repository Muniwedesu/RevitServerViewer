using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
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

public class ObservableRevitProcess : IDisposable
{
    private Process _process;
    private readonly IObservable<Unit> _obs;
    private CompositeDisposable _dr = new();
    private IObservable<Unit> _exitSwitch;
    public int Id { get; }
    public int SessionId { get; }
    public bool Exited => _process?.HasExited ?? true;

    public ObservableRevitProcess(string version)
    {
        // _exitSwitch = Observable.Interval(TimeSpan.FromMilliseconds(500))
        //     .Select(x => _process?.HasExited ?? false)
        //     .Buffer(2)
        //     .Where(x => x[0] != x[1] && x[1])
        //     .Select(_ => Unit.Default);
        _process = new Process();
        _dr.Add(_process);
        _process.EnableRaisingEvents = true;
        _process.StartInfo = new ProcessStartInfo($"C:\\Program Files\\Autodesk\\Revit {version}\\Revit.exe");
        _obs = Observable.FromEventPattern<EventHandler, EventArgs>(
                h => _process.Exited += h
                , h => _process.Exited -= h)
            .Select(_ => Unit.Default);
        // .Merge(_exitSwitch);
        _process.Start();
        Id = _process.Id;
        SessionId = _process.SessionId;
        Application.Current.Exit += CurrentOnExit;
    }

    private void CurrentOnExit(object sender, ExitEventArgs e)
    {
        if (!_process.HasExited) _process.Kill();
    }

    public void Kill() => _process.Kill();

    public IDisposable OnExit(Action handler)
    {
        return _obs.Subscribe(_ => { handler(); }).DisposeWith(_dr);
    }

    public void Dispose()
    {
        _dr.Dispose();
    }
}
// TODO: handle app closing unexpectedly 

public class RevitProxy : ReactiveObject
{
    [Reactive] public bool Exited { get; set; } = true;
    private PipeClient<SerializableMessage>? RvtPipe { get; set; }
    public SourceList<ModelOperationRequest> ActiveTasks { get; } = new();
    private readonly List<IDisposable> _pipeSubs = new();
    [Reactive] public bool Finished { get; set; }
    private static int _runningAppCount;
    public string ModelKey { get; set; }
    private readonly object _lock = new();
    private readonly IpcService _ipcSvc;
    private ObservableRevitProcess? _revit;
    private IObserver<ModelOperationStatusMessage>? _currentTaskObserver;

    public RevitProxy(string modelKey, string revitVersion)
    {
        //TODO: check available RAM, do not launch new instances until we have enough
        //TODO: properly close apps that have fininished (and decide when task is considered finished)
        //TODO: implement retrying 
        ModelKey = modelKey;
        _ipcSvc = Locator.Current.GetService<IpcService>()!;
        Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(UpdateConnectionStatus);
        ActiveTasks.Connect()
            .OnItemAdded(x =>
            {
                Observable.FromAsync(async () =>
                    {
                        if (_revit?.Exited ?? true) await CreateRevitProcess(revitVersion);
                    })
                    .Subscribe((_) =>
                    {
                        lock (_lock)
                        {
                            Observable.FromAsync(async () =>
                                    await RvtPipe!.WriteAsync(SerializableMessage.Create(x, x.GetType())))
                                .Subscribe(_ =>
                                {
                                    Debug.WriteLine("Request " + x.ModelKey + " " + x.GetType().Name + " sent");
                                });
                        }
                    });
            })
            .Subscribe(x => { Debug.WriteLine(x); });

        var pipes = PipeWatcher.GetActivePipes();
    }

    private async Task CreateRevitProcess(string versionString)
    {
        while (_runningAppCount >= _ipcSvc.MaxAppCount) await Task.Delay(1000);
        ++_runningAppCount;
        lock (_lock)
        {
            _revit = new ObservableRevitProcess(versionString);
            Exited = false;
            _revit.OnExit(OnProcessExited);
            RvtPipe = new PipeClient<SerializableMessage>(PipeNames.RevitServerModelDownloader
                                                          + $"_{_revit.Id}_{_revit.SessionId}");
            RvtPipe.AutoReconnect = true;
            _pipeSubs.ForEach(ps => ps.Dispose());
            _pipeSubs.Clear();
            _pipeSubs.Add(RvtPipe.OnConnected()
                .Subscribe(a => Debug.WriteLine(a.EventArgs.Connection.PipeName + " connected")));
            _pipeSubs.Add(RvtPipe.OnMessage().Subscribe(ProcessMessage));
        }
    }

    private void OnProcessExited() => _runningAppCount--;

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

    private void ProcessMessage(ConnectionMessageEventArgs<SerializableMessage> msg)
    {
        try
        {
            var message = System.Text.Json.JsonSerializer
                .Deserialize(msg.Message.SerializedString, Type.GetType(msg.Message.TypeName)!);

            if (message is ModelOperationStatusMessage statusMessage)
            {
                Debug.WriteLine("IPCsvc OnMessage : "
                                + statusMessage.OperationType + " "
                                + statusMessage.OperationStage);
                if (statusMessage.OperationStage is OperationStage.Completed or OperationStage.Error)
                {
                    _currentTaskObserver!.OnNext(statusMessage);
                    _currentTaskObserver!.OnCompleted();
                    Finished = true;
                    if (statusMessage.OperationType is OperationType.Save) _revit?.Kill();
                }
                else
                {
                    _currentTaskObserver!.OnNext(statusMessage);
                }
            }
        }
        catch
        {
            Debug.WriteLine(nameof(IpcService) + " EX : " + msg.Message.SerializedString);
        }
    }

    public IObservable<ModelOperationStatusMessage> Enqueue(ModelOperationRequest request)
    {
        Finished = false;
        var stageObservable = new Subject<ModelOperationStatusMessage>();
        _currentTaskObserver = stageObservable;
        ActiveTasks.Add(request);
        return stageObservable;
    }
}