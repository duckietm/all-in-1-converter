namespace Habbo_Downloader.App.Workspaces;

public sealed class WorkspaceFileWriter
{
    private readonly AssetWorkspacePaths _paths;
    private readonly WorkspaceFileTransaction _transaction;

    public WorkspaceFileWriter(AssetWorkspacePaths paths, Func<DateTimeOffset>? clock = null)
    {
        _paths = paths;
        _transaction = new WorkspaceFileTransaction(paths, clock);
    }

    public WorkspaceChangePreview Preview(IEnumerable<string> targetPaths) =>
        _transaction.Preview(targetPaths.Select(ToRelativePath));

    public Task<WorkspaceChangeResult> WriteAllBytesAsync(
        string targetPath,
        byte[] content,
        CancellationToken cancellationToken = default) =>
        _transaction.ApplyAsync([new WorkspaceFileChange(ToRelativePath(targetPath), content)], cancellationToken);

    private string ToRelativePath(string targetPath)
    {
        string fullPath = Path.GetFullPath(targetPath);
        string relativePath = Path.GetRelativePath(_paths.Root, fullPath);
        if (relativePath == "." || relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("The target file is outside the configured workspace.");
        return relativePath;
    }
}

public static class WorkspaceOutput
{
    public static async Task WriteAllBytesAsync(string targetPath, byte[] content, CancellationToken cancellationToken = default)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is not null && IsInside(paths.Root, targetPath))
        {
            await new WorkspaceFileWriter(paths).WriteAllBytesAsync(targetPath, content, cancellationToken);
            return;
        }

        string? directory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(directory)) Directory.CreateDirectory(directory);
        await File.WriteAllBytesAsync(targetPath, content, cancellationToken);
    }

    private static bool IsInside(string root, string targetPath)
    {
        string normalizedRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        string normalizedTarget = Path.GetFullPath(targetPath);
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        return normalizedTarget.StartsWith(normalizedRoot, comparison);
    }
}
