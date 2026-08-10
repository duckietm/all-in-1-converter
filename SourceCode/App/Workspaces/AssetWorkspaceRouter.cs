namespace Habbo_Downloader.App.Workspaces;

public enum WorkspaceAssetKind
{
    Furniture,
    Pets,
    Effects,
    Clothing
}

public sealed class AssetWorkspaceRouter
{
    public AssetWorkspaceRouter(AssetWorkspacePaths? paths) => Paths = paths;

    public AssetWorkspacePaths? Paths { get; }
    public bool IsConfigured => Paths is not null;

    public string AssetDirectory(WorkspaceAssetKind kind, string fallback) => Paths is null
        ? fallback
        : kind switch
        {
            WorkspaceAssetKind.Furniture => Paths.Furniture,
            WorkspaceAssetKind.Pets => Paths.Pets,
            WorkspaceAssetKind.Effects => Paths.Effects,
            WorkspaceAssetKind.Clothing => Paths.Clothing,
            _ => fallback
        };

    public string GameDataFile(string fileName, string fallback) =>
        Paths is null ? fallback : Path.Combine(Paths.GameData, fileName);
}

public static class AssetWorkspaceRuntime
{
    private static AssetWorkspaceRouter _router = new(null);

    public static AssetWorkspaceRouter Router => Volatile.Read(ref _router);

    public static void Load(string configPath)
    {
        try
        {
            string? root = new WorkspaceSettingsStore(configPath).LoadRoot();
            Set(string.IsNullOrWhiteSpace(root) ? null : AssetWorkspacePaths.FromRoot(root));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Console.Error.WriteLine($"Workspace configuration ignored: {ex.Message}");
            Set(null);
        }
    }

    public static void Set(AssetWorkspacePaths? paths) =>
        Volatile.Write(ref _router, new AssetWorkspaceRouter(paths));
}
