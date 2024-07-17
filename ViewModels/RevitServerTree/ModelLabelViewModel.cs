using RevitServerViewer.Models.ServerContent;

namespace RevitServerViewer.ViewModels;

public class ModelLabelViewModel : TreeItem
{
    public ModelLabelViewModel(RevitModelInfo revitModelInfo)
    {
        FullName = revitModelInfo.FullName;
        ModifiedDate = revitModelInfo.ModifiedDate;
        DisplayName = revitModelInfo.Name;
    }

    public string FullName { get; set; }
    public DateTime ModifiedDate { get; set; }
    public override string ToString() => $"{DisplayName}";
}