using IBS.RevitServerTool;

namespace RevitServerViewer;

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
}