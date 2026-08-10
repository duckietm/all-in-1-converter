using Habbo_Downloader.App.Workspaces;

namespace Habbo_Downloader.App.Professional.ViewModels;

public sealed class AssetWorkspaceViewModel : ObservableObject
{
    private readonly WorkspaceSettingsStore _settings;
    private string _rootPath = string.Empty;
    private string _statusText = "Choose your nitro-assets folder";
    private bool _isBusy;
    private AssetWorkspacePaths? _paths;
    private AssetWorkspaceSnapshot? _snapshot;

    public AssetWorkspaceViewModel(WorkspaceSettingsStore settings)
    {
        _settings = settings;
        RootPath = settings.LoadRoot() ?? string.Empty;
    }

    public string RootPath { get => _rootPath; set => SetProperty(ref _rootPath, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public bool IsBusy { get => _isBusy; private set => SetProperty(ref _isBusy, value); }
    public AssetWorkspacePaths? Paths { get => _paths; private set => SetProperty(ref _paths, value); }
    public AssetWorkspaceSnapshot? Snapshot { get => _snapshot; private set => SetProperty(ref _snapshot, value); }
    public bool IsConfigured => Paths is not null && Snapshot?.IsReady == true;
    public int ExistingFolderCount => Snapshot?.ExistingFolderCount ?? 0;
    public int TotalFiles => Snapshot?.TotalFiles ?? 0;
    public IReadOnlyList<AssetWorkspaceFolder> Folders => Snapshot?.Folders ?? [];

    public async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(RootPath)) return;
        await RefreshAsync(save: false);
    }

    public Task SaveAndRefreshAsync() => RefreshAsync(save: true);

    private async Task RefreshAsync(bool save)
    {
        if (string.IsNullOrWhiteSpace(RootPath))
        {
            StatusText = "Choose your nitro-assets folder";
            return;
        }

        IsBusy = true;
        try
        {
            AssetWorkspacePaths paths = AssetWorkspacePaths.FromRoot(RootPath);
            if (!Directory.Exists(paths.Root))
                throw new DirectoryNotFoundException($"Folder not found: {paths.Root}");
            if (save) _settings.SaveRoot(paths.Root);
            AssetWorkspaceSnapshot snapshot = await Task.Run(() => AssetWorkspaceInspector.Inspect(paths));
            Paths = paths;
            Snapshot = snapshot;
            RootPath = paths.Root;
            AssetWorkspaceRuntime.Set(paths);
            StatusText = $"{snapshot.ExistingFolderCount}/5 folders ready · {snapshot.TotalFiles:N0} files";
            NotifySummaryChanged();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            Paths = null;
            Snapshot = null;
            StatusText = ex.Message;
            AssetWorkspaceRuntime.Set(null);
            NotifySummaryChanged();
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void NotifySummaryChanged()
    {
        OnPropertyChanged(nameof(IsConfigured));
        OnPropertyChanged(nameof(ExistingFolderCount));
        OnPropertyChanged(nameof(TotalFiles));
        OnPropertyChanged(nameof(Folders));
    }
}
