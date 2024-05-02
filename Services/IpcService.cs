using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Autodesk.Revit.DB.Events;
using DynamicData;
using DynamicData.Kernel;
using H.Pipes;
using H.Pipes.Args;
using IBS.IPC;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.Services;

public class IpcService : ReactiveObject
{
    /// <summary>
    /// 
    /// </summary>
    public SourceCache<ModelOperationRequest, string> _activeTasks = new(x => x.ModelKey);

    /// <summary>
    /// dict of active pipes
    /// </summary>
    public Dictionary<string, PipeClient<SerializableMessage>> _clients = new();

    /// <summary>
    /// to route incoming messages to the observing tasks
    /// </summary>
    public Dictionary<string, Queue<ISubject<ModelOperationStatusMessage>>> _stageMessages = new();

    /// <summary>
    /// running revits 
    /// </summary>
    private Dictionary<string, Process> _processes = new();

    [Reactive] public string RevitVersion { get; set; }

    private object _clients_lock = new();

    public IpcService()
    {
#if DEBUG
        Debugger.Launch();
#endif
        // _client.OnMessage().Subscribe(ProcessMessage);
        _activeTasks.Connect()
            .OnItemUpdated((x, xx) =>
            {
                Observable.FromAsync(async () =>
                        await _clients[x.ModelKey].WriteAsync(SerializableMessage.Create(x, x.GetType())))
                    .Subscribe(_ => { Debug.WriteLine("Request " + x.ModelKey); });
            })
            .OnItemAdded(x =>
            {
                Observable.FromAsync(async () =>
                        await _clients[x.ModelKey].WriteAsync(SerializableMessage.Create(x, x.GetType())))
                    .Subscribe(_ => { Debug.WriteLine("Request " + x.ModelKey); });
            })
            .OnItemRemoved(_ =>
            {
                // if (!_activeTasks.Items.Any())
                // {
                //     _revitMessages.OnCompleted();
                // }
            })
            .Subscribe(x => { Debug.WriteLine(x); });

        // _client.OnDisconnected().ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(_ => { Application.Current.Shutdown(); });
        // _client.ConnectAsync().Wait(3000);

        Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(UpdateConnectionStatus);
        // if (!_client.IsConnected) ;
    }

    private void ProcessMessage(ConnectionMessageEventArgs<SerializableMessage> msg)
    {
        try
        {
            var message = System.Text.Json.JsonSerializer
                .Deserialize(msg.Message.SerializedString, Type.GetType(msg.Message.TypeName));

            if (message is ModelOperationStatusMessage statusMessage)
            {
                Debug.WriteLine("IPCsvc OnMessage : "
                                + statusMessage.OperationType + " "
                                + statusMessage.OperationStage);
                if (statusMessage.OperationStage is OperationStage.Completed or OperationStage.Error)
                {
                    var st = _stageMessages[statusMessage.ModelKey].Dequeue();
                    st.OnNext(statusMessage);
                    st.OnCompleted();
                    if (statusMessage.OperationStage is OperationStage.Completed)
                        _processes[statusMessage.ModelKey].Kill();
                    //TODO: this isn't very reliable
                    _activeTasks.RemoveKey(statusMessage.ModelKey);
                }
                else
                {
                    _stageMessages[statusMessage.ModelKey].Peek().OnNext(statusMessage);
                }

                // _revitMessages.OnNext(statusMessage);
                // if (statusMessage.OperationStage is OperationStage.Completed or OperationStage.Error)
                //     _activeTasks.RemoveKey(statusMessage.ModelKey);
            }
            else if (message is string version)
            {
                RevitVersion = version;
            }
        }
        catch
        {
            Debug.WriteLine(nameof(IpcService) + " EX : " + msg.Message.SerializedString);
        }
    }

    private void UpdateConnectionStatus(long _)
    {
        try
        {
            lock (_clients_lock)
            {
                foreach (var p in _clients.Values.Where(p => !p.IsConnected))
                    p.ConnectAsync().Wait(500);
            }
        }
        catch
        {
            //TODO: fix this (collection modified etc)
        }
    }


    public IObservable<ModelOperationStatusMessage> RequestOperation(ModelOperationRequest request)
    {
        //check if we have space first
        //then add a new one ig?
        if (!_clients.TryGetValue(request.ModelKey, out var client))
        {
            lock (_clients_lock)
            {
                //TODO: check available RAM, do not launch new instances until we have enough
                //TODO: properly close apps that have fininished
                //TODO: implement retrying 
                var revit = Process.Start($"C:\\Program Files\\Autodesk\\Revit {RevitVersion}\\Revit.exe");

                var p = new PipeClient<SerializableMessage>(PipeNames.RevitServerModelDownloader
                                                            + $"_{revit.Id}_{revit.SessionId}");
                _clients.Add(request.ModelKey, p);
                _processes.Add(request.ModelKey, revit);
                p.AutoReconnect = true;

                p.Connected += (sender, args) => Debug.WriteLine(args.Connection.PipeName + " connected");
                p.OnMessage().Subscribe(ProcessMessage);
            }
        }

        var stageObservable = new Subject<ModelOperationStatusMessage>();
        if (this._stageMessages.TryGetValue(request.ModelKey, out var q)) { }
        else
        {
            q = new Queue<ISubject<ModelOperationStatusMessage>>();
            this._stageMessages[request.ModelKey] = q;
        }

        q.Enqueue(stageObservable);

        _activeTasks.AddOrUpdate(request);
        return stageObservable;
    }
}