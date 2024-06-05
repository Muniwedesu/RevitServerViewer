using DynamicData;
using RevitServerViewer.Models.ServerContent;

namespace RevitServerViewer.ViewModels;

public class FolderViewModel : TreeItem
{
    public FolderViewModel(RevitFolder f, string server) : this(f)
    {
        this.DisplayName = server;
    }

    public FolderViewModel(RevitFolder f)
    {
        DisplayName = f.Path?.Split("\\").LastOrDefault();
        Children.AddRange(f.Models.Select(x => new ModelViewModel(x) as TreeItem)
            .Concat(f.RevitFolders.Select(x => new FolderViewModel(x))));
    }

    public List<TreeItem> Flatten()
    {
        var list = new List<TreeItem>();
        foreach (var ch in Children)
        {
            list.Add(ch);
            if (ch is FolderViewModel f) list.AddRange(f.Flatten());
        }

        return list;
    }

    public override string ToString() => $"\\{DisplayName}\\";
}