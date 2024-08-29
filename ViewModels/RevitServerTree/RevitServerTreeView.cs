using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Models.ServerContent;
using Serilog;
using Splat;
using ILogger = Serilog.ILogger;

namespace RevitServerViewer.ViewModels;

public class RevitServerTreeViewModel : ReactiveObject
{
    private IDisposable? _sub;
    public ObservableCollection<TreeItem> Folders { get; set; } = new();
    [Reactive] public string SelectedServer { get; set; }
    public string Version { get; set; }
    private ISubject<bool> _loading = new ReplaySubject<bool>(1);
    private readonly ILogger? _log;
    public IObservable<bool> Loading => _loading;

    public RevitServerTreeViewModel()
    {
        _log = Locator.Current.GetService<Serilog.ILogger>();
        this.WhenAnyValue(x => x.SelectedServer)
            .Where(s => !string.IsNullOrEmpty(s))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(server =>
            {
                _sub?.Dispose();
                _loading.OnNext(true);
                Folders.Clear();
                var rs = new RevitServerConnection(Version, server);
                _sub = Observable.FromAsync(async (token) => await rs.GetFileStructureAsync(token))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(r =>
                        {
                            _loading.OnNext(false);
                            Folders.Add(new FolderLabelViewModel(r, SelectedServer));
                            _log?.Information($"Loaded structure for {SelectedServer}");
                        },
                        exception =>
                        {
                            _loading.OnNext(false);
                            _log?.Warning(exception, "Ex while loading RS folders");
                        });
            });
    }

    // [Reactive] public bool Loading { get; set; }
}