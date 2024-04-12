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
    private RevitServerService _rsSvc;
    public string ConfigPath(string version) => @"C:\ProgramData\Autodesk\Revit Server " + version + @"\Config\RSN.ini";
    public ReactiveCommand<Unit, Unit> SaveModelsCommand { get; set; }
    public ObservableCollection<string> ServerList { get; set; } = new();
    public SaveOptionsViewModel SaveOptions { get; } = new();

    [Reactive] public string SelectedServer { get; set; }
    public ObservableCollection<ModelProcessViewModel> Downloads { get; set; } = new();
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
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(x => ConnectionString = x ? "Connected" : "Not connected");
        _ipcSvc.WhenAnyValue(x => x.RevitVersion)
            .ObserveOn(RxApp.MainThreadScheduler)
            .WhereNotNull()
            .Subscribe(OnVersionReceived);
        //TODO: or smth
        this.WhenAnyValue(x => x.SelectedVersion)
            .ObserveOn(RxApp.MainThreadScheduler)
            .BindTo(this.ServerViewModel, x => x.Version);

        this.WhenAnyValue(x => x.SelectedVersion)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnVersionChanged);

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

        this.WhenAnyValue(x => x.SelectedServer).Subscribe(OnServerChanged);
    }

    private void OnVersionReceived(string x)
    {
        this.SelectedVersion = ServerVersions.First(v => v == x);
        this.IsStandalone = false;
    }

    private void OnVersionChanged(string ver)
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

    [Reactive] public bool IsStandalone { get; set; } = true;

    private void SaveModels()
    {
        _rsSvc.ClearDownloads();
        var flat = (ServerViewModel.Folders.First() as FolderViewModel)!.Flatten();
        var modelPaths = flat.OfType<ModelViewModel>()
            .Where(m => m.IsSelected)
            .Select(x => x.FullName)
            .ToArray();
        //TODO: set destination folder
        var outputFolder = $@"{Environment.GetFolderPath(Environment.SpecialFolder.Desktop)}\RS\{SelectedServer}\";

        var opts = SaveOptions.GetTasks();

        foreach (var sourcePath in modelPaths)
        {
            var op = new ModelProcessViewModel(sourcePath, outputFolder, opts);
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

public class SaveOptionsViewModel : ReactiveObject
{
    [Reactive] public bool IsDetaching { get; set; }
    [Reactive] public bool IsExporting { get; set; }
    [Reactive] public bool IsDiscarding { get; set; }
    [Reactive] public bool IsCleaning { get; set; }

    /// <summary>
    /// exists solely to disable unchecking 'detach' if other boxes are checked
    /// </summary>
    [Reactive] public bool DetachEnabled { get; set; }

    public SaveOptionsViewModel()
    {
        this.WhenAnyValue(x => x.IsExporting, x => x.IsDiscarding, x => x.IsCleaning)
            .Select(x => x.Item1 || x.Item2 || x.Item3)
            .Subscribe(x =>
            {
                this.IsDetaching = x;
                this.DetachEnabled = !x;
            });
    }

    public ICollection<ModelProcessViewModel.ProcessStage> GetTasks()
    {
        var stages = new List<ModelProcessViewModel.ProcessStage>();
        stages.Add(ModelProcessViewModel.ProcessStage.Download);
        if (IsDetaching) stages.Add(ModelProcessViewModel.ProcessStage.Detach);
        // if (IsDiscarding) stages.Add(ModelProcessViewModel.ProcessStage.Detach);
        // if (IsCleaning) stages.Add(ModelProcessViewModel.ProcessStage.Detach);
        if (IsExporting) stages.Add(ModelProcessViewModel.ProcessStage.Export);
        // if (IsDetaching) stages.Add(ModelProcessViewModel.ProcessStage.Detach);
        return stages;
    }
}