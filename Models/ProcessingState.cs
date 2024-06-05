using IBS.IPC.DataTypes;
using IBS.RevitServerTool;

namespace RevitServerViewer.Models;

public struct ProcessingState
{
    public ProcessingState(string sourcePath, string destPath, string msg, ProcessingStage stage)
        : this(sourcePath, destPath, stage)
    {
        StateMessage = msg;
    }

    public ProcessingState(string sourcePath, string destPath, ProcessingStage stage)
    {
        SourcePathFile = sourcePath;
        DestFile = destPath;
        Stage = stage;
    }


    public ProcessingStage Stage { get; set; }
    public string SourcePathFile { get; set; }
    public string DestFile { get; set; }
    public string StateMessage { get; set; } = string.Empty;

    public static ProcessingState FromMessage(ModelOperationStatusMessage msg)
    {
        var stage = msg.OperationType switch
        {
            OperationType.Detach => msg.OperationStage switch
            {
                OperationStage.Error => ProcessingStage.DetachError
                , OperationStage.Completed => ProcessingStage.Detached
                , OperationStage.Requested => ProcessingStage.Idle
                , _ => ProcessingStage.Detaching
            }
            , OperationType.Cleanup => msg.OperationStage switch
            {
                OperationStage.Error => ProcessingStage.DetachError
                , OperationStage.Completed => ProcessingStage.Detached
                , OperationStage.Requested => ProcessingStage.Idle
                , _ => ProcessingStage.Detaching
            }
            , OperationType.DiscardLinks => msg.OperationStage switch
            {
                OperationStage.Error => ProcessingStage.DetachError
                , OperationStage.Completed => ProcessingStage.Detached
                , OperationStage.Requested => ProcessingStage.Idle
                , _ => ProcessingStage.Detaching
            }
            , OperationType.Export => msg.OperationStage switch
            {
                OperationStage.Error => ProcessingStage.ExportError
                , OperationStage.Completed => ProcessingStage.Exported
                , OperationStage.Requested => ProcessingStage.Idle
                , _ => ProcessingStage.ExportingFromRevit
            }
        };
        return new ProcessingState(msg.ModelKey, msg.RvtLocation, "message placeholder", stage);
    }
}