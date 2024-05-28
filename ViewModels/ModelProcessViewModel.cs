using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public enum TaskType
{
    Download
    , Detach
    , Export
    , Cleanup
    , DiscardLinks
    , SaveModel
    , DebugError
}

public class ModelProcessViewModel : ReactiveObject
{
    public DateTime StartupTime { get; set; } = DateTime.Now;
    public string DisplayStartupTime { get; set; } = DateTime.Now.ToString("HH:mm:ss");
    public string Name { get; set; }

    [Reactive] public ModelTaskViewModel? CurrentTask { get; set; } = null;

    // public Queue<TaskType> RemainingTaskTypes { get; set; } = new();
    public ObservableCollectionExtended<ModelTaskViewModel> FinishedTasks { get; set; } = new();
    public const string ElapsedFormat = @"hh\:mm\:ss";
    [Reactive] public TimeSpan Elapsed { get; set; }
    public string OutputFolder { get; }
    public ReactiveCommand<Unit, Unit> RetryCommand { get; set; }
    public Queue<ModelTaskViewModel> TaskQueue { get; } = new();

    public ModelProcessViewModel(ModelViewModel sourceModel, string outputFolder, ICollection<TaskType> opts)
    {
        Name = sourceModel.FullName;
        OutputFolder = outputFolder;
        // foreach (var o in opts) RemainingTaskTypes.Enqueue(o);
        ModelTaskViewModel last = null!;
        foreach (var t in opts)
        {
            last = t switch
            {
                TaskType.Download => new ModelDownloadTaskViewModel(Name, OutputFolder, sourceModel.ModifiedDate)
                , TaskType.Detach => new ModelDetachTaskViewModel(Name, last!.OutputFile)
                , TaskType.DiscardLinks => new ModelDiscardTaskViewModel(Name, last!.OutputFile)
                , TaskType.Cleanup => new ModelCleanupTaskViewModel(Name, last!.OutputFile
                    , OutputFolder)
                , TaskType.Export => new ModelExportTaskViewModel(Name, last!.OutputFile
                    , OutputFolder)
                , _ => new ModelErrorTaskViewModel(Name, last!.OutputFile)
            };
            TaskQueue.Enqueue(last);
        }

        this.RetryCommand = ReactiveCommand.Create(RetryFromLast, CanRetry);

        this.WhenAnyValue(x => x.CurrentTask)
            .WhereNotNull()
            .Subscribe(t =>
            {
                CanRetry.OnNext(false);
                //Handles first task
                // t ??= SetNextTask(t);
                t?.WhenAnyValue(x => x.IsDone)
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => MoveToFinished(t));
                t?.WhenAnyValue(x => x.Elapsed).Subscribe(UpdateTotalTime(t));
                t?.Execute();
            });
        FinishedTasks.ToObservableChangeSet()
            .OnItemAdded(x =>
            {
                if (CurrentTask == x) CurrentTask = SetNextTask(x) ?? x;
            })
            .Subscribe(_ =>
            {
                if (!TaskQueue.Any() && CurrentTask is { IsDone: true })
                {
                    Debug.WriteLine(this.Name
                                    + " finished "
                                    + FinishedTasks.Select(x => x.Elapsed).Aggregate((a, b) => a + b)
                                        .ToString(ElapsedFormat));
                }
            });
        CurrentTask = TaskQueue.Peek();
    }

    private void RetryFromLast()
    {
        CurrentTask = TaskQueue.Peek();
    }

    private Action<TimeSpan> UpdateTotalTime(ModelTaskViewModel t)
    {
        return x => Elapsed = FinishedTasks.Where(t2 => t != t2).Select(y => y.Elapsed)
            .Aggregate(TimeSpan.Zero, (a, b) => a + b) + x;
    }

    private ModelTaskViewModel? SetNextTask(ModelTaskViewModel previous)
    {
        if (previous?.Stage is OperationStage.Error)
        {
            Debug.WriteLine("Prev errored");
            this.CanRetry.OnNext(true);
            return null;
        }

        if (TaskQueue.TryPeek(out var nextTask))
            //TODO: check if CurrentTask should be set here anyway
            return nextTask;

        return ShouldSaveModel(previous)
            ? new ModelSaveTaskViewModel(previous!.ModelKey, previous!.SourceFile)
            : previous;
    }

    public ISubject<bool> CanRetry { get; } = new ReplaySubject<bool>();

    private bool ShouldSaveModel(ModelTaskViewModel? previous)
    {
        return !FinishedTasks.Any(t => t is ModelSaveTaskViewModel)
               && !FinishedTasks.All(t => t is ModelDownloadTaskViewModel)
               && previous?.Stage != OperationStage.Error;
    }

    private void MoveToFinished(ModelTaskViewModel t)
    {
        if (t is { Stage: OperationStage.Completed } && !FinishedTasks.Contains(t))
            FinishedTasks.Add(TaskQueue.Dequeue());
        else CanRetry.OnNext(true);
    }
}

public class ModelErrorTaskViewModel : ModelTaskViewModel
{
    public ModelErrorTaskViewModel(string sourcePath, string outputFile) : base(sourcePath, outputFile, outputFile) { }

    public override string OperationTypeString { get; } = "Ошибка";

    public override bool ExecuteCommand()
    {
        this.StageDescription = "Ошибка тест";
        this.Stage = OperationStage.Error;
        this.IsDone = true;
        return false;
    }
}