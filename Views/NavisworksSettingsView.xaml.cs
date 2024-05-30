using RevitServerViewer.ViewModels;
using Splat;

namespace RevitServerViewer.Views;

public partial class NavisworksSettingsView
{
    public NavisworksSettingsView()
    {
        InitializeComponent();
        var viewModel = Locator.Current.GetService<SaveSettingsViewModel>();
        ViewModel = viewModel;
        this.WhenActivated(dr =>
        {
            dr(this.Bind(ViewModel, vm => vm.ExportIds, v => v.ExportIdsBox.IsChecked));
            dr(this.OneWayBind(ViewModel, vm => vm.ParametersList, v => v.ParametersBox.ItemsSource));
            dr(this.Bind(ViewModel, vm => vm.SelectedParameters, v => v.ParametersBox.SelectedItem));

            dr(this.OneWayBind(ViewModel, vm => vm.CoordinatesList, v => v.CoordinatesBox.ItemsSource));
            dr(this.Bind(ViewModel, vm => vm.SelectedCoordinates, v => v.CoordinatesBox.SelectedItem));

            dr(this.Bind(ViewModel, vm => vm.FacetingFactor, v => v.FacetingFactorBox.Text));

            dr(this.Bind(ViewModel, vm => vm.ExportUrls, v => v.ExportUrlsBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ConvertLights, v => v.ConvertLightsBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ConvertRoomsToAttributes, v => v.ConvertRoomsToAttributesBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ConvertElementProperties, v => v.ConvertElementPropertiesBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ConvertLinkedCad, v => v.ConvertLinkedCadBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ExportLinks, v => v.ExportLinksBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ExportParts, v => v.ExportPartsBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.FindMissingMaterials, v => v.FindMissingMaterialsBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.DivideIntoLevels, v => v.DivideIntoLevelsBox.IsChecked));
            dr(this.Bind(ViewModel, vm => vm.ExportRoomGeometry, v => v.ExportRoomGeometryBox.IsChecked));

            // dr(this.BindCommand(ViewModel, vm => vm.BackCommand, v => v.BackCommandButton));
        });
    }
}