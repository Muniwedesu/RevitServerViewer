using System.Diagnostics;
using System.Reactive.Linq;
using DynamicData;
using DynamicData.Binding;
using IBS.RevitServerTool;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public class ModelProcessViewModel : ReactiveObject
{
    [Reactive] public DateTime StartupTime { get; set; } = DateTime.Now;
    public string DisplayStartupTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Name { get; set; }
    [Reactive] public ModelTaskViewModel? CurrentTask { get; set; } = null;
    public Queue<ProcessStages> RemainingStages { get; set; } = new();
    public ObservableCollectionExtended<ModelTaskViewModel> FinishedTasks { get; set; } = new();

    public enum ProcessStages
    {
        Download
        , Detach
        , Export
    }

    public ModelProcessViewModel(string sourcePath, string outputFolder)
    {
        Name = sourcePath;
        OutputFolder = outputFolder;
        RemainingStages.Enqueue(ProcessStages.Download);
        RemainingStages.Enqueue(ProcessStages.Detach);
        RemainingStages.Enqueue(ProcessStages.Export);

        this.WhenAnyValue(x => x.CurrentTask)
            .Subscribe(t =>
            {
                t ??= SetNextTask(sourcePath, t);
                t?.WhenAnyValue(x => x.IsFinished)
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => SetFinished(t));
                t?.Execute();
            });
        FinishedTasks.ToObservableChangeSet()
            .OnItemAdded(x =>
            {
                if (CurrentTask == x) CurrentTask = SetNextTask(x.SourceFile, x);
            })
            .Subscribe(_ =>
            {
                if (!RemainingStages.Any() && CurrentTask is null)
                {
                    Debug.WriteLine(this.Name +
                                    " finished " +
                                    FinishedTasks.Select(x => x.Elapsed).Aggregate((a, b) => a + b).ToString("g"));
                }
            });
    }

    private ModelTaskViewModel? SetNextTask(string sourcePath, ModelTaskViewModel? previous)
    {
        if (RemainingStages.TryDequeue(out var stage))
        {
            CurrentTask = stage switch
            {
                ProcessStages.Download => new ModelDownloadTaskViewModel(sourcePath, OutputFolder)
                , ProcessStages.Detach => new ModelDetachTaskViewModel(sourcePath
                    , previous!.OutputFile)
                , ProcessStages.Export => new ModelDetachTaskViewModel(sourcePath
                    , previous!.OutputFile)
            };
            return CurrentTask;
        }
        else return null!;
    }

    public string OutputFolder { get; }

    private void SetFinished(ModelTaskViewModel t)
    {
        if (!FinishedTasks.Contains(t)) FinishedTasks.Add(t);
    }
}

public class OperationResultViewModel : ReactiveObject
{
    private readonly Stopwatch _timer = new Stopwatch();

    public OperationResultViewModel(ProcessingState processingState)
    {
        Name = processingState.SourcePathFile;
        Message = processingState.StateMessage;
        Stage = processingState.Stage;
        StartupTime = DateTime.Now;
        _timer.Start();
        this.WhenAnyValue(x => x.StartupTime)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(t =>
            {
                // var dT = DateTime.Now - StartupTime;
                // Elapsed = $"{dT.Hours:00}:{dT.Minutes:00}:{dT.Seconds:00}";
                DisplayStartupTime = t.ToString("HH:mm:ss");
            });

        this.WhenAnyValue(x => x.Stage)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(st =>
            {
                var timer = Observable.Interval(TimeSpan.FromSeconds(1))
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ =>
                    {
                        var dt = DateTime.Now - StartupTime;
                        Elapsed = $"{dt.Hours:00}:{dt.Minutes:00}:{dt.Seconds:00}";
                    });
                this.WhenAnyValue(x => x.TimerActive).Where(x => x).Subscribe(_ => timer.Dispose());
                State = Enum.GetName(typeof(ProcessingStage), st)!;
                //TODO: use something else there
                TimerActive = st
                    switch
                    {
                        ProcessingStage.Completed
                            or ProcessingStage.Exported
                            or ProcessingStage.Detached
                            or ProcessingStage.DownloadError
                            or ProcessingStage.DownloadComplete
                            or ProcessingStage.DetachError
                            or ProcessingStage.SaveError
                            or ProcessingStage.OpenError
                            or ProcessingStage.ExportError
                            or ProcessingStage.Idle => true
                        , _ => false
                    };
            });
    }

    [Reactive] public ProcessingStage Stage { get; set; }

    [Reactive] public bool TimerActive { get; set; } = false;
    [Reactive] public string Elapsed { get; set; } = string.Empty;

    [Reactive] public DateTime StartupTime { get; set; }
    public string DisplayStartupTime { get; set; }

    [Reactive] public string State { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }

    public override string ToString() => $"{Name} {DisplayStartupTime} {State}";
}