namespace RevitServerViewer.Models.ServerContent;

public abstract class RevitFileSystemInfo
{
    public string? Name { get; set; }
}

public abstract class LockableFileSystemInfo : RevitFileSystemInfo
{
    public string LockContext { get; set; }
    public LockState LockState { get; set; }
    public ICollection<object> ModelLocksInProgress { get; set; }
}