using System.Security.Principal;

namespace RevitServerViewer.Models.ServerContent;

public struct ServerProгperties
{
    //todo: not sure about that
    public ICollection<TokenAccessLevels> AccessLevelTypes { get; set; }
    public string MachineName { get; set; }
    public int MaximumFolderPathLength { get; set; }
    public int MaximumModelNameLength { get; set; }
    public ICollection<ServerRole> ServerRoles { get; set; }
    public ICollection<string> Servers { get; set; }
}