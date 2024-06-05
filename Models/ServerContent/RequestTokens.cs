namespace RevitServerViewer.Models.ServerContent;

public static class RequestTokens
{
    /* GET /{modelPath}/thumbnail?width={width}&height={height} */

    /// <code>GET /serverProperties</code>
    /// <example><code>var info = RequestUri + ModelInfo</code></example>
    public const string ServerProperties = "/serverProperties";

    ///<code>GET /{folderPath}/contents</code>
    /// <example>var info = RequestUri + ModelInfo</example>
    public const string Contents = "/contents";

    ///<code>GET /{folderPath}/DirectoryInfo</code>
    /// <example>var info = RequestUri + ModelInfo</example>
    public const string DirInfo = "/DirectoryInfo";

    ///<code>GET /{modelPath}/history</code>
    /// <example>var info = RequestUri + ModelInfo</example>
    public const string History = "/history";

    ///<code>GET /{modelPath}/projectInfo</code>
    /// <example>var info = RequestUri + ModelInfo</example>
    public const string ProjectInfo = "/projectInfo";

    /// <code>GET /{modelPath}/modelInfo</code>
    /// <example>var info = RequestUri + ModelInfo</example>
    public const string ModelInfo = "/modelInfo";
}