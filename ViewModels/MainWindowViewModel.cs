using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace RevitServerViewer.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private RevitServerService _rsSvc;
    public string ConfigPath(string version) => @"C:\ProgramData\Autodesk\Revit Server " + version + @"\Config\RSN.ini";
    public ReactiveCommand<Unit, Unit> SaveModelsCommand { get; set; }
    public ObservableCollection<string> ServerList { get; set; } = new();

    [Reactive] public string SelectedServer { get; set; }
    public ObservableCollection<OperationResultViewModel> Downloads { get; set; } = new();
    private ReadOnlyObservableCollection<OperationResultViewModel> _downloads;
    private readonly IpcService _ipcSvc;

    [Reactive] public ReactiveObject DisplayModel { get; set; }

    public RevitServerViewModel ServerViewModel { get; set; }
    public LoadingViewModel LoadingViewModel { get; set; } = new LoadingViewModel("Выбрать сервер");
    [Reactive] public string ConnectionString { get; set; }

    [Reactive] public string SelectedVersion { get; set; } = "2021";

    public string[] ServerVersions { get; } = { "2020", "2021", "2022", "2023" };

    public MainWindowViewModel()
    {
        ServerViewModel = new RevitServerViewModel();
        _rsSvc = Locator.Current.GetService<RevitServerService>()!;
        _ipcSvc = Locator.Current.GetService<IpcService>()!;
        _ipcSvc.WhenAnyValue(x => x.Connected)
            .Subscribe(x => ConnectionString = x ? "Connected" : "Not connected");
        _ipcSvc.WhenAnyValue(x => x.RevitVersion)
            .WhereNotNull()
            .Subscribe(x =>
            {
                this.SelectedVersion = ServerVersions.First(v => v == x);
                this.IsStandalone = false;
            });
        //TODO: revamp or smth
        this.WhenAnyValue(x => x.SelectedVersion)
            .BindTo(this.ServerViewModel, x => x.Version);

        this.WhenAnyValue(x => x.SelectedVersion)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(ver =>
            {
                ServerList.Clear();
                if (!File.Exists(ConfigPath(ver))) return;
                var f = File.ReadAllLines(ConfigPath(ver));
                ServerList.AddRange(f);
            });
        // SelectedServer = ServerList.First();
        var isIdling = new ReplaySubject<bool>(1);
        this.WhenAnyValue(x => x.SelectedVersion).BindTo(this, model => model.ServerViewModel.Version);
        this.WhenAnyValue(x => x.SelectedServer).BindTo(this, model => model.ServerViewModel.SelectedServer);
        isIdling.OnNext(true);
        this.ServerViewModel.WhenAnyValue(x => x.Loading)
            .Subscribe(x =>
            {
                if (!x) DisplayModel = ServerViewModel;
            });
        SaveModelsCommand = ReactiveCommand.Create(SaveModels, isIdling);
        _rsSvc.Connect()
            .SortBy(x => x.SourcePathFile)
            .Transform(x => new OperationResultViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .OnItemAdded(x =>
            {
                // Debug.WriteLine($"Add {x}");
                Downloads.Add(x);
            })
            .OnItemRemoved(x =>
            {
                // Debug.WriteLine($"Remove {x}");
                // Debug.WriteLine(Downloads.Contains(x));
                Downloads.Remove(x);
            })
            .OnItemUpdated((@new, old) =>
            {
                // Debug.WriteLine(old);
                // Debug.WriteLine(Downloads.Contains(old));
                // Debug.WriteLine(@new);
                // Debug.WriteLine(Downloads.Contains(@new));
                if (Downloads.Contains(old))
                {
                    @new.StartupTime = old.StartupTime;
                    Downloads.Replace(old, @new);
                }
            })
            .Bind(out _downloads)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => isIdling.OnNext(Downloads.All(x => x.TimerActive)));
        this.WhenAnyValue(x => x.SelectedServer)
            .Subscribe(x =>
            {
                LoadingViewModel.StateText = $"Загружается {x}";
                DisplayModel = LoadingViewModel;
                _rsSvc.SetServer(address: x, version: SelectedVersion);
            });
    }

    [Reactive] public bool IsStandalone { get; set; } = true;
    
    private void SaveModels()
    {
        _rsSvc.ClearDownloads();
        var flat = (ServerViewModel.Folders.First() as FolderViewModel)!.Flatten();
        var modelPaths = flat.OfType<ModelViewModel>()
            .Where(m => m.IsSelected)
            .Select(x => x.FullName)
            .ToArray();
        
        _rsSvc.AddDownloads(modelPaths, ServerViewModel.SelectedServer, true);
    }
}