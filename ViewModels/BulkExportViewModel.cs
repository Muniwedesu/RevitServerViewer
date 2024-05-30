using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Services;
using RevitServerViewer.Views;
using Splat;

namespace RevitServerViewer.ViewModels;

public class BulkExportViewModel : ReactiveObject
{
    [Reactive] public bool IsStandalone { get; set; } = true;
    [Reactive] public ReactiveObject? DisplayedViewModel { get; set; }
    [Reactive] public string SelectedVersion { get; set; }
    [Reactive] public int MaxAppCount { get; set; } = 4;
    [Reactive] public string? SelectedServer { get; set; }
    public ProcessesViewModel ProcessesViewModel { get; set; }

    private readonly RevitServerService _rsSvc;

    public string RsnPath(string version) => @"C:\ProgramData\Autodesk\Revit Server " + version + @"\Config\RSN.ini";
    public ReactiveCommand<Unit, Unit> SaveModelsCommand { get; set; }
    public SaveOptionsViewModel SaveOptions { get; } = new();

    public RevitServerViewModel ServerViewModel { get; set; }
    public LoadingViewModel LoadingViewModel { get; set; } = new("Выбрать сервер");

    public ObservableCollection<string> ServerList { get; set; } = new();
    public string[] ServerVersions { get; } = { "2020", "2021", "2022", "2023" };

    public static readonly string DefaultSavePath
        = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\";

    public BulkExportViewModel()
    {
        SavePath = File.Exists(".\\path") ? File.ReadAllText(".\\path") : SavePath = DefaultSavePath;
        this.WhenAnyValue(x => x.SavePath)
            .Subscribe(x => { File.WriteAllText(".\\path", x); });

        ServerViewModel = new RevitServerViewModel();
        _rsSvc = Locator.Current.GetService<RevitServerService>()!;
        var ipcSvc = Locator.Current.GetService<IpcService>()!;
        ProcessesViewModel = Locator.Current.GetService<ProcessesViewModel>()!;

        this.WhenAnyValue(x => x.MaxAppCount)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ipcSvc, x => x.MaxAppCount);

        this.WhenAnyValue(x => x.SelectedVersion)
            .Skip(1)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ServerViewModel, x => x.Version);

        this.WhenAnyValue(x => x.SelectedVersion)
            .WhereNotNull()
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ipcSvc, x => x.RevitVersionString);

        this.WhenAnyValue(x => x.SelectedVersion)
            .Skip(1)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe((v) =>
            {
                Debug.WriteLine(v.GetHashCode());
                var obs = Observable.FromAsync((ct) => RereadRSN(v, ct))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(servers =>
                    {
                        ServerList.Clear();
                        ServerList.AddRange(servers);
#if !DEBUG
                        var s = ServerList.FirstOrDefault();
#else
                        var s = ServerList.FirstOrDefault(x => x == "192.168.0.45");
                        if (s is null) s = ServerList.FirstOrDefault();
#endif
                        SelectedServer = s;
                    });
            });

        var isIdling = new ReplaySubject<bool>(1);
        this.WhenAnyValue(x => x.SelectedVersion)
            .Where(x => !string.IsNullOrEmpty(x))
            .BindTo(this, model => model.ServerViewModel.Version);
        this.WhenAnyValue(x => x.SelectedServer)
            .Where(x => !string.IsNullOrEmpty(x))
            .BindTo(this, model => model.ServerViewModel.SelectedServer);

        isIdling.OnNext(true);
        this.ServerViewModel.Loading
            .Subscribe(x =>
            {
                if (!x) DisplayedViewModel = ServerViewModel;
                else
                {
                    LoadingViewModel.StateText = $"Загружается {SelectedServer}";
                    DisplayedViewModel = LoadingViewModel;
                }
            });

        this.WhenAnyValue(x => x.SelectedServer)
            .WhereNotNull()
            .Subscribe(OnServerChanged);
        SetPathCommand = ReactiveCommand.Create(() =>
        {
            SavePathInteraction.Handle(SavePath)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(p => SavePath = p);
        });
        SaveModelsCommand = ReactiveCommand.Create(SaveModels, isIdling);
        SelectedVersion = ServerVersions.FirstOrDefault(v => v == "2021")!;
    }

    // ReSharper disable once InconsistentNaming
    private async Task<IEnumerable<string>> RereadRSN(string ver, CancellationToken ct)
    {
        if (!File.Exists(RsnPath(ver))) return Array.Empty<string>();
        return await File.ReadAllLinesAsync(RsnPath(ver), ct);
    }

    private void OnServerChanged(string x)
    {
        _rsSvc.SetServer(address: x, version: SelectedVersion);
    }


    private void SaveModels()
    {
        _rsSvc.ClearDownloads();
        var flat = (ServerViewModel.Folders.First() as FolderViewModel)!.Flatten();
        var models = flat.OfType<ModelViewModel>()
            .Where(m => m.IsSelected)
            .ToArray();

        var outputFolder = SavePath + '\\' + SelectedServer + "\\";

        var opts = SaveOptions.GetTasks();

        ProcessesViewModel.SaveModels(models, outputFolder, opts);
    }

    public Interaction<string, string> SavePathInteraction = new();

    [Reactive] public string SavePath { get; set; }

    public ReactiveCommand<Unit, Unit> SetPathCommand { get; }
}