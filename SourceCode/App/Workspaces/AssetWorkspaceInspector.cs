namespace Habbo_Downloader.App.Workspaces;

public sealed record AssetWorkspaceFolder(string Name, string Path, bool Exists, int FileCount);

public sealed record AssetWorkspaceSnapshot(IReadOnlyList<AssetWorkspaceFolder> Folders)
{
    public int ExistingFolderCount => Folders.Count(folder => folder.Exists);
    public int TotalFiles => Folders.Sum(folder => folder.FileCount);
    public bool IsReady => ExistingFolderCount > 0;
}

public static class AssetWorkspaceInspector
{
    public static AssetWorkspaceSnapshot Inspect(AssetWorkspacePaths paths)
    {
        var folders = new[]
        {
            InspectFolder("Furniture", paths.Furniture),
            InspectFolder("Pets", paths.Pets),
            InspectFolder("Effects", paths.Effects),
            InspectFolder("Clothing", paths.Clothing),
            InspectFolder("Gamedata", paths.GameData)
        };
        return new AssetWorkspaceSnapshot(folders);
    }

    private static AssetWorkspaceFolder InspectFolder(string name, string path)
    {
        if (!Directory.Exists(path)) return new AssetWorkspaceFolder(name, path, false, 0);
        try
        {
            return new AssetWorkspaceFolder(name, path, true, Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories).Count());
        }
        catch (UnauthorizedAccessException)
        {
            return new AssetWorkspaceFolder(name, path, true, 0);
        }
        catch (IOException)
        {
            return new AssetWorkspaceFolder(name, path, true, 0);
        }
    }
}
