namespace Habbo_Downloader.App.Workspaces;

public sealed record WorkspaceFileChange(string RelativePath, byte[] Content);
public sealed record WorkspaceFilePreview(string RelativePath, string TargetPath, bool WillReplaceExisting);
public sealed record WorkspaceChangePreview(IReadOnlyList<WorkspaceFilePreview> Files);
public sealed record WorkspaceChangeResult(string BackupDirectory, IReadOnlyList<string> WrittenFiles);

public sealed class WorkspaceFileTransaction
{
    private readonly AssetWorkspacePaths _paths;
    private readonly Func<DateTimeOffset> _clock;

    public WorkspaceFileTransaction(AssetWorkspacePaths paths, Func<DateTimeOffset>? clock = null)
    {
        _paths = paths;
        _clock = clock ?? (() => DateTimeOffset.Now);
    }

    public WorkspaceChangePreview Preview(IEnumerable<string> relativePaths)
    {
        WorkspaceFilePreview[] files = relativePaths
            .Select(relativePath =>
            {
                string target = ResolveTarget(relativePath);
                return new WorkspaceFilePreview(relativePath, target, File.Exists(target));
            })
            .ToArray();
        return new WorkspaceChangePreview(files);
    }

    public async Task<WorkspaceChangeResult> ApplyAsync(IEnumerable<WorkspaceFileChange> changes, CancellationToken cancellationToken = default)
    {
        WorkspaceFileChange[] batch = changes.ToArray();
        WorkspaceChangePreview preview = Preview(batch.Select(change => change.RelativePath));
        string backupDirectory = Path.Combine(_paths.Backups, _clock().ToString("yyyyMMdd-HHmmss"));
        var temporaryFiles = new List<(string Temporary, string Target)>();
        var writtenFiles = new List<string>();

        try
        {
            foreach (WorkspaceFilePreview file in preview.Files.Where(file => file.WillReplaceExisting))
            {
                string backup = Path.Combine(backupDirectory, file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                File.Copy(file.TargetPath, backup, overwrite: false);
            }

            foreach (WorkspaceFileChange change in batch)
            {
                string target = ResolveTarget(change.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                string temporary = target + $".{Guid.NewGuid():N}.tmp";
                await File.WriteAllBytesAsync(temporary, change.Content, cancellationToken);
                temporaryFiles.Add((temporary, target));
            }

            foreach ((string temporary, string target) in temporaryFiles)
            {
                File.Move(temporary, target, overwrite: true);
                writtenFiles.Add(target);
            }

            return new WorkspaceChangeResult(backupDirectory, writtenFiles);
        }
        catch
        {
            foreach (string target in writtenFiles.AsEnumerable().Reverse())
            {
                WorkspaceFilePreview file = preview.Files.Single(item => string.Equals(item.TargetPath, target, StringComparison.Ordinal));
                if (file.WillReplaceExisting)
                {
                    string backup = Path.Combine(backupDirectory, file.RelativePath);
                    File.Copy(backup, target, overwrite: true);
                }
                else if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
            throw;
        }
        finally
        {
            foreach ((string temporary, _) in temporaryFiles)
                if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    private string ResolveTarget(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) || Path.IsPathRooted(relativePath))
            throw new InvalidOperationException("Workspace changes require a relative path.");
        string target = Path.GetFullPath(Path.Combine(_paths.Root, relativePath));
        string rootPrefix = _paths.Root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!target.StartsWith(rootPrefix, comparison))
            throw new InvalidOperationException("The target file is outside the configured workspace.");
        return target;
    }
}
