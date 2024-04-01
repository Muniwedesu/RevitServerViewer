using System.Diagnostics;
using System.Reactive.Subjects;
using DynamicData;
using H.Pipes;
using IBS.IPC;
using IBS.IPC.DataTypes;
using IBS.RevitServerTool;

namespace RevitServerViewer;

public class IpcService
{
    public PipeClient<SerializableMessage> _client = new(IBS.IPC.PipeNames.RevitServerModelDownloader);
    private ISubject<ModelOperationStatusMessage> _revitMessages = new Subject<ModelOperationStatusMessage>();
    public IObservable<ModelOperationStatusMessage> RevitMessages => _revitMessages;

    public SourceCache<ModelOperationRequest, string> _ops = new(x => x.ModelKey);

    public IpcService()
    {
        _client.OnMessage()
            .Subscribe(msg =>
            {
                var str = System.Text.Json.JsonSerializer.Deserialize(msg.Message.SerializedString
                    , Type.GetType(msg.Message.TypeName)) as ModelOperationStatusMessage;

                _revitMessages.OnNext(str);
                if (str.OperationStatus is OperationStatus.Completed or OperationStatus.Error)
                {
                    _ops.RemoveKey(str.ModelKey);
                }
            });
        _ops.Connect()
            .OnItemAdded(x =>
            {
                // _client.WriteAsync(SerializableMessage.Create(x)).Wait();
                _client.WriteAsync(SerializableMessage.Create(x, x.GetType())).Wait();
            })
            .OnItemRemoved(_ =>
            {
                if (!_ops.Items.Any())
                {
                    _revitMessages.OnCompleted();
                }
            })
            .Subscribe();
        _client.Connected += (sender, args) => { Debug.WriteLine(args.Connection.PipeName); };

        _client.OnDisconnected().Subscribe(_ =>
        {
            //TODO: close app
        });
        // _client.ConnectAsync().Wait(3000);
        // if (!_client.IsConnected)
        // {
        //     //TODO: download-only mode
        // }
    }

    public void RequestOperation(ModelOperationRequest request)
    {
        _ops.AddOrUpdate(request);
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