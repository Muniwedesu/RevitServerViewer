using IBS.IPC.DataTypes;
using Splat;

namespace RevitServerViewer.ViewModels;

public class ModelDetachTaskViewModel : ModelTaskViewModel
{
    public ModelDetachTaskViewModel(string key, string sourceFile) : base(key, sourceFile, sourceFile) { }
    public override OperationType OperationType => OperationType.Detach;
    public override string OperationTypeString => "Отсоединение";

    //TODO: maybe determine what to do in service
    //TODO: maybe use base class for ipc operations with the same execute method, idk
    public override bool Execute()
    {
        var svc = Locator.Current.GetService<RevitServerService>();
        //TODO: enqueue self and subscribe to updates
        return true;
    }
}