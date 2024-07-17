using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using DynamicData;
using DynamicData.Binding;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using Splat;

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
    public Serilog.ILogger _log;
    [Reactive] public ModelTaskViewModel? CurrentTask { get; set; } = null;

    // public Queue<TaskType> RemainingTaskTypes { get; set; } = new();
    public ObservableCollectionExtended<ModelTaskViewModel> FinishedTasks { get; set; } = new();
    public const string ElapsedFormat = @"hh\:mm\:ss";
    [Reactive] public TimeSpan Elapsed { get; set; }
    public string OutputFolder { get; }
    public ReactiveCommand<Unit, Unit> RetryCommand { get; set; }
    public Queue<ModelTaskViewModel> TaskQueue { get; } = new();

    public ModelProcessViewModel(ModelLabelViewModel sourceModelLabel
        , string outputFolder
        , bool preserveStructure
        , ICollection<TaskType> opts
        , NavisworksExportSettings settings)
    {
        _log = Locator.Current.GetService<Serilog.ILogger>()!;
        Name = sourceModelLabel.FullName;
        OutputFolder = outputFolder;
        // foreach (var o in opts) RemainingTaskTypes.Enqueue(o);
        ModelTaskViewModel last = null!;
        foreach (var t in opts)
        {
            last = t switch
            {
                TaskType.Download => new ModelDownloadTaskViewModel(Name, OutputFolder, sourceModelLabel.ModifiedDate
                    , preserveStructure)
                , TaskType.Detach => new ModelDetachTaskViewModel(Name, last!.OutputFile, OutputFolder)
                , TaskType.DiscardLinks => new ModelDiscardTaskViewModel(Name, last!.OutputFile, OutputFolder)
                , TaskType.Cleanup => new ModelCleanupTaskViewModel(Name, last!.OutputFile, OutputFolder)
                , TaskType.Export => new ModelExportTaskViewModel(Name, last!.OutputFile, OutputFolder, settings)
                , _ => new ModelErrorTaskViewModel(Name, last!.OutputFile)
            };
            TaskQueue.Enqueue(last);
        }

        this.RetryCommand = ReactiveCommand.Create(RetryFromLast, CanRetry);

        this.WhenAnyValue(x => x.CurrentTask)
            .WhereNotNull()
            .Subscribe(task =>
            {
                CanRetry.OnNext(false);
                //Handles first task
                // t ??= SetNextTask(t);
                task?.WhenAnyValue(x => x.IsDone)
                    .Where(x => x)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Subscribe(_ => MoveToFinished(task));
                task?.WhenAnyValue(x => x.Elapsed)
                    .Subscribe(time => UpdateTotalTime(time, task));
                task?.Execute();
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
                    _log.Information(this.Name
                                     + " finished "
                                     + FinishedTasks.Select(x => x.Elapsed)
                                         .Aggregate((a, b) => a + b)
                                         .ToString(ElapsedFormat));
                }
            });
        CurrentTask = TaskQueue.Peek();
    }

    private void RetryFromLast()
    {
        CurrentTask = null;
        var ct = TaskQueue.Peek();
        ct.Reset();
        //because it doesn't always fire in CurrentTask observer
        CanRetry.OnNext(false);
        CurrentTask = ct;
    }

    private void UpdateTotalTime(TimeSpan x, ModelTaskViewModel vm)
    {
        Elapsed = FinishedTasks.Where(vm2 => vm != vm2)
            .Select(y => y.Elapsed)
            .Aggregate(TimeSpan.Zero, (a, b) => a + b) + x;
    }

    private ModelTaskViewModel? SetNextTask(ModelTaskViewModel previous)
    {
        if (previous?.Stage is OperationStage.Error)
        {
            _log.Information($"{previous.OperationType} for {previous.ModelKey} errored");
            this.CanRetry.OnNext(true);
            return null;
        }

        if (TaskQueue.TryPeek(out var nextTask))
            //TODO: check if CurrentTask should be set here anyway
            return nextTask;
        //TODO: add this to the queue instead
        return ShouldSaveModel(previous)
            ? new ModelSaveTaskViewModel(previous!.ModelKey, previous!.SourceFile)
            : previous;
    }

    public ISubject<bool> CanRetry { get; } = new ReplaySubject<bool>();

    private bool ShouldSaveModel(ModelTaskViewModel? previous)
    {
        //TODO: skip saving on detach too
        //TODO: only save if there are no task remaining?
        return !FinishedTasks.Any(t => t is ModelSaveTaskViewModel)
               && !FinishedTasks.All(t => t is ModelDownloadTaskViewModel or ModelDetachTaskViewModel)
               && previous?.Stage != OperationStage.Error;
    }

    private void MoveToFinished(ModelTaskViewModel t)
    {
        //TODO: remove this after fixing save command
        if (!TaskQueue.Any()) return;
        if (t is { Stage: OperationStage.Completed } && !FinishedTasks.Contains(t))
            FinishedTasks.Add(TaskQueue.Dequeue());
        else CanRetry.OnNext(true);
    }
}

public class ModelErrorTaskViewModel : ModelTaskViewModel
{
    public ModelErrorTaskViewModel(string sourcePath, string outputFile) : base(sourcePath, outputFile, outputFile)
    {
        OutputFile = outputFile;
    }

    public override string OperationTypeString { get; } = "Ошибка";

    public override bool ExecuteCommand()
    {
        this.StageDescription = "Ошибка тест";
        this.Stage = OperationStage.Error;
        this.IsDone = true;
        return false;
    }
}