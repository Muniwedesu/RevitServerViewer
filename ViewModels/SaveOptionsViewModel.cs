using System.Reactive.Linq;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

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

    public ICollection<ProcessType> GetTasks()
    {
        var stages = new List<ProcessType>();
        stages.Add(ProcessType.Download);
        if (IsDetaching) stages.Add(ProcessType.Detach);
        if (IsDiscarding) stages.Add(ProcessType.DiscardLinks);
        if (IsCleaning) stages.Add(ProcessType.Cleanup);
        if (IsExporting) stages.Add(ProcessType.Export);
        // if (IsDetaching) stages.Add(ProcessType.Detach);
        return stages;
    }
}