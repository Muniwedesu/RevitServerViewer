using System.IO;

namespace RevitServerViewer;

public static class PathUtils
{
    public static string SetRvtExtension(string fileNameWithExtension)
    {
        var extension = Path.GetExtension(fileNameWithExtension);
        if (extension.Equals(".rvt", StringComparison.InvariantCultureIgnoreCase))
            return fileNameWithExtension;
        fileNameWithExtension = $"{Path.GetFileNameWithoutExtension(fileNameWithExtension)}.rvt";
        return fileNameWithExtension;
    }

    public static string PersonalFolder()
    {
        var folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
        var strFolderPath = !string.IsNullOrEmpty(folderPath)
            ? Path.Combine(folderPath, "RevitServerDownload")
            : throw new Exception("Failed to determine the user's personal folder name.");
        return CreateFolder(strFolderPath)
            ? strFolderPath
            : throw new Exception($"Failed to create RevitServerTool personal data folder \"{strFolderPath}\".");
    }

    public static bool CreateFolder(string strFolderPath)
    {
        try
        {
            Directory.CreateDirectory(strFolderPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static (string Source, string Destination) GetValidPaths(string modelPath, string destination)
    {
        var fileName = SetRvtExtension(Path.GetFileName(modelPath));
        var src = Path.Combine(Path.GetDirectoryName(modelPath.Replace('/', '\\'))!, fileName);

        if (string.IsNullOrEmpty(destination))
        {
            destination = Path.Combine(PersonalFolder()
                , Path.GetDirectoryName($@"{modelPath.Replace('/', '\\')}")!
                , fileName);
        }
        else
        {
            var path1 = Path.GetDirectoryName(destination);
            var fileNameWithExtension = Path.GetFileName(destination);
            fileNameWithExtension = string.IsNullOrEmpty(fileNameWithExtension)
                ? fileName
                : SetRvtExtension(fileNameWithExtension);
            if (string.IsNullOrEmpty(path1))
                path1 = !destination.Equals(Path.GetPathRoot(destination))
                    ? PersonalFolder()
                    : destination;
            destination = Path.Combine(path1
                , Path.GetDirectoryName(modelPath.Replace('/', '\\'))!
                , fileNameWithExtension);
        }

        return (src, destination);
    }
}