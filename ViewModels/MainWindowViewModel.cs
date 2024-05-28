using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    [Reactive] public bool IsStandalone { get; set; } = true;
    [Reactive] public ReactiveObject DisplayModel { get; set; }
    [Reactive] public string SelectedVersion { get; set; } = "2021";
    [Reactive] public int MaxAppCount { get; set; } = 4;
    [Reactive] public string SelectedServer { get; set; }

    private readonly RevitServerService _rsSvc;
    public string ConfigPath(string version) => @"C:\ProgramData\Autodesk\Revit Server " + version + @"\Config\RSN.ini";
    public ReactiveCommand<Unit, Unit> SaveModelsCommand { get; set; }
    public ObservableCollection<string> ServerList { get; set; } = new();
    public SaveOptionsViewModel SaveOptions { get; } = new();
    public ObservableCollection<ModelProcessViewModel> Downloads { get; set; } = new();


    public RevitServerViewModel ServerViewModel { get; set; }
    public LoadingViewModel LoadingViewModel { get; set; } = new("Выбрать сервер");

    public string[] ServerVersions { get; } = { "2020", "2021", "2022", "2023" };

    public MainWindowViewModel()
    {
        ServerViewModel = new RevitServerViewModel();
        _rsSvc = Locator.Current.GetService<RevitServerService>()!;
        var ipcSvc = Locator.Current.GetService<IpcService>()!;
        // _ipcSvc.WhenAnyValue(x => x.Connected)
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(x => ConnectionString = x ? "Connected" : "Not connected");
        // _ipcSvc.WhenAnyValue(x => x.RevitVersionString)
        //     .ObserveOn(RxApp.MainThreadScheduler)
        //     .WhereNotNull()
        //     .Subscribe(OnVersionReceived);
        this.WhenAnyValue(x => x.SelectedVersion)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ServerViewModel, x => x.Version);
        this.WhenAnyValue(x => x.MaxAppCount)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ipcSvc, x => x.MaxAppCount);
        this.WhenAnyValue(x => x.SelectedVersion)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(ipcSvc, x => x.RevitVersionString);

        this.WhenAnyValue(x => x.SelectedVersion)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(RereadRSN);

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

        this.WhenAnyValue(x => x.SelectedServer).WhereNotNull().Subscribe(OnServerChanged);
#if DEBUG
        SelectedServer = "192.168.0.45";
#endif
    }

    private void OnVersionReceived(string x)
    {
        this.SelectedVersion = ServerVersions.First(v => v == x);
        this.IsStandalone = false;
    }

    // ReSharper disable once InconsistentNaming
    private void RereadRSN(string ver)
    {
        ServerList.Clear();
        if (!File.Exists(ConfigPath(ver))) return;
        var f = File.ReadAllLines(ConfigPath(ver));
        ServerList.AddRange(f);
    }

    private void OnServerChanged(string x)
    {
        LoadingViewModel.StateText = $"Загружается {x}";
        DisplayModel = LoadingViewModel;
        _rsSvc.SetServer(address: x, version: SelectedVersion);
    }


    private void SaveModels()
    {
        _rsSvc.ClearDownloads();
        var flat = (ServerViewModel.Folders.First() as FolderViewModel)!.Flatten();
        var models = flat.OfType<ModelViewModel>()
            .Where(m => m.IsSelected)
            .Cast<ModelViewModel>()
            .ToArray();
        //TODO: ability to change destination folder in UI
        var outputFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\{SelectedServer}\";

        var opts = SaveOptions.GetTasks();

        foreach (var model in models)
        {
            var op = new ModelProcessViewModel(model, outputFolder, opts);
            if (Downloads.FirstOrDefault(d => d.Name == op.Name) is { } mp)
            {
                // mp.Cancel();
                Downloads.Remove(mp);
            }

            Downloads.Add(op);
        }
        //TODO: remove completed tasks


        // _rsSvc.AddDownloads(modelPaths, ServerViewModel.SelectedServer, true);
    }
}