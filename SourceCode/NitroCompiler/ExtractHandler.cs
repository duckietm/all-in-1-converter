public static class ExtractHandler
{
    public static string[] GetFiles(string folder)
    {
        string fullPath = Habbo_Downloader.App.Workspaces.AssetWorkspaceRuntime.Router.AssetDirectory(
            ToWorkspaceKind(folder),
            Path.Combine("NitroCompiler", "extract", folder));

        if (!Directory.Exists(fullPath))
        {
            Console.WriteLine($"Directory not found: {fullPath}");
            return Array.Empty<string>();
        }
        return Directory.GetFiles(fullPath, "*.nitro");
    }

    public static byte[] ReadFile(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }

    private static Habbo_Downloader.App.Workspaces.WorkspaceAssetKind ToWorkspaceKind(string folder) => folder switch
    {
        "furni" => Habbo_Downloader.App.Workspaces.WorkspaceAssetKind.Furniture,
        "clothing" => Habbo_Downloader.App.Workspaces.WorkspaceAssetKind.Clothing,
        "effects" => Habbo_Downloader.App.Workspaces.WorkspaceAssetKind.Effects,
        "pets" => Habbo_Downloader.App.Workspaces.WorkspaceAssetKind.Pets,
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, "Unknown Nitro asset type.")
    };
}
