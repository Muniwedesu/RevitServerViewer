using System.Collections.ObjectModel;
using RevitServerViewer.Services;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ProcessesViewModel : ReactiveObject
{
    private readonly SaveSettingsViewModel _settings;
    public ObservableCollection<ModelProcessViewModel> Downloads { get; set; } = new();

    public ProcessesViewModel()
    {
        _settings = Locator.Current.GetService<SaveSettingsViewModel>()!;
        Locator.Current.GetService<RevitServerService>();
    }

    public void SaveModels(IEnumerable<ModelViewModel> models, string outputPath, ICollection<TaskType> tasks)
    {
        _settings.NavisworksSettings
            .Subscribe(s =>
            {
                foreach (var model in models)
                {
                    var op = new ModelProcessViewModel(model, outputPath, tasks, s);
                    if (Downloads.FirstOrDefault(d => d.Name == op.Name) is { } mp)
                    {
                        // mp.Cancel();
                        Downloads.Remove(mp);
                    }

                    Downloads.Add(op);
                }
                //TODO: remove completed tasks


                // _rsSvc.AddDownloads(modelPaths, ServerViewModel.SelectedServer, true);
            });
    }
}