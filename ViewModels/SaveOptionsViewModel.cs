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

    [Reactive] public bool CleanEnabled { get; set; }

    public SaveOptionsViewModel()
    {
        this.WhenAnyValue(x => x.IsExporting, x => x.IsDiscarding, x => x.IsCleaning)
            .Select(x => x.Item1 || x.Item2 || x.Item3)
            .Subscribe(x =>
            {
                if (!this.IsDetaching & x) this.IsDetaching = x;
                this.DetachEnabled = !x;
            });
        this.WhenAnyValue(x => x.IsDiscarding)
            .Subscribe(x =>
            {
                if (!this.IsCleaning & x) this.IsCleaning = x;
                this.CleanEnabled = !x;
            });
    }

    public ICollection<TaskType> GetTasks()
    {
        var stages = new List<TaskType>();
        stages.Add(TaskType.Download);
        if (IsGeneratingError) stages.Add(TaskType.DebugError);
        if (IsDetaching) stages.Add(TaskType.Detach);
        if (IsDiscarding) stages.Add(TaskType.DiscardLinks);
        if (IsCleaning) stages.Add(TaskType.Cleanup);
        if (IsExporting) stages.Add(TaskType.Export);
        // if (IsDiscarding | IsCleaning | IsExporting) stages.Add(TaskType.SaveModel);
        return stages;
    }

    [Reactive] public bool IsGeneratingError { get; set; }
}