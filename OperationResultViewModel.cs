using System.Diagnostics;
using System.Reactive.Linq;
using IBS.RevitServerTool;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer;

public class OperationResultViewModel : ReactiveObject
{
    public OperationResultViewModel(ProcessingState processingState)
    {
        Name = processingState.SourcePathFile;
        Message = processingState.StateMessage;
        Stage = processingState.Stage;
        StartupTime = DateTime.Now;
        this.WhenAnyValue(x => x.StartupTime)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(t =>
            {
                var dT = DateTime.Now - StartupTime;
                Elapsed = $"{dT.Hours:00}:{dT.Minutes:00}:{dT.Seconds:00}";
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
                this.WhenAnyValue(x => x.IsFinished).Where(x => x).Subscribe(_ => timer.Dispose());
                State = Enum.GetName(typeof(ProcessingStage), st)!;
                IsFinished = st
                    switch
                    {
                        ProcessingStage.Completed or ProcessingStage.DownloadError or ProcessingStage.DownloadComplete
                            or ProcessingStage.DetachError or ProcessingStage.SaveError or ProcessingStage.OpenError
                            or ProcessingStage.ExportError or ProcessingStage.Idle => true
                        , _ => false
                    };
            });
    }

    [Reactive] public ProcessingStage Stage { get; set; }

    [Reactive] public bool IsFinished { get; set; } = false;
    [Reactive] public string Elapsed { get; set; } = string.Empty;

    [Reactive] public DateTime StartupTime { get; set; }
    public string DisplayStartupTime { get; set; }

    [Reactive] public string State { get; set; }
    public string Name { get; set; }
    public string Message { get; set; }

    public override string ToString()
    {
        return $"{Name} {DisplayStartupTime} {State}";
    }
}