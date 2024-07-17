namespace RevitServerViewer.Models.ServerContent;

public class RevitFolder
{
    private string? _p = null;
    public string Path { get; set; } = string.Empty;
    public long DriveFreeSpace { get; set; }
    public long DriveSpace { get; set; }


    // public ICollection<RevitFileInfo> RevitFiles { get; set; } = new List<RevitFileInfo>();
    public IList<RevitFolder> RevitFolders { get; set; } = new List<RevitFolder>();
    public ICollection<RevitModelInfo> Models { get; set; } = Array.Empty<RevitModelInfo>();
    public LockState LockState { get; set; }

    [NetJSON.NetJSONProperty("Files")]
    public ICollection<RevitFileInfo> FileInfos { get; set; } = Array.Empty<RevitFileInfo>();

    [NetJSON.NetJSONProperty("Folders")]
    public ICollection<RevitFolderInfo> FolderInfos { get; set; } = Array.Empty<RevitFolderInfo>();

    public string ServerPath => _p ??= "/|" + Path.Replace('\\', '|');
    public static RevitFolder Empty { get; } = new();
}