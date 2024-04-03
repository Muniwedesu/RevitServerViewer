using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.ViewModels;

namespace RevitServerViewer;

public class RevitServerViewModel : ReactiveObject
{
    private IDisposable _sub;
    public ObservableCollection<TreeItem> Folders { get; set; } = new();
    [Reactive] public string SelectedServer { get; set; }
    public string Version { get; set; }

    public RevitServerViewModel()
    {
        this.WhenAnyValue(x => x.SelectedServer)
            .Where(s => !string.IsNullOrEmpty(s))
            .Subscribe(server =>
            {
                _sub?.Dispose();
                this.Loading = true;
                Folders.Clear();
                var rs = new RevitServerConnection(Version, server);
                _sub = Observable.FromAsync(async (token) => await rs.GetFileStructureAsync(token))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(r =>
                        {
                            this.Loading = false;
                            Folders.Add(new FolderViewModel(r));
                        },
                        exception =>
                        {
                            this.Loading = false;
                            Debug.WriteLine(exception);
                        });
            });
    }

    [Reactive] public bool Loading { get; set; }
}