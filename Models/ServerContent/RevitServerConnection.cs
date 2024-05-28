using System.Net;
using System.Net.Http;

namespace RevitServerViewer;

public class RevitServerConnection
{
    private const string root = "/|";
    private readonly HttpClientHandler _requestHandler;
    public string ServicePath { get; private set; }
    public const string DefaultHost = "192.168.0.8";
    public string Host { get; set; }
    public HttpClient Client { get; set; }

    public RevitServerConnection(string version, string host = DefaultHost)
    {
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

    public async Task<RevitFolder> GetFileStructureAsync(string path, CancellationToken ct)
    {
        // if (string.IsNullOrEmpty(path)) path = root;
        path = path.Replace("\\", "|");
        try
        {
            if (ct.IsCancellationRequested) throw new TaskCanceledException();
            if (!path.StartsWith(root)) path = root + path;
            var requestPath = "http://" + Host + ServicePath + path;
            var resp = await Client.GetStringAsync("http://" + Host + ServicePath + path + RequestTokens.Contents, ct);
            var folder = NetJSON.NetJSON.Deserialize<RevitFolder>(resp);
            foreach (var fi in folder.FolderInfos)
            {
                folder.RevitFolders.Add(await GetFileStructureAsync(path + (path == root ? "" : "|") + fi.Name, ct));
            }

            foreach (var m in folder.Models)
            {
                var modelPath = requestPath + "|" + m.Name;
                var re = await Client.GetStringAsync(modelPath + "/ModelInfo", ct);
                var modelInfo = NetJSON.NetJSON.Deserialize<Dictionary<string, string>>(re)["DateModified"];
                //ticks since epoch in milliseconds
                var dat = modelInfo.Trim("/Date()".ToCharArray());
                //1712823803000

                var dt5 = DateTime.UnixEpoch;
                var date = dt5.AddMilliseconds(1712823803000);
                m.FullName = folder.Path + "\\" + m.Name;
                m.ModifiedDate = date;
            }

            return folder;
        }
        catch
        {
            return new RevitFolder();
        }
    }

    public async Task<RevitFolder> GetFileStructureAsync(CancellationToken token)
    {
        return await GetFileStructureAsync(root, token);
    }
}