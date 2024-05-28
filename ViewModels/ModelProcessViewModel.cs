using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public enum ProcessType
{
    Download
    , Detach
    , Export
    , Cleanup
    , DiscardLinks
    , SaveModel
}

public class ModelProcessViewModel : ReactiveObject
{
    public DateTime StartupTime { get; set; } = DateTime.Now;
    public string DisplayStartupTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Name { get; set; }
    [Reactive] public ModelTaskViewModel? CurrentTask { get; set; } = null;
    public Queue<ProcessType> RemainingStages { get; set; } = new();
    public ObservableCollectionExtended<ModelTaskViewModel> FinishedTasks { get; set; } = new();
    public const string ElapsedFormat = @"hh\:mm\:ss";
    [Reactive] public TimeSpan Elapsed { get; set; }
    public string OutputFolder { get; }


    public ModelProcessViewModel(string sourcePath, string outputFolder, ICollection<ProcessType> opts)
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
                t?.WhenAnyValue(x => x.IsDone)
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
                if (!RemainingStages.Any() && CurrentTask.IsDone)
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
                ProcessType.Download => new ModelDownloadTaskViewModel(previousSource, OutputFolder)
                , ProcessType.Detach => new ModelDetachTaskViewModel(previousSource, previous!.OutputFile)
                , ProcessType.DiscardLinks => new ModelDiscardTaskViewModel(previous!.ModelKey, previous.OutputFile)
                , ProcessType.Cleanup => new ModelCleanupTaskViewModel(previous!.ModelKey, previous.OutputFile
                    , OutputFolder)
                , ProcessType.Export => new ModelExportTaskViewModel(previous!.ModelKey, previous.OutputFile
                    , OutputFolder)
            };
            return CurrentTask;
        }

        return ShouldSaveModel(previous)
            ? new ModelSaveTaskViewModel(previous!.ModelKey, previous!.SourceFile)
            : previous;
    }

    private bool ShouldSaveModel(ModelTaskViewModel? previous)
    {
        return !FinishedTasks.Any(t => t is ModelSaveTaskViewModel)
               && !FinishedTasks.All(t => t is ModelDownloadTaskViewModel)
               && previous?.Stage != OperationStage.Error;
    }

    private void SetFinished(ModelTaskViewModel t)
    {
        if (!FinishedTasks.Contains(t)) FinishedTasks.Add(t);
    }
}