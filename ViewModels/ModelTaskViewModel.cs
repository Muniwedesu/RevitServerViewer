using System.Diagnostics;
using System.Reactive.Linq;
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
    public string DestinationFile { get; set; }

    public abstract OperationType OperationType { get; }
    public abstract string OperationTypeString { get; }
    [Reactive] public OperationStage Stage { get; set; }
    [Reactive] public string StageString { get; set; }

    [Reactive] public bool IsActive { get; set; }
    [Reactive] public bool IsFinished { get; set; }

    public ObservableTimer TaskTimer { get; set; } = new(TimeSpan.FromMilliseconds(250));

    public ModelTaskViewModel(string key, string sourceFile, string destinationFile)
    {
        TaskTimer.Subscribe(x => Elapsed = x.Milliseconds);
        this.WhenAnyValue(x => x.IsActive).Subscribe(x =>
        {
            if (x) TaskTimer.Start();
            else TaskTimer.Stop();
        });

        this.WhenAnyValue(x => x.IsFinished).Where(x => x)
            .Subscribe(_ => TaskTimer.Stop());


        ModelKey = key;
        SourceFile = sourceFile;
        DestinationFile = destinationFile;
        this.WhenAnyValue(x => x.Stage)
            .Subscribe(st =>
            {
                StageString = st switch
                {
                    OperationStage.Requested => "Ожидается"
                    , OperationStage.Started => "Выполняется"
                    , OperationStage.Completed => "Завершено"
                    , _ => "Ошибка"
                };
                //TODO: timer stuff
            });
    }

    public int Elapsed { get; set; }
    public abstract bool Execute();
}