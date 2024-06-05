using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Threading;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Models;
using Splat;

namespace RevitServerViewer.ViewModels;

public abstract class ModelTaskViewModel : ReactiveObject
{
    private Serilog.ILogger? _log;

    /// <summary>
    /// path to the model on RS
    /// </summary>
    public string ModelKey { get; set; }

    /// <summary>
    /// source file path used for this task
    /// </summary>
    public string SourceFile { get; set; }

    /// <summary>
    /// output file path for this task, same as source by default
    /// </summary>
    public string OutputFolder { get; set; }

    /// <summary>
    /// Stage displayed on the view
    /// </summary>
    public abstract string OperationTypeString { get; }

    /// <summary>
    /// Shows if task is executing
    /// </summary>
    [Reactive] public bool IsExecuting { get; set; }

    /// <summary>
    /// If task has stopped executing (state is completed or error)
    /// </summary>
    [Reactive] public bool IsDone { get; set; }

    /// <summary>
    /// By default is set to source file
    /// </summary>
    public string OutputFile { get; set; }

    public OperationType OperationType { get; protected set; } = OperationType.Download;
    [Reactive] public string? StageDescription { get; set; }
    [Reactive] public OperationStage Stage { get; set; }
    [Reactive] public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    public ObservableTimer TaskTimer { get; set; } = new(TimeSpan.FromMilliseconds(1000));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="key">Path on the RS </param>
    /// <param name="sourceFile">Path to the source .rvt file</param>
    /// <param name="outputFolder">Base output folder location</param>
    public ModelTaskViewModel(string key, string sourceFile, string outputFolder)
    {
        _log = Locator.Current.GetService<Serilog.ILogger>();
        this.WhenAnyValue(x => x.IsExecuting)
            .Subscribe(x =>
            {
                if (x) TaskTimer.Subscribe(y => Elapsed = y, RxApp.MainThreadScheduler);
                else TaskTimer.Unsubscribe();
            });

        this.WhenAnyValue(x => x.IsDone).Where(x => x)
            .Subscribe(_ => TaskTimer.Unsubscribe());

        ModelKey = key;
        SourceFile = sourceFile;
        OutputFile = sourceFile;
        OutputFolder = outputFolder;
        this.WhenAnyValue(x => x.Stage)
            .Skip(1)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(OnStageChanged);
    }

    private void OnStageChanged(OperationStage st)

    {
        IsDone = st is OperationStage.Error or OperationStage.Completed;
        IsExecuting = st == OperationStage.Started;
        _log?.Information(this.ModelKey
                          + " " + Enum.GetName(typeof(OperationType), OperationType)
                          + " " + Enum.GetName(typeof(OperationStage), st)
                          + " " + Elapsed.ToString(TimeSpanConverter.Format));
        //TODO: timer stuff
    }

    /// <summary>
    /// Show exception message and set state to Error
    /// </summary>
    /// <param name="exception"></param>
    protected void HandleException(Exception exception)
    {
        Stage = OperationStage.Error;
        StageDescription = exception.Message;
    }

    public bool Execute() => ExecuteCommand();

    public abstract bool ExecuteCommand();

    /// <summary>
    /// Updates stage and sets its description from message
    /// </summary>
    /// <param name="msg"></param>
    public void UpdateStage(ModelOperationStatusMessage msg)
    {
        _log?.Information(ModelKey
                          + " " + msg.OperationStage
                          + " " + msg.OperationMessage
                          + " " + StageDescription);
        this.Stage = msg.OperationStage;
        this.StageDescription = msg.OperationMessage;
    }

    public void Reset()
    {
        IsExecuting = default;
        IsDone = default;
        this.Stage = default;
        TaskTimer.Reset();
    }
}