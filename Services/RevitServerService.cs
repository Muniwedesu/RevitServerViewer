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
}