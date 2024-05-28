using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Timer = MultimediaTimer.Timer;

namespace RevitServerViewer.Models;

public class ObservableTimer //: IDisposable
{
    // private TimeSpan _elapsed = TimeSpan.Zero;

    // private readonly Timer _t;
    public IObservable<TimeSpan> Timer { get; set; }
    private int _interval;
    private TimeSpan _lastTime = TimeSpan.Zero;
    private CompositeDisposable _dr = new();

    public ObservableTimer(TimeSpan interval)
    {
        _interval = (int)(interval.TotalMilliseconds + 0.1);
        // _t = new Timer();
        // _t.Interval = interval;
        // _t.Resolution = TimeSpan.FromMilliseconds(_interval);
        // Timer = Observable.FromEventPattern(
        //     handler => _t.Elapsed += handler,
        //     handler => _t.Elapsed -= handler
        // ).Select(_ => _elapsed += _t.Interval);
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

    public ObservableTimer() : this(TimeSpan.FromMilliseconds(500)) { }

    public void Start()
    {
        // foreach (var i in Enumerable.Range(0, 5))
        // {
        //     try
        //     {
        //         _t.Start();
        //         return;
        //     }
        //     catch (Exception e)
        //     {
        //         Thread.Sleep(500);
        //         Debug.WriteLine(e.Message);
        //     }
        // }


        // Timer = Timer.Merge();
    }

    // public void Stop() => _t.Stop();
    public IDisposable Subscribe(Action<TimeSpan> action, IScheduler scheduler)
    {
        Debug.WriteLine("Timer sub");
        return Timer.ObserveOn(scheduler).Subscribe(action).DisposeWith(_dr);
    }

    public IDisposable Subscribe(Action<TimeSpan> action) => Subscribe(action, ThreadPoolScheduler.Instance);

    public void Unsubscribe()
    {
        Debug.WriteLine("Timer unsub");
        _dr.Dispose();
        _dr = new CompositeDisposable();
    }
}