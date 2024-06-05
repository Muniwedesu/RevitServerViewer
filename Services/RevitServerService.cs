using System.Reactive.Concurrency;
using System.Reactive.Linq;
using DynamicData;
using IBS.IPC.DataTypes;
using IBS.RevitServerTool;

namespace RevitServerViewer.Services;

public class RevitServerService
{
    private RevitServerDownloader _dl;

    public SourceCache<ProcessingState, string> Operations { get; set; } = new(x => x.SourcePathFile);

    private IObservable<ModelOperationStatusMessage> _downloads = Observable.Empty<ModelOperationStatusMessage>();

    public void SetServer(string address, string version)
    {
        _dl = new RevitServerDownloader(address, version);
        Address = address;
        ServerVersion = version;
    }

    public string Address { get; set; }

    public string ServerVersion { get; set; }

    public IObservable<IChangeSet<ProcessingState, string>> Connect() => Operations.Connect();

    public void ClearDownloads() => this.Operations.Clear();

    public IObservable<ModelDownloadStatusMessage> AddDownload(string srcFile, string outFile, string outFolder)
    {
        //TODO: this was intended to be used as a singleton but it seems to work fine anyway
        
        var st = new RevitServerDownloader(Address, ServerVersion).Download(srcFile, outFile, outFolder)
            .ObserveOn(TaskPoolScheduler.Default)
            .SubscribeOn(TaskPoolScheduler.Default);
        _downloads = _downloads.Merge(st);
        return st;
    }
}