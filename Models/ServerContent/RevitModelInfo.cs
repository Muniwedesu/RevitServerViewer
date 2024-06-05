namespace RevitServerViewer.Models.ServerContent;

public class RevitModelInfo : LockableFileSystemInfo
{
    // "LockContext": null,
    // "LockState": 0,
    // "ModelLocksInProgress": null,
    // "ModelSize": 759011233,
    // "Name": "DH5302_AR_SCHOOL1200_R21_PD.rvt",
    // "ProductVersion": 11,
    // "SupportSize": 137659651
    public required string FullName { get; set; }
    public long ModelSize { get; set; }
    public int ProductVersion { get; set; }
    public long SupportSize { get; set; }
    public DateTime ModifiedDate { get; set; }

    public override string ToString()
    {
        return $"{ModifiedDate:dd.MM.yyyy HH:mm:ss} - {FullName}";
    }
}