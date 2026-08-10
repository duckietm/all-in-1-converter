using System.Collections.ObjectModel;
using System.Text;
using Habbo_Downloader.App.Operations;

namespace Habbo_Downloader.App.Professional.ViewModels;

public sealed class ProfessionalShellViewModel : ObservableObject, IAsyncDisposable
{
    private readonly OperationRunner _runner = new();
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    private readonly StringBuilder _logBuilder = new();
    private OperationDefinition? _selectedOperation;
    private string _pageTitle = "Dashboard";
    private string _pageSubtitle = "Your Habbo asset workstation at a glance";
    private string _logText = "Select an operation to begin.\n";
    private string _statusText = "Ready";
    private string _inputText = string.Empty;
    private bool _isRunning;

    public ProfessionalShellViewModel()
    {
        VisibleOperations = new ObservableCollection<OperationDefinition>();
        RecentRuns = new ObservableCollection<RunHistoryItem>();
        _runner.OutputReceived += HandleOutput;
    }

    public ObservableCollection<OperationDefinition> VisibleOperations { get; }
    public ObservableCollection<RunHistoryItem> RecentRuns { get; }
    public IReadOnlyList<OperationDefinition> AllOperations => OperationCatalog.All;

    public OperationDefinition? SelectedOperation
    {
        get => _selectedOperation;
        private set
        {
            if (!SetProperty(ref _selectedOperation, value)) return;
            OnPropertyChanged(nameof(CanRun));
            OnPropertyChanged(nameof(NeedsInput));
        }
    }

    public string PageTitle { get => _pageTitle; private set => SetProperty(ref _pageTitle, value); }
    public string PageSubtitle { get => _pageSubtitle; private set => SetProperty(ref _pageSubtitle, value); }
    public string LogText { get => _logText; private set => SetProperty(ref _logText, value); }
    public string StatusText { get => _statusText; private set => SetProperty(ref _statusText, value); }
    public string InputText { get => _inputText; set => SetProperty(ref _inputText, value); }
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value)) return;
            OnPropertyChanged(nameof(CanRun));
        }
    }
    public bool CanRun => SelectedOperation is not null && !IsRunning;
    public bool NeedsInput => SelectedOperation?.RequiresInput == true;

    public void ShowDashboard()
    {
        PageTitle = "Dashboard";
        PageSubtitle = "Your Habbo asset workstation at a glance";
        VisibleOperations.Clear();
        SelectedOperation = null;
    }

    public void ShowAssetWorkspace()
    {
        PageTitle = "Asset Workspace";
        PageSubtitle = "Connect the converter directly to your Nitro asset folders";
        VisibleOperations.Clear();
        SelectedOperation = null;
    }

    public void ShowCategory(OperationCategory category)
    {
        (PageTitle, PageSubtitle) = category switch
        {
            OperationCategory.HabboOriginal => ("Habbo Original", "Download official assets from the Habbo CDN"),
            OperationCategory.NitroCustom => ("Nitro Custom", "Import custom Nitro furniture and clothing"),
            OperationCategory.HotelTools => ("Hotel Tools", "Merge, compile and convert your asset pipeline"),
            OperationCategory.Database => ("Database", "Inspect and maintain the configured hotel database"),
            _ => ("About", "Version and project information")
        };

        VisibleOperations.Clear();
        foreach (OperationDefinition operation in OperationCatalog.ForCategory(category))
            VisibleOperations.Add(operation);
        SelectedOperation = VisibleOperations.FirstOrDefault();
    }

    public void SelectOperation(OperationDefinition operation) => SelectedOperation = operation;

    public async Task RunSelectedAsync()
    {
        if (!CanRun || SelectedOperation is null) return;
        OperationDefinition operation = SelectedOperation;
        _logBuilder.Clear();
        LogText = string.Empty;
        IsRunning = true;
        StatusText = $"Running: {operation.Title}";

        OperationResult result = await _runner.RunAsync(operation);
        PostToUi(() =>
        {
            IsRunning = false;
            StatusText = result.Succeeded ? "Completed successfully" : $"Failed: {result.Error?.Message}";
            RecentRuns.Insert(0, new RunHistoryItem(operation.Title, result.Succeeded, result.FinishedAt));
            while (RecentRuns.Count > 8) RecentRuns.RemoveAt(RecentRuns.Count - 1);
        });
    }

    public void SubmitInput()
    {
        if (!IsRunning || string.IsNullOrWhiteSpace(InputText)) return;
        string value = InputText;
        InputText = string.Empty;
        _runner.SubmitInput(value);
    }

    public void NotifyCloseBlocked() =>
        StatusText = "Wait for the active operation to finish before closing";

    private void HandleOutput(string text) => PostToUi(() =>
    {
        _logBuilder.Append(text);
        LogText = _logBuilder.ToString();
    });

    private void PostToUi(Action action)
    {
        if (_uiContext is null || SynchronizationContext.Current == _uiContext) action();
        else _uiContext.Post(_ => action(), null);
    }

    public async ValueTask DisposeAsync()
    {
        _runner.OutputReceived -= HandleOutput;
        await _runner.DisposeAsync();
    }
}

public sealed record RunHistoryItem(string Title, bool Succeeded, DateTimeOffset FinishedAt);
