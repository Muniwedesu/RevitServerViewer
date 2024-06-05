using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Threading;
using IBS.RevitServerTool;
using IBS.Shared;
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
    private readonly Serilog.ILogger _log;

    public App()
    {
        AppDomain.CurrentDomain.AssemblyResolve += RevitServerDownloader.ResolveAssembly;
        AppDomain.CurrentDomain.AssemblyResolve += LoadRevitApi;
        Assembly.LoadFrom("C:\\Program Files\\Autodesk\\Revit 2021\\RevitAPI.dll");
        // TryLoadRevit();
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
        _log = LoggerService.CreateLogger();
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
        _log.Information("Started");
        Current.DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
        TaskScheduler.UnobservedTaskException += TaskSchedulerOnUnobservedTaskException;
        Application.Current.Exit += (sender, args) => { _log.Information("Shutting down"); };
    }

    private void TaskSchedulerOnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        // _log.Warning("Task exception");
        _log.Error(e.Exception, nameof(TaskSchedulerOnUnobservedTaskException));
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
        var d = new DirectoryInfo("C:\\Program Files\\Autodesk\\Revit 2021");
        var files = d.EnumerateFiles("*.dll");
        var ass = files.FirstOrDefault(f => f.Name.Replace(f.Extension, string.Empty) == args.Name.Split(',').First());
        if (ass is null) return null;
        return Assembly.LoadFrom(ass.FullName);
    }
}