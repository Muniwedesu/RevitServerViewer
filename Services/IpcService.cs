using System.Reactive.Linq;
using Autodesk.Revit.DB.Events;
using DynamicData;
using DynamicData.Kernel;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.Services;

public class IpcService : ReactiveObject
{
    /// <summary>
    /// dict of active pipes and processes
    /// </summary>
    public readonly SourceCache<RevitProxy, string> Clients = new(x => x.ModelKey);

    [Reactive] public string RevitVersionString { get; set; }
    [Reactive] public int MaxAppCount { get; set; } = 4;

    public IpcService()
    {
        //TODO: free resources (close all active processes)
        Clients.Connect().AutoRefresh(x => x.Finished).Subscribe(x =>
        {
            var xx = x;

            var sub = Observable.Timer(TimeSpan.FromSeconds(5))
                .Subscribe(_ =>
                {
                    //if is idling
                    if (true) Clients.RemoveKey("");
                    //TODO: and shutdown? idk, need to make sure if it has finished its job
                });
            sub.Dispose();
        });
        // .OnItemRefreshed(x => Clients.Remove(x));
    }


    public IObservable<ModelOperationStatusMessage> RequestOperation(ModelOperationRequest request)
    {
        //check if we have space first
        //^ then start a new process
        //TODO: delete process when revit has exited
        IObservable<ModelOperationStatusMessage> obs = null!;
        Clients.Lookup(request.ModelKey)
            .IfHasValue(x => obs = x.Enqueue(request))
            .Else(() =>
            {
                var x = new RevitProxy(request.ModelKey, RevitVersionString);
                Clients.AddOrUpdate(x);
                obs = x.Enqueue(request);
            });

        return obs;
    }
}