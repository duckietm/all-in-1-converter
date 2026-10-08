using System.Collections.ObjectModel;
using System.Text;
using Avalonia.Collections;
using Habbo_Downloader.App.Operations;

namespace Habbo_Downloader.App.Professional.ViewModels;

public sealed class ProfessionalShellViewModel : ObservableObject, IAsyncDisposable
{
    private readonly OperationRunner _runner = new();
    private readonly SynchronizationContext? _uiContext = SynchronizationContext.Current;
    // Output waits here and reaches the UI as lines every 100 ms; only the newest lines are kept.
    private readonly StringBuilder _pendingOutput = new();
    private const int MaxLogLines = 5_000;
    private const int MaxPendingChars = 2_000_000;
    private const int LogFlushMs = 100;
    private bool _logFlushPending;
    private bool _lastLineOpen;
    private OperationDefinition? _selectedOperation;
    private string _pageTitle = "Dashboard";
    private string _pageSubtitle = "Your Habbo asset workstation at a glance";
    private string _statusText = "Ready";
    private string _inputText = string.Empty;
    private bool _isRunning;
    private bool _hasFinishedRun;
    private bool _lastRunSucceeded;

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
    /// <summary>The output of the current or last run; the last line is still open for more text.</summary>
    public AvaloniaList<string> LogLines { get; } = new();

    /// <summary>Raised on the UI thread after new output was added to <see cref="LogLines"/>.</summary>
    public event Action? LogUpdated;

    public string LogText => string.Join(Environment.NewLine, LogLines);
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

    /// <summary>True from the end of a run until its log is cleared (Done, or another page).</summary>
    public bool HasFinishedRun { get => _hasFinishedRun; private set => SetProperty(ref _hasFinishedRun, value); }

    /// <summary>Outcome of the last run; read it while <see cref="HasFinishedRun"/> is true.</summary>
    public bool LastRunSucceeded { get => _lastRunSucceeded; private set => SetProperty(ref _lastRunSucceeded, value); }

    /// <summary>Clears the log of the last run; a running operation keeps its log.</summary>
    public void ClearLog()
    {
        if (IsRunning) return;
        ResetLog();
        HasFinishedRun = false;
        StatusText = "Ready";
    }
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

    public void ShowSettings()
    {
        PageTitle = "Settings";
        PageSubtitle = "Edit config.ini: Habbo hotel, downloads, Nitro retro and database";
        VisibleOperations.Clear();
        SelectedOperation = null;
    }

    public void ShowCategory(OperationCategory category)
    {
        (PageTitle, PageSubtitle) = category switch
        {
            OperationCategory.HabboOriginal => ("Habbo Original", "Download official assets from the Habbo CDN"),
            OperationCategory.NitroCustom => ("Nitro Custom", "Import custom Nitro furniture and clothing"),
            OperationCategory.HotelTools => ("Hotel Tools", "Merge, (de)compile and convert to .nitro / .hab (json + WebP Lossless)"),
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
        ResetLog();
        HasFinishedRun = false;
        IsRunning = true;
        StatusText = $"Running: {operation.Title}";

        try
        {
            OperationResult result = await _runner.RunAsync(operation);
            PostToUi(() =>
            {
                IsRunning = false;
                LastRunSucceeded = result.Succeeded;
                HasFinishedRun = true;
                StatusText = result.Succeeded
                    ? $"{operation.Title} completed in {Duration(result.FinishedAt - result.StartedAt)}"
                    : $"{operation.Title} failed: {result.Error?.Message}";
                RecentRuns.Insert(0, new RunHistoryItem(operation.Title, result.Succeeded, result.FinishedAt));
                while (RecentRuns.Count > 8) RecentRuns.RemoveAt(RecentRuns.Count - 1);
            });
        }
        catch (Exception ex)
        {
            PostToUi(() =>
            {
                IsRunning = false;
                LastRunSucceeded = false;
                HasFinishedRun = true;
                StatusText = $"Error: {ex.Message}";
            });
        }
        finally
        {
            PostToUi(() =>
            {
                if (IsRunning) IsRunning = false;
            });
        }
    }

    /// <summary>Sends the typed answer; an empty answer is Enter, so the prompt takes its default.</summary>
    public void SubmitInput()
    {
        if (!IsRunning) return;
        string value = InputText?.Trim() ?? string.Empty;
        InputText = string.Empty;
        _runner.SubmitInput(value);
    }

    private static string Duration(TimeSpan time) =>
        time.TotalSeconds < 60 ? $"{time.TotalSeconds:0.0} s" : $"{(int)time.TotalMinutes} min {time.Seconds} s";

    public void NotifyCloseBlocked() =>
        StatusText = "Wait for the active operation to finish before closing";

    /// <summary>Collects output from any thread; it is shown at most every 100 ms.</summary>
    private void HandleOutput(string text)
    {
        lock (_pendingOutput)
        {
            _pendingOutput.Append(text);
            // A flood faster than the UI drains: drop the oldest part, it would be trimmed anyway.
            if (_pendingOutput.Length > MaxPendingChars)
                _pendingOutput.Remove(0, _pendingOutput.Length - MaxPendingChars / 2);

            if (_logFlushPending) return;
            _logFlushPending = true;
        }

        _ = Task.Delay(LogFlushMs).ContinueWith(_ => PostToUi(FlushLog), TaskScheduler.Default);
    }

    private void FlushLog()
    {
        string chunk;
        lock (_pendingOutput)
        {
            _logFlushPending = false;
            chunk = _pendingOutput.ToString();
            _pendingOutput.Clear();
        }
        if (chunk.Length == 0) return;

        // The last part has no newline yet: the next output continues it (prompts, progress dots).
        string[] parts = chunk.Replace("\r\n", "\n").Split('\n');
        int first = 0;
        if (_lastLineOpen && LogLines.Count > 0)
        {
            LogLines[^1] += parts[0];
            first = 1;
        }
        if (parts.Length > first) LogLines.AddRange(parts.Skip(first));
        _lastLineOpen = true;

        if (LogLines.Count > MaxLogLines) LogLines.RemoveRange(0, LogLines.Count - MaxLogLines);
        LogUpdated?.Invoke();
    }

    private void ResetLog()
    {
        lock (_pendingOutput) { _pendingOutput.Clear(); }
        LogLines.Clear();
        _lastLineOpen = false;
    }

    private static void PostToUi(Action action)
    {
        if (Avalonia.Threading.Dispatcher.UIThread.CheckAccess())
        {
            action();
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(action);
        }
    }

    public async ValueTask DisposeAsync()
    {
        _runner.OutputReceived -= HandleOutput;
        await _runner.DisposeAsync();
    }
}

public sealed record RunHistoryItem(string Title, bool Succeeded, DateTimeOffset FinishedAt);
