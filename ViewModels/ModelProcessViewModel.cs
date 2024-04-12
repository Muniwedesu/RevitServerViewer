using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public class ModelProcessViewModel : ReactiveObject
{
    public DateTime StartupTime { get; set; } = DateTime.Now;
    public string DisplayStartupTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Name { get; set; }
    [Reactive] public ModelTaskViewModel? CurrentTask { get; set; } = null;
    public Queue<ProcessStage> RemainingStages { get; set; } = new();
    public ObservableCollectionExtended<ModelTaskViewModel> FinishedTasks { get; set; } = new();
    public const string ElapsedFormat = @"hh\:mm\:ss";
    [Reactive] public TimeSpan Elapsed { get; set; }
    public string OutputFolder { get; }

    public enum ProcessStage
    {
        Download
        , Detach
        , Export
        , Cleanup
        , Discard
    }

    public ModelProcessViewModel(string sourcePath, string outputFolder, ICollection<ProcessStage> opts)
    {
        Name = sourcePath;
        OutputFolder = outputFolder;
        foreach (var o in opts)
        {
            RemainingStages.Enqueue(o);
        }

        this.WhenAnyValue(x => x.CurrentTask)
            .Subscribe(t =>
            {
                t ??= SetNextTask(sourcePath, t);
                t?.WhenAnyValue(x => x.IsFinished)
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => SetFinished(t));
                t?.WhenAnyValue(x => x.Elapsed).Subscribe(UpdateTotalTime(t));
                t?.Execute();
            });
        // this.WhenAnyValue(x => x.Elapsed).Subscribe(x => Debug.WriteLine(x.ToString(ElapsedFormat)));
        FinishedTasks.ToObservableChangeSet()
            .OnItemAdded(x =>
            {
                if (CurrentTask == x) CurrentTask = SetNextTask(x.SourceFile, x);
            })
            .Subscribe(_ =>
            {
                if (!RemainingStages.Any() && CurrentTask.IsFinished)
                {
                    Debug.WriteLine(this.Name
                                    + " finished "
                                    + FinishedTasks.Select(x => x.Elapsed).Aggregate((a, b) => a + b)
                                        .ToString(ElapsedFormat));
                }
            });
    }

    private Action<TimeSpan> UpdateTotalTime(ModelTaskViewModel t)
    {
        return x => Elapsed = FinishedTasks.Where(t2 => t != t2).Select(y => y.Elapsed)
            .Aggregate(TimeSpan.Zero, (a, b) => a + b) + x;
    }

    private ModelTaskViewModel? SetNextTask(string previousSource, ModelTaskViewModel? previous)
    {
        if (RemainingStages.TryDequeue(out var stage))
        {
            CurrentTask = stage switch
            {
                ProcessStage.Download => new ModelDownloadTaskViewModel(previousSource, OutputFolder)
                , ProcessStage.Detach => new ModelDetachTaskViewModel(previousSource, previous!.OutputFile)
                , ProcessStage.Export => new ModelExportTaskViewModel(previous!.ModelKey, previous!.OutputFile
                    , OutputFolder)
            };
            return CurrentTask;
        }

        return previous;
    }

    private void SetFinished(ModelTaskViewModel t)
    {
        if (!FinishedTasks.Contains(t)) FinishedTasks.Add(t);
    }
}