using DynamicData;
using RevitServerViewer.Models.ServerContent;
using System.Reactive;
using System.Reactive.Subjects;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer.ViewModels;

public class FolderLabelViewModel : TreeItem
{
    public FolderLabelViewModel(RevitFolder f, string server) : this(f)
    {
        this.DisplayName = server;
    }

    public FolderLabelViewModel(RevitFolder f)
    {
        var s = new ReplaySubject<bool>(1);
        s.OnNext(false);
        s.Subscribe(x => this.IsExpandable = x);
        ToggleExpand = ReactiveCommand.Create(() => IsExpanded = !IsExpanded, s);
        DisplayName = f.Path?.Split("\\").LastOrDefault();
        Children.AddRange(f.Models.Select(x => new ModelLabelViewModel(x) as TreeItem)
            .Concat(f.RevitFolders.Select(x => new FolderLabelViewModel(x))));
    }

    [Reactive] public bool IsExpandable { get; set; }

    public ReactiveCommand<Unit, bool> ToggleExpand { get; }

    public List<TreeItem> Flatten()
    {
        var list = new List<TreeItem>();
        foreach (var ch in Children)
        {
            list.Add(ch);
            if (ch is FolderLabelViewModel f) list.AddRange(f.Flatten());
        }

        return list;
    }

    public override string ToString() => $"\\{DisplayName}\\";
}