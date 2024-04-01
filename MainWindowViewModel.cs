using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Security.Authentication;
using System.Xml;
using DynamicData;
using ReactiveUI.Fody.Helpers;
using Splat;

namespace RevitServerViewer;

// "ModelLocksInProgress": [
// {
//     "Age": "PT48.666204S",
//     "ModelLockOptions": 0,
//     "ModelLockType": 0,
//     "ModelPath": "Школа Благовещенск\\Стадия Р\\216_ОВ1\\DH5302_OV1_SCHOOL1200_R21_RD.rvt",
//     "TimeStamp": "/Date(1711352626000)/",
//     "UserName": "LPodstrigach"
// }

public class RevitModel
{
// "Path": "Школа Благовещенск\\Стадия П\\212_АР\\DH5302_AR_SCHOOL1200_R21_PD.rvt",
// "DateCreated": "/Date(1699969531675)/",
// "DateModified": "/Date(1708434235000)/",
// "LastModifiedBy": "LVA",
// "ModelGUID": "8e1cf8ff-df1c-4ce9-8be8-94ec82f6031b",
// "ModelSize": 759011233,
// "SupportSize": 137659651
}

public class RevitFileInfo : RevitFileSystemInfo
{
    public bool IsText { get; set; }
}

public class MainWindowViewModel : ReactiveObject
{
    private RevitServerService _rsSvc;
    public string Version { get; set; } = "2021";
    public string ConfigPath(string version) => @"C:\ProgramData\Autodesk\Revit Server " + version + @"\Config\RSN.ini";
    public ReactiveCommand<Unit, Unit> SaveModelsCommand { get; set; }
    public ObservableCollection<string> ServerList { get; set; } = new();

    [Reactive] public string SelectedServer { get; set; }
    public ObservableCollection<OperationResultViewModel> Downloads { get; set; } = new();
    public ReadOnlyObservableCollection<OperationResultViewModel> _downloads;
    private CancellationTokenSource? _cts;

    public MainWindowViewModel()
    {
        _rsSvc = Locator.Current.GetService<RevitServerService>()!;
        var f = File.ReadAllLines(ConfigPath(Version));
        ServerList.AddRange(f);
        ServerViewModel = new RevitServerViewModel();
        // SelectedServer = ServerList.First();
        var isIdling = new ReplaySubject<bool>(1);
        this.WhenAnyValue(x => x.Version).BindTo(this, model => model.ServerViewModel.Version);
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
            .OnItemAdded(x =>
            {
                Debug.WriteLine($"Add {x}");
                Downloads.Add(x);
            })
            .OnItemRemoved(x =>
            {
                Debug.WriteLine($"Remove {x}");
                Debug.WriteLine(Downloads.Contains(x));
                Downloads.Remove(x);
            })
            .OnItemUpdated((@new, old) =>
            {
                Debug.WriteLine(old);
                Debug.WriteLine(Downloads.Contains(old));
                Debug.WriteLine(@new);
                Debug.WriteLine(Downloads.Contains(@new));
                if (Downloads.Contains(old))
                {
                    @new.StartupTime = old.StartupTime;
                    Downloads.Replace(old, @new);
                }
            })
            .Bind(out _downloads)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                // Downloads.Clear();
                // Downloads.AddRange(_downloads.ToArray());
                isIdling.OnNext(Downloads.All(x => x.IsFinished));
            });
        this.WhenAnyValue(x => x.SelectedServer)
            .Subscribe(x =>
            {
                LoadingViewModel.StateText = $"Загружается {x}";
                DisplayModel = LoadingViewModel;
                _rsSvc.SetServer(address: x, version: Version);
            });
        // http://192.168.0.41/RevitServerAdmin2021
        // SelectedServer = "192.168.0.45";
    }

    [Reactive] public ReactiveObject DisplayModel { get; set; }

    public RevitServerViewModel ServerViewModel { get; set; }
    public LoadingViewModel LoadingViewModel { get; set; } = new LoadingViewModel("Выбрать сервер");

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