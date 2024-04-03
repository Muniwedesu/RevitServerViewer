using System.Reactive.Linq;
using Timer = MultimediaTimer.Timer;

namespace RevitServerViewer.Models;

public class ObservableTimer
{
    private TimeSpan _elapsed = TimeSpan.Zero;
    private readonly Timer _t;
    public IObservable<TimeSpan> Timer { get; set; }

    public ObservableTimer(TimeSpan interval)
    {
        _t = new Timer();
        _t.Interval = interval;
        _t.Resolution = TimeSpan.FromMilliseconds(50);
        Timer = Observable.FromEventPattern(
            handler => _t.Elapsed += handler,
            handler => _t.Elapsed -= handler
        ).Select(_ => _elapsed += _t.Interval);
    }

    public ObservableTimer() : this(TimeSpan.FromMilliseconds(500)) { }

    public void Start() => _t.Start();
    public void Stop() => _t.Stop();
    public IDisposable Subscribe(Action<TimeSpan> action) => Timer.Subscribe(action);
}