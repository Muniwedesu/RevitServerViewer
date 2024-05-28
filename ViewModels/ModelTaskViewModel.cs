using System.Diagnostics;
using System.Reactive.Linq;
using System.Windows.Threading;
using IBS.IPC.DataTypes;
using ReactiveUI.Fody.Helpers;
using RevitServerViewer.Models;

namespace RevitServerViewer.ViewModels;

public abstract class ModelTaskViewModel : ReactiveObject
{
    /// <summary>
    /// path to that model on server?
    /// </summary>
    public string ModelKey { get; set; }

    /// <summary>
    /// file path used for this task
    /// </summary>
    public string SourceFile { get; set; }

    /// <summary>
    /// output file path for this task
    /// </summary>
    public string OutputFolder { get; set; }

    public OperationType OperationType { get; protected set; } = OperationType.Download;
    public abstract string OperationTypeString { get; }
    public string OutputFile { get; set; }
    [Reactive] public OperationStage Stage { get; set; }
    [Reactive] public string StageString { get; set; }
    [Reactive] public bool IsActive { get; set; }
    [Reactive] public bool IsDone { get; set; }
    [Reactive] public string? StageDescription { get; set; }

    [Reactive] public TimeSpan Elapsed { get; set; } = TimeSpan.Zero;
    private const string? ElapsedFormat = @"hh\:mm\:ss";
    public ObservableTimer TaskTimer { get; set; } = new(TimeSpan.FromMilliseconds(1000));

    public ModelTaskViewModel(string key, string sourceFile, string outputFolder)
    {
        // TaskTimer.Subscribe.ObserveOn(RxApp.MainThreadScheduler)
        //     .Subscribe(x => );

        this.WhenAnyValue(x => x.IsActive)
            .Subscribe(x =>
            {
                if (x) TaskTimer.Subscribe(y => Elapsed = y, RxApp.MainThreadScheduler);
                else TaskTimer.Unsubscribe();
            });

        this.WhenAnyValue(x => x.IsDone).Where(x => x)
            .Subscribe(_ => TaskTimer.Unsubscribe());

        ModelKey = key;
        SourceFile = sourceFile;
        OutputFolder = outputFolder;
        this.WhenAnyValue(x => x.Stage)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(st =>
            {
                StageString = GetStageString(st);
                IsDone = st is OperationStage.Error or OperationStage.Completed;
                IsActive = st == OperationStage.Started;
                Debug.WriteLine(this.ModelKey
                                + " " + this.OperationTypeString
                                + " " + this.StageString
                                + " " + Elapsed.ToString(ElapsedFormat));
                //TODO: timer stuff
            });
    }

    private static string GetStageString(OperationStage st)
    {
        return st switch
        {
            OperationStage.Requested => "В очереди"
            , OperationStage.Started => "Выполняется"
            , OperationStage.Completed => "Завершено"
            , _ => "Ошибка"
        };
    }

    public bool Execute() => ExecuteCommand();

    public abstract bool ExecuteCommand();

    public void UpdateStage(ModelOperationStatusMessage msg)
    {
        Debug.WriteLine(ModelKey + " " + msg.OperationStage);
        this.Stage = msg.OperationStage;
        this.StageDescription = msg.OperationMessage;
    }
}