using System.Collections.ObjectModel;
using DynamicData;
using DynamicData.Alias;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace RevitServerViewer;

public class TreeItem : ReactiveObject
{
    [Reactive] public bool IsSelected { get; set; }
    public string DisplayName { get; set; }
    public ObservableCollection<TreeItem> Children { get; set; } = new();
    [Reactive] public bool IsExpanded { get; set; } = true;

    public TreeItem()
    {
        this.WhenAnyValue(x => x.IsSelected)
            .Subscribe(SelectChildren);
        this.Children
            .ToObservableChangeSet(x => x.DisplayName)
            .AutoRefresh(x => x.IsSelected)
            .Subscribe(x =>
            {
                if (Children.All(ch => ch.IsSelected)) this.IsSelected = true;
                if (Children.All(ch => !ch.IsSelected)) this.IsSelected = false;
            });
    }

    private void SelectChildren(bool b)
    {
        foreach (var ch in Children.Where(ch => ch.IsSelected != b).ToArray()) ch.IsSelected = b;
    }
}