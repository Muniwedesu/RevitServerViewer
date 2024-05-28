using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using IBS.RevitServerTool;
using RevitServerViewer.Services;
using RevitServerViewer.ViewModels;
using RevitServerViewer.Views;
using Splat;

namespace RevitServerViewer;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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
        var pipes = H.Pipes.PipeWatcher.GetActivePipes()
            .Where(x => !(x.Contains("jetbrains")
                          || x.Contains("mojo")
                          || x.Contains("NvMessage")
                          || x.Contains("crashpad")
                          || x.Contains("cubeb")
                          || x.Contains("dotnet-diagnostic")
                          || x.Contains("pgsignal")
                          || x.Contains("Winsock2")
                          || x.Contains("Zoom")
                          || x.Contains("AppContracts_")
                          || x.Contains("pgsignal")
                          || (x.Contains("gecko"))
                ))
            .OrderBy(x => x)
            .ToArray();
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