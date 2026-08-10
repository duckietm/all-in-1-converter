using System.Text;
using Habbo_Downloader.App.Professional.ViewModels;
using Habbo_Downloader.App.Workspaces;
using Xunit;

namespace Habbo_Downloader.Tests.App;

public sealed class AssetWorkspaceTests
{
    [Fact]
    public void WorkspaceResolvesAndInspectsKnownNitroFolders()
    {
        using var directory = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(directory.Path, "bundled", "furniture"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "bundled", "pet"));
        Directory.CreateDirectory(Path.Combine(directory.Path, "gamedata"));
        File.WriteAllText(Path.Combine(directory.Path, "bundled", "furniture", "chair.nitro"), "asset");
        File.WriteAllText(Path.Combine(directory.Path, "gamedata", "FurnitureData.json"), "{}");

        AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(directory.Path);
        AssetWorkspaceSnapshot snapshot = AssetWorkspaceInspector.Inspect(paths);

        Assert.Equal(Path.GetFullPath(directory.Path), paths.Root);
        Assert.Equal(Path.Combine(paths.Root, "bundled", "furniture"), paths.Furniture);
        Assert.Equal(Path.Combine(paths.Root, "bundled", "pet"), paths.Pets);
        Assert.Equal(Path.Combine(paths.Root, "bundled", "effect"), paths.Effects);
        Assert.Equal(Path.Combine(paths.Root, "bundled", "figure"), paths.Clothing);
        Assert.Equal(Path.Combine(paths.Root, "gamedata"), paths.GameData);
        Assert.Equal(Path.Combine(paths.Root, "backups"), paths.Backups);
        Assert.Equal(2, snapshot.TotalFiles);
        Assert.Equal(3, snapshot.ExistingFolderCount);
        Assert.Equal(5, snapshot.Folders.Count);
    }

    [Fact]
    public void SettingsStorePreservesConfigAndReplacesWorkspaceRoot()
    {
        using var directory = new TemporaryDirectory();
        string configPath = Path.Combine(directory.Path, "config.ini");
        File.WriteAllText(configPath, "[AppSettings]\nDATABASESERVER=127.0.0.1\nnitro_assets_root=C:\\old\\assets\n");
        var store = new WorkspaceSettingsStore(configPath);
        string expected = Path.Combine(directory.Path, "nitro-assets");

        store.SaveRoot(expected);

        Assert.Equal(Path.GetFullPath(expected), store.LoadRoot());
        string content = File.ReadAllText(configPath);
        Assert.Contains("DATABASESERVER=127.0.0.1", content);
        Assert.Contains($"nitro_assets_root={Path.GetFullPath(expected)}", content);
        Assert.DoesNotContain("C:\\old\\assets", content);
    }

    [Fact]
    public async Task ApplyingChangesBacksUpExistingFilesBeforeReplacement()
    {
        using var directory = new TemporaryDirectory();
        AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(directory.Path);
        string relativePath = Path.Combine("bundled", "furniture", "chair.nitro");
        string targetPath = Path.Combine(paths.Root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        await File.WriteAllTextAsync(targetPath, "old");
        var transaction = new WorkspaceFileTransaction(
            paths,
            () => new DateTimeOffset(2026, 8, 10, 18, 45, 0, TimeSpan.Zero));

        WorkspaceChangePreview preview = transaction.Preview([relativePath]);
        WorkspaceChangeResult result = await transaction.ApplyAsync(
            [new WorkspaceFileChange(relativePath, Encoding.UTF8.GetBytes("new"))]);

        Assert.True(preview.Files.Single().WillReplaceExisting);
        Assert.Equal("new", await File.ReadAllTextAsync(targetPath));
        string backupPath = Path.Combine(paths.Backups, "20260810-184500", relativePath);
        Assert.Equal("old", await File.ReadAllTextAsync(backupPath));
        Assert.Equal(Path.Combine(paths.Backups, "20260810-184500"), result.BackupDirectory);
        Assert.Equal(targetPath, result.WrittenFiles.Single());
    }

    [Fact]
    public void PreviewRejectsFilesOutsideTheWorkspace()
    {
        using var directory = new TemporaryDirectory();
        var transaction = new WorkspaceFileTransaction(AssetWorkspacePaths.FromRoot(directory.Path));

        Assert.Throws<InvalidOperationException>(() => transaction.Preview([Path.Combine("..", "outside.txt")]));
    }

    [Fact]
    public async Task ViewModelSavesAndReportsTheSelectedWorkspace()
    {
        using var directory = new TemporaryDirectory();
        string root = Path.Combine(directory.Path, "nitro-assets");
        Directory.CreateDirectory(Path.Combine(root, "bundled", "furniture"));
        await File.WriteAllTextAsync(Path.Combine(root, "bundled", "furniture", "chair.nitro"), "asset");
        var viewModel = new AssetWorkspaceViewModel(new WorkspaceSettingsStore(Path.Combine(directory.Path, "config.ini")));
        viewModel.RootPath = root;

        await viewModel.SaveAndRefreshAsync();

        Assert.True(viewModel.IsConfigured);
        Assert.Equal(1, viewModel.TotalFiles);
        Assert.Equal(1, viewModel.ExistingFolderCount);
        Assert.Contains("1 files", viewModel.StatusText);
        Assert.Equal(Path.GetFullPath(root), viewModel.Paths!.Root);
    }

    [Fact]
    public void RouterMapsAssetTypesWithoutChangingLegacyFallbacks()
    {
        using var directory = new TemporaryDirectory();
        AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(directory.Path);
        var configured = new AssetWorkspaceRouter(paths);
        var legacy = new AssetWorkspaceRouter(null);

        Assert.Equal(paths.Furniture, configured.AssetDirectory(WorkspaceAssetKind.Furniture, "legacy-furni"));
        Assert.Equal(paths.Pets, configured.AssetDirectory(WorkspaceAssetKind.Pets, "legacy-pets"));
        Assert.Equal(paths.Effects, configured.AssetDirectory(WorkspaceAssetKind.Effects, "legacy-effects"));
        Assert.Equal(paths.Clothing, configured.AssetDirectory(WorkspaceAssetKind.Clothing, "legacy-clothing"));
        Assert.Equal(Path.Combine(paths.GameData, "FurnitureData.json"), configured.GameDataFile("FurnitureData.json", "legacy.json"));
        Assert.Equal("legacy-furni", legacy.AssetDirectory(WorkspaceAssetKind.Furniture, "legacy-furni"));
        Assert.Equal("legacy.json", legacy.GameDataFile("FurnitureData.json", "legacy.json"));
    }

    [Fact]
    public async Task WorkspaceWriterUsesTransactionalBackupForRoutedTargets()
    {
        using var directory = new TemporaryDirectory();
        AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(directory.Path);
        string target = Path.Combine(paths.GameData, "FigureData.json");
        Directory.CreateDirectory(paths.GameData);
        await File.WriteAllTextAsync(target, "old");
        var writer = new WorkspaceFileWriter(
            paths,
            () => new DateTimeOffset(2026, 8, 10, 20, 15, 0, TimeSpan.Zero));

        WorkspaceChangePreview preview = writer.Preview([target]);
        await writer.WriteAllBytesAsync(target, Encoding.UTF8.GetBytes("new"));

        Assert.True(preview.Files.Single().WillReplaceExisting);
        Assert.Equal("new", await File.ReadAllTextAsync(target));
        Assert.Equal("old", await File.ReadAllTextAsync(Path.Combine(paths.Backups, "20260810-201500", "gamedata", "FigureData.json")));
    }

    [Fact]
    public async Task FailedBatchRestoresFilesAlreadyReplaced()
    {
        using var directory = new TemporaryDirectory();
        AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(directory.Path);
        string firstRelative = Path.Combine("gamedata", "FurnitureData.json");
        string firstTarget = Path.Combine(paths.Root, firstRelative);
        Directory.CreateDirectory(paths.GameData);
        await File.WriteAllTextAsync(firstTarget, "old");
        Directory.CreateDirectory(Path.Combine(paths.Root, "blocked"));
        var transaction = new WorkspaceFileTransaction(paths);

        await Assert.ThrowsAnyAsync<Exception>(() => transaction.ApplyAsync(
        [
            new WorkspaceFileChange(firstRelative, Encoding.UTF8.GetBytes("new")),
            new WorkspaceFileChange("blocked", Encoding.UTF8.GetBytes("cannot replace a directory"))
        ]));

        Assert.Equal("old", await File.ReadAllTextAsync(firstTarget));
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"all-in-1-tests-{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
