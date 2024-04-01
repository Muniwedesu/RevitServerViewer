using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using IBS.RevitServerTool;
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
        Locator.CurrentMutable.RegisterViewsForViewModels(Assembly.GetExecutingAssembly());
        Locator.CurrentMutable.RegisterLazySingleton(() => new RevitServerService());
        Locator.CurrentMutable.RegisterConstant(new IpcService());
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