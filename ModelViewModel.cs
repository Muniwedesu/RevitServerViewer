namespace RevitServerViewer;

public class ModelViewModel : TreeItem
{
    public ModelViewModel(RevitModelInfo revitModelInfo)
    {
        FullName = revitModelInfo.FullName;
        DisplayName = revitModelInfo.Name;
    }

    public string FullName { get; set; }
    public override string ToString() => $"{DisplayName}";
}