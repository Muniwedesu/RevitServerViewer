using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public class RevitServerViewModel : ReactiveObject
{
    private IDisposable _sub;
    public ObservableCollection<TreeItem> Folders { get; set; } = new();
    [Reactive] public string SelectedServer { get; set; }
    public string Version { get; set; }
    private ISubject<bool> _loading = new ReplaySubject<bool>(1);
    public IObservable<bool> Loading => _loading;

    public RevitServerViewModel()
    {
        this.WhenAnyValue(x => x.SelectedServer)
            .Where(s => !string.IsNullOrEmpty(s))
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
                            Folders.Add(new FolderViewModel(r, SelectedServer));
                        },
                        exception =>
                        {
                            _loading.OnNext(false);
                            Debug.WriteLine(exception);
                        });
            });
    }

    // [Reactive] public bool Loading { get; set; }
}