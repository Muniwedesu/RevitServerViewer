using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData.Binding;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public record NavisworksParametersViewModel(string Name, NavisworksExportSettings.NavisworksParameters Value)
{
    public string Name { get; set; } = Name;
    public NavisworksExportSettings.NavisworksParameters Value { get; set; } = Value;

    public static readonly NavisworksParametersViewModel Default = new("Все"
        , NavisworksExportSettings.NavisworksParameters.All);
}

public record NavisworksCoordinatesViewModel(string Name, NavisworksExportSettings.NavisworksCoordinates Value)
{
    public string Name { get; set; } = Name;
    public NavisworksExportSettings.NavisworksCoordinates Value { get; set; } = Value;

    public static readonly NavisworksCoordinatesViewModel Default = new("Общие"
        , NavisworksExportSettings.NavisworksCoordinates.Shared);
}

//TODO: check https://www.reactiveui.net/docs/handbook/data-persistence.html
public class SaveSettingsViewModel : ReactiveObject //, IRoutableViewModel
{
    [Reactive] public bool ConvertLinkedCad { get; set; }
    [Reactive] public bool ConvertLights { get; set; }
    [Reactive] public double FacetingFactor { get; set; }
    [Reactive] public bool DivideIntoLevels { get; set; }
    [Reactive] public bool ConvertElementProperties { get; set; }
    [Reactive] public bool FindMissingMaterials { get; set; }

    [Reactive] public bool ExportRoomGeometry { get; set; }

    [Reactive] public int ExportScope { get; set; }

    [Reactive] public NavisworksParametersViewModel SelectedParameters { get; set; }
        = NavisworksParametersViewModel.Default;

    [Reactive] public NavisworksCoordinatesViewModel SelectedCoordinates { get; set; }
        = NavisworksCoordinatesViewModel.Default;

    [Reactive] public bool ExportUrls { get; set; }
    [Reactive] public bool ConvertRoomsToAttributes { get; set; }
    [Reactive] public bool ExportLinks { get; set; }
    [Reactive] public bool ExportIds { get; set; }
    [Reactive] public bool ExportParts { get; set; }

    public NavisworksCoordinatesViewModel[] CoordinatesList { get; set; } =
    {
        new("Внутренние", NavisworksExportSettings.NavisworksCoordinates.Internal)
        , NavisworksCoordinatesViewModel.Default
    };

    public NavisworksParametersViewModel[] ParametersList { get; set; } =
    {
        new("Не выгружать", NavisworksExportSettings.NavisworksParameters.None)
        , new("Параметры элементов", NavisworksExportSettings.NavisworksParameters.Elements)
        , NavisworksParametersViewModel.Default
    };

    private string _navisworksSettingsPath = "./Settings/settings.json";
    private string _appSettingsPath = "./Settings/path.json";
    private ISubject<NavisworksExportSettings> _navisworksSettings = new ReplaySubject<NavisworksExportSettings>(1);

    public SaveSettingsViewModel()
    {
        Observable.FromAsync(ReadFromFile)
            .Subscribe(x =>
            {
                ConvertLinkedCad = x.ConvertLinkedCad;
                ConvertLights = x.ConvertLights;
                FacetingFactor = x.FacetingFactor;
                DivideIntoLevels = x.DivideIntoLevels;
                ConvertElementProperties = x.ConvertElementProperties;
                FindMissingMaterials = x.FindMissingMaterials;
                ExportRoomGeometry = x.ExportRoomGeometry;
                ExportScope = x.ExportScope;
                SelectedCoordinates = CoordinatesList.FirstOrDefault(y =>
                        y.Value == (NavisworksExportSettings.NavisworksCoordinates)x.Coordinates
                    , NavisworksCoordinatesViewModel.Default);
                ExportUrls = x.ExportUrls;
                ConvertRoomsToAttributes = x.ConvertRoomsToAttributes;
                ExportIds = x.ExportIds;
                ExportLinks = x.ExportLinks;
                SelectedParameters = ParametersList.FirstOrDefault(y =>
                        y.Value == (NavisworksExportSettings.NavisworksParameters)x.Coordinates
                    , NavisworksParametersViewModel.Default);
                ExportParts = x.ExportParts;
                _navisworksSettings.OnNext(x);
            });

        this.WhenAnyPropertyChanged()
            .WhereNotNull()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .Subscribe(async x =>
            {
                _navisworksSettings.OnNext(x.GetNavisworksSettings());
                await WriteToFile(x);
            });
    }

    private async Task WriteToFile(SaveSettingsViewModel vm)
    {
        try
        {
            if (!Directory.Exists(_navisworksSettingsPath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(_navisworksSettingsPath));
            }

            await File.WriteAllTextAsync(_navisworksSettingsPath
                , NetJSON.NetJSON.Serialize(vm.GetNavisworksSettings()));
        }
        catch { }
    }

    public async Task<NavisworksExportSettings> ReadFromFile()
    {
        if (File.Exists(_navisworksSettingsPath))
        {
            var txt = await File.ReadAllTextAsync(_navisworksSettingsPath);
            var s = NetJSON.NetJSON.Deserialize<NavisworksExportSettings>(txt);
            return s;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(_navisworksSettingsPath));
        await WriteToFile(this);
        return NavisworksExportSettings.Default;
    }

    public NavisworksExportSettings GetNavisworksSettings()
    {
        return new NavisworksExportSettings
        {
            ConvertLinkedCad = ConvertLinkedCad
            , ConvertLights = ConvertLights
            , FacetingFactor = FacetingFactor
            , DivideIntoLevels = DivideIntoLevels
            , ConvertElementProperties = ConvertElementProperties
            , FindMissingMaterials = FindMissingMaterials
            , ExportRoomGeometry = ExportRoomGeometry
            , ExportScope = ExportScope
            , Coordinates = (int)(SelectedCoordinates.Value)
            , ExportUrls = ExportUrls
            , ConvertRoomsToAttributes = ConvertRoomsToAttributes
            , ExportIds = ExportIds
            , ExportLinks = ExportLinks
            , Parameters = (int)(SelectedParameters.Value)
            , ExportParts = ExportParts
        };
    }

    // public string? UrlPathSegment { get; } = "Settings";
    // public IScreen HostScreen { get; }
    [Reactive] public string SavePath { get; set; }
    public IObservable<NavisworksExportSettings> NavisworksSettings => _navisworksSettings;
}