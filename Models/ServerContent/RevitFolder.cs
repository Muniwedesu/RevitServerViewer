namespace RevitServerViewer;

public class RevitFolder
{
    private string? _p = null;
    public string Path { get; set; } = string.Empty;
    public long DriveFreeSpace { get; set; }
    public long DriveSpace { get; set; }


    // public ICollection<RevitFileInfo> RevitFiles { get; set; } = new List<RevitFileInfo>();
    public ICollection<RevitFolder> RevitFolders { get; set; } = new List<RevitFolder>();
    public ICollection<RevitModelInfo> Models { get; set; } = new List<RevitModelInfo>();
    public LockState LockState { get; set; }

    [NetJSON.NetJSONProperty("Files")]
    public ICollection<RevitFileInfo> FileInfos { get; set; } = new List<RevitFileInfo>();

    [NetJSON.NetJSONProperty("Folders")]
    public ICollection<RevitFolderInfo> FolderInfos { get; set; } = new List<RevitFolderInfo>();

    public string ServerPath => _p ??= "/|" + Path.Replace('\\', '|');
}