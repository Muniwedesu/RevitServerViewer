using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Windows.Threading;
using IBS.RevitServerTool;
using IBS.Shared;
using Microsoft.Win32;
using RevitServerViewer.Services;
using RevitServerViewer.ViewModels;
using RevitServerViewer.Views;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.File;
using Splat;
using ILogger = Splat.ILogger;

namespace RevitServerViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static readonly Dictionary<string, string> RevitLocations = new();
    private static readonly string[] RevitVersions = new[] { "20", "21", "22", "23" };
    private readonly string[] _templates = { "Revit 20{0}", "{{7346B4A0-{0}00-0510-0000-705C0D862004}}" };

    public const string UninstallLocation = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";

    private readonly Serilog.ILogger _log;

    public App()
    {
        _log = LoggerService.CreateLogger();
        foreach (var ver in RevitVersions)
        {
            foreach (var template in _templates)
            {
                var fullKeyPath = UninstallLocation + string.Format(template, ver);
                var rvtPath = Registry.GetValue(fullKeyPath, "InstallLocation", string.Empty) as string;
                var key = "20" + ver;
                if (!RevitLocations.ContainsKey(key)) RevitLocations.Add(key, string.Empty);
                if (!string.IsNullOrEmpty(rvtPath))
                {
                    if (!(Directory.Exists(rvtPath) &&
                          File.Exists(Path.Join(rvtPath, "Revit.exe"))))
                    {
                        _log.Information("Found {2} for {1}, but path is empty ({0})", rvtPath, ver, fullKeyPath);
                        continue;
                    }

                    RevitLocations[key] = rvtPath;
                    _log.Information("Found {2} setting {1} revit path to {0}", rvtPath, ver, fullKeyPath);
                    break;
                }

                _log.Information(fullKeyPath + " key not found");
            }
        }

        LatestRevitLocation = RevitLocations.Values.Last(x => !string.IsNullOrEmpty(x));
        RevitServerDownloader.RevitLocation = LatestRevitLocation;

        AppDomain.CurrentDomain.AssemblyResolve += RevitServerDownloader.ResolveAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += LoadRevitApi;

        var dc = new DialogCloser("caption", "text");
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
        Locator.CurrentMutable.RegisterLazySingleton(() => new RevitServerService());
        Locator.CurrentMutable.RegisterConstant(new IpcService());
        Locator.CurrentMutable.Register(() => new ModelDownloadTaskView()
            , typeof(IViewFor<ModelDownloadTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelDetachTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelDiscardTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelExportTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelCleanupTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelSaveTaskViewModel>));
        Locator.CurrentMutable.Register(() => new ModelTaskView(), typeof(IViewFor<ModelErrorTaskViewModel>));
        Locator.CurrentMutable.RegisterConstant(_log, typeof(Serilog.ILogger));
        Locator.CurrentMutable.RegisterLazySingleton(() => new SaveSettingsViewModel(), typeof(SaveSettingsViewModel));
        Locator.CurrentMutable.RegisterLazySingleton(() => new ProcessesViewModel(), typeof(ProcessesViewModel));
        // var pipes = H.Pipes.PipeWatcher.GetActivePipes()
        //     .Where(x => !(x.Contains("jetbrains")
        //                   || x.Contains("mojo")
        //                   || x.Contains("NvMessage")
        //                   || x.Contains("crashpad")
        //                   || x.Contains("cubeb")
        //                   || x.Contains("dotnet-diagnostic")
        //                   || x.Contains("pgsignal")
        //                   || x.Contains("Winsock2")
        //                   || x.Contains("Zoom")
        //                   || x.Contains("AppContracts_")
        //                   || x.Contains("pgsignal")
        //                   || x.Contains("gecko")
        //         ))
        //     .OrderBy(x => x)
        //     .ToArray();
        // Environment.FailFast("123");
        _log.Information("Started");
        Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        AppDomain.CurrentDomain.FirstChanceException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        Application.Current.Exit += (sender, args) => { _log.Information("Shutting down"); };
    }

    public string LatestRevitLocation { get; set; }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // _log.Warning("Task exception");
        _log.Error(e.Exception, nameof(TaskSchedulerOnUnobservedTaskException));
    }

    private void CurrentDomainOnUnhandledException(object? sender, FirstChanceExceptionEventArgs e)
    {
        // _log.Warning("Domain exception");
        _log.Error((Exception)(e.Exception), nameof(CurrentDomainOnUnhandledException));
    }

    private void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        // _log.Warning("Domain exception");
        _log.Error((Exception)(e.ExceptionObject), nameof(CurrentDomainOnUnhandledException));
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // _log.Warning("Dispatcher exception");
        _log.Error(e.Exception, nameof(OnDispatcherUnhandledException));
    }

    private Assembly? LoadRevitApi(object? sender, ResolveEventArgs args)
    {
        //var baseDir = new DirectoryInfo("./");
        //var baseFiles = baseDir.EnumerateFiles("*.dll");
        //var target = baseFiles.FirstOrDefault(x => x.Name.Contains(string.Join("", args.Name.TakeWhile(c => c!= ','))));
        //if (target is not null) return Assembly.LoadFrom(target.FullName);
        var d = new DirectoryInfo(LatestRevitLocation);
        var files = d.EnumerateFiles("*.dll");
        var ass = files.FirstOrDefault(f => f.Name.Replace(f.Extension, string.Empty) == args.Name.Split(',').First());
        if (ass is null) return null;
        return Assembly.LoadFrom(ass.FullName);
    }
}