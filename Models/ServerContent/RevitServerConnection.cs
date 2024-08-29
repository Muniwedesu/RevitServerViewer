using System.Net;
using System.Net.Http;
using System.Web;
using Splat;
using ILogger = Serilog.ILogger;

namespace RevitServerViewer.Models.ServerContent;

public class RevitServerConnection
{
    private const string root = "/|";
    private readonly HttpClientHandler _requestHandler;
    private readonly ILogger? _log;
    public string ServicePath { get; private set; }
    public const string DefaultHost = "192.168.0.8";
    public string Host { get; set; }
    public HttpClient Client { get; set; }

    public RevitServerConnection(string version, string host = DefaultHost)
    {
        _log = Locator.Current.GetService<Serilog.ILogger>();
        Host = host;
        _requestHandler = new HttpClientHandler
        {
            AutomaticDecompression = DecompressionMethods.All
            , Credentials = CredentialCache.DefaultNetworkCredentials
            , UseDefaultCredentials = true
        };
        Client = new HttpClient(_requestHandler);
        Client.DefaultRequestHeaders.Add("User-Name", Environment.UserName);
        Client.DefaultRequestHeaders.Add("User-Machine-Name", Environment.MachineName);
        Client.DefaultRequestHeaders.Add("Operation-GUID", Guid.NewGuid().ToString());
        ServicePath = @"/RevitServerAdminRESTService" + version + @"/AdminRESTService.svc";
    }


    public async Task<string?> TryGet(string path, string token, CancellationToken ct)
    {
        try
        {
            var contents = await Client.GetStringAsync($"http://{Host}{ServicePath}" + path + token, ct);
            return contents;
        }
        catch
        {
            return string.Empty;
        }
    }

    public async Task<RevitFolder> GetFileStructureAsync(string path, CancellationToken ct)
    {
        path = path.Replace("\\", "|");
        if (!path.StartsWith(root)) path = root + path;

        try
        {
            //TODO: handle canceled exception properly?
            if (ct.IsCancellationRequested) throw new TaskCanceledException();

            var folderContents = await TryGet(Uri.EscapeDataString(path), RequestTokens.Contents, ct);

            var folder = NetJSON.NetJSON.Deserialize<RevitFolder>(folderContents);
            foreach (var fi in folder.FolderInfos)
            {
                folder.RevitFolders.Add(await GetFileStructureAsync(path + (path == root ? "" : "|") + fi.Name, ct));
            }

            foreach (var m in folder.Models)
            {
                (m.FullName, m.ModifiedDate) = await SetModelInfo(path, m.Name, folder.Path, ct);
                _log?.Information("Load model: " + m);
            }

            return folder;
        }
        catch
        {
            return RevitFolder.Empty;
        }
    }

    private async Task<(string fullName, DateTime date)> SetModelInfo(string path, string modelName, string folderPath
        , CancellationToken ct)
    {
        var re = await TryGet(Uri.EscapeDataString(path + "|" + modelName), RequestTokens.ModelInfo, ct);
        var modelInfo = NetJSON.NetJSON.Deserialize<Dictionary<string, string>>(re)["DateModified"];

        var dat = modelInfo.Trim("/Date()".ToCharArray());

        var dt5 = DateTime.UnixEpoch;
        var date = dt5.AddMilliseconds(Int64.Parse(dat));
        return (folderPath + "\\" + modelName, date);
    }

    public async Task<RevitFolder> GetFileStructureAsync(CancellationToken token)
    {
        return await GetFileStructureAsync(root, token);
    }
}