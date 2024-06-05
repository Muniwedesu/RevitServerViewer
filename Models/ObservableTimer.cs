using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Timer = MultimediaTimer.Timer;

namespace RevitServerViewer.Models;

public class ObservableTimer
{
    public IObservable<TimeSpan> Timer { get; set; }
    private int _interval;
    private TimeSpan _lastTime = TimeSpan.Zero;
    private CompositeDisposable _dr = new();

    public ObservableTimer(TimeSpan interval)
    {
        _interval = (int)(interval.TotalMilliseconds + 0.1);
        Timer = CreateInterval();
    }

    private IObservable<TimeSpan> CreateInterval()
    {
        return Observable.Interval(TimeSpan.FromMilliseconds(_interval))
            .Select(tick =>
            {
                var ts = _lastTime + new TimeSpan(10000 * _interval);
                _lastTime = ts;
                return ts;
            })
            .SubscribeOn(ThreadPoolScheduler.Instance);
    }

    public void Reset()
    {
        this._lastTime = TimeSpan.Zero;
    }

    public IDisposable Subscribe(Action<TimeSpan> action, IScheduler scheduler) =>
        Timer.ObserveOn(scheduler).Subscribe(action).DisposeWith(_dr);

    public IDisposable Subscribe(Action<TimeSpan> action) => Subscribe(action, ThreadPoolScheduler.Instance);

    public void Unsubscribe()
    {
        _dr.Dispose();
        _dr = new CompositeDisposable();
    }
}