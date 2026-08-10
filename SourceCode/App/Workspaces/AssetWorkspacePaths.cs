namespace Habbo_Downloader.App.Workspaces;

public sealed record AssetWorkspacePaths(
    string Root,
    string Furniture,
    string Pets,
    string Effects,
    string Clothing,
    string GameData,
    string Backups)
{
    public static AssetWorkspacePaths FromRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Choose a Nitro assets folder.", nameof(root));

        string normalized = Path.GetFullPath(root.Trim());
        string bundled = Path.Combine(normalized, "bundled");
        return new AssetWorkspacePaths(
            normalized,
            Path.Combine(bundled, "furniture"),
            Path.Combine(bundled, "pet"),
            Path.Combine(bundled, "effect"),
            Path.Combine(bundled, "figure"),
            Path.Combine(normalized, "gamedata"),
            Path.Combine(normalized, "backups"));
    }
}
