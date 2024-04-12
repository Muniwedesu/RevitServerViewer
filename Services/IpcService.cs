using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Autodesk.Revit.DB.Events;
using DynamicData;
using DynamicData.Kernel;
using H.Pipes;
using IBS.IPC;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.Services;

public class IpcService : ReactiveObject
{
    public PipeClient<SerializableMessage> _client = new(IBS.IPC.PipeNames.RevitServerModelDownloader);
    private ISubject<ModelOperationStatusMessage> _revitMessages = new Subject<ModelOperationStatusMessage>();
    public IObservable<ModelOperationStatusMessage> RevitMessages => _revitMessages;
    public SourceCache<ModelOperationRequest, string> _ops = new(x => x.ModelKey);

    public Dictionary<string, Queue<ISubject<ModelOperationStatusMessage>>> _stageMessages = new();

    [Reactive] public bool Connected { get; set; }
    [Reactive] public string RevitVersion { get; set; }


    public IpcService()
    {
#if DEBUG
        Debugger.Launch();
#endif
        _client.OnMessage()
            .Subscribe(msg =>
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
                            //TODO: this isn't very reliable
                            _ops.RemoveKey(statusMessage.ModelKey);
                        }
                        else
                        {
                            _stageMessages[statusMessage.ModelKey].Peek().OnNext(statusMessage);
                        }

                        // _revitMessages.OnNext(statusMessage);
                        // if (statusMessage.OperationStage is OperationStage.Completed or OperationStage.Error)
                        //     _ops.RemoveKey(statusMessage.ModelKey);
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
            });
        _ops.Connect()
            .OnItemUpdated((x, xx) =>
            {
                Observable.FromAsync(async () => await _client.WriteAsync(SerializableMessage.Create(x, x.GetType())))
                    .Subscribe(_ => { Debug.WriteLine("Request " + x.ModelKey); });
            })
            .OnItemAdded(x =>
            {
                // _client.WriteAsync(SerializableMessage.Create(x)).Wait();
                //TODO: display connecting status if not connected and do not 
                //TODO: ^ ??????????
                Observable.FromAsync(async () => await _client.WriteAsync(SerializableMessage.Create(x, x.GetType())))
                    .Subscribe(_ => { Debug.WriteLine("Request " + x.ModelKey); });
            })
            .OnItemRemoved(_ =>
            {
                // if (!_ops.Items.Any())
                // {
                //     _revitMessages.OnCompleted();
                // }
            })
            .Subscribe(x => { Debug.WriteLine(x); });
        _client.Connected += (sender, args) => { Debug.WriteLine(args.Connection.PipeName); };

        _client.OnDisconnected().Subscribe(_ => { Application.Current.Shutdown(); });
        _client.ConnectAsync().Wait(3000);

        Observable.Interval(TimeSpan.FromMilliseconds(1000)).Subscribe(UpdateConnectionStatus);
        if (!_client.IsConnected) ;
    }

    private void UpdateConnectionStatus(long _)
    {
        // Debug.WriteLine("Checking connection");
        if (!this.Connected)
        {
            _client.ConnectAsync().Wait(500);
            Debug.WriteLine("Trying to connect: " + _client.IsConnected);
        }

        if (this.Connected != _client.IsConnected)
        {
            this.Connected = _client.IsConnected;
        }
    }


    public IObservable<ModelOperationStatusMessage> RequestOperation(ModelOperationRequest request)
    {
        var stageObservable = new Subject<ModelOperationStatusMessage>();
        if (this._stageMessages.TryGetValue(request.ModelKey, out var q)) { }
        else
        {
            q = new Queue<ISubject<ModelOperationStatusMessage>>();
            this._stageMessages[request.ModelKey] = q;
        }

        q.Enqueue(stageObservable);

        _ops.AddOrUpdate(request);
        return stageObservable;
    }
}

// _client.WriteAsync(
//         SerializableMessage.Create(new DiscardLinksRequest("physPath", "root", "DiscardLinksKey")))
//     .Wait();
// _client.WriteAsync(SerializableMessage.Create(new CleanUpModelRequest("physPath", "root", "CleanKey"
//     , new[] { "model1", "model2" }))).Wait();
// _client.WriteAsync(
//         SerializableMessage.Create(
//             new ExportModelRequest("physPath", "root", "ExportKey", "outputLocation")))
//     .Wait();