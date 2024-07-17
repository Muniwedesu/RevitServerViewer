using System.Diagnostics;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace RevitServerViewer.Services;

public class ObservableRevitProcess : IDisposable
{
    private Process _process;
    private readonly IObservable<Unit> _exitObservable;
    private CompositeDisposable _dr = new();
    private IObservable<Unit> _exitSwitch;
    public int Id { get; }
    public int SessionId { get; }
    public bool Exited => _process?.HasExited ?? true;

    public ObservableRevitProcess(string version)
    {
        // _exitSwitch = Observable.Interval(TimeSpan.FromMilliseconds(500))
        //     .Select(x => _process?.HasExited ?? false)
        //     .Buffer(2)
        //     .Where(x => x[0] != x[1] && x[1])
        //     .Select(_ => Unit.Default);
        _process = new Process();
        _dr.Add(_process);
        _process.EnableRaisingEvents = true;
        _process.StartInfo = new ProcessStartInfo($"{App.RevitLocations[version]}\\Revit.exe");
        _exitObservable = Observable.FromEventPattern<EventHandler, EventArgs>(
                h => _process.Exited += h
                , h => _process.Exited -= h)
            .Select(_ => Unit.Default);
        // .Merge(_exitSwitch);
        _process.Start();
        Id = _process.Id;
        SessionId = _process.SessionId;
        Application.Current.Exit += CurrentOnExit;
    }

    private void CurrentOnExit(object sender, ExitEventArgs e)
    {
        if (!_process.HasExited) _process.Kill();
    }

    public void Kill() => _process.Kill();

    public IDisposable OnExit(Action handler)
    {
        return _exitObservable.Subscribe(_ => { handler(); }).DisposeWith(_dr);
    }

    public void Dispose()
    {
        _dr.Dispose();
    }
}