using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public class LoadingViewModel : ReactiveObject
{
    public LoadingViewModel(string? selectedServer)
    {
        StateText += " " + selectedServer;
    }

    [Reactive] public string StateText { get; set; } = "Загрузка";
}