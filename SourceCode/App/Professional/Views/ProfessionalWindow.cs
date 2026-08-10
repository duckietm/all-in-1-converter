using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using Habbo_Downloader.App.Professional.ViewModels;
using Habbo_Downloader.App.Workspaces;

namespace Habbo_Downloader.App.Professional.Views;

public sealed class ProfessionalWindow : Window
{
    private readonly TaskCompletionSource _closed = new();
    public Task ClosedTask => _closed.Task;
    private static readonly IBrush Navy = Brush(ProfessionalPalette.SidebarBackground);
    private static readonly IBrush SidebarText = Brush(ProfessionalPalette.SidebarText);
    private static readonly IBrush SidebarMuted = Brush(ProfessionalPalette.SidebarMutedText);
    private static readonly IBrush NavigationHover = Brush(ProfessionalPalette.NavigationHover);
    private static readonly IBrush NavigationSelected = Brush(ProfessionalPalette.NavigationSelected);
    private static readonly IBrush SelectionIndicator = Brush(ProfessionalPalette.SelectionIndicator);
    private static readonly IBrush Accent = Brush(ProfessionalPalette.Accent);
    private static readonly IBrush CallToAction = Brush(ProfessionalPalette.CallToAction);
    private static readonly IBrush CallToActionHover = Brush(ProfessionalPalette.CallToActionHover);
    private static readonly IBrush CallToActionBorder = Brush(ProfessionalPalette.CallToActionBorder);
    private static readonly IBrush Muted = Brush(ProfessionalPalette.ContentMutedText);
    private static readonly HashSet<string> WorkspaceOperationIds =
    [
        "habbo.furnidata",
        "nitro.furniture",
        "nitro.clothes",
        "tools.decompile-nitro",
        "tools.compile-nitro",
        "tools.swf-furniture",
        "tools.swf-clothes",
        "tools.swf-pets",
        "tools.swf-effects",
        "database.offer-id",
        "database.item-settings",
        "database.sprite-id"
    ];
    private readonly ProfessionalShellViewModel _viewModel = new();
    private readonly AssetWorkspaceViewModel _workspaceViewModel = new(
        new WorkspaceSettingsStore(Path.Combine(Environment.CurrentDirectory, "config.ini")));
    private readonly StackPanel _content = new() { Spacing = 16 };
    private readonly TextBlock _title = new() { FontSize = 28, FontWeight = FontWeight.SemiBold };
    private readonly TextBlock _subtitle = new() { FontSize = 14, Foreground = Muted };
    private readonly TextBlock _status = new() { FontSize = 13, Foreground = Muted };
    private readonly TextBox _log = new()
    {
        AcceptsReturn = true,
        IsReadOnly = true,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
        FontSize = 12,
        MinHeight = 170
    };
    private readonly TextBox _input = new() { PlaceholderText = "Type a response and press Enter" };
    private readonly Button _send = new() { Content = "Send", Padding = new Thickness(18, 8) };
    private readonly TextBox _workspacePath = new() { PlaceholderText = @"E:\Users\you\Nitro-Files\nitro-assets" };
    private readonly TextBlock _workspaceStatus = new() { FontSize = 15, FontWeight = FontWeight.SemiBold, Foreground = Accent };
    private readonly WrapPanel _workspaceFolders = new() { Orientation = Orientation.Horizontal };
    private readonly Button _openWorkspace = new() { Content = "Open workspace folder", Padding = new Thickness(18, 8) };
    private readonly Border _activityPanel;
    private readonly Control _workspacePanel;
    private readonly List<Button> _navigationButtons = [];
    private readonly Dictionary<OperationCategory, Button> _categoryNavigation = [];
    private Button? _activeNavigation;

    public ProfessionalWindow()
    {
        Title = "All-in-1 Converter — Professional";
        Width = 1380;
        Height = 860;
        MinWidth = 1040;
        MinHeight = 700;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Default;
        DataContext = _viewModel;
        _openWorkspace.Click += (_, _) => OpenWorkspaceFolder();

        _activityPanel = BuildActivityPanel();
        _workspacePanel = BuildWorkspacePanel();
        Content = BuildLayout();
        _viewModel.PropertyChanged += ViewModelChanged;
        Closing += (_, eventArgs) =>
        {
            if (!_viewModel.IsRunning) return;
            eventArgs.Cancel = true;
            _viewModel.NotifyCloseBlocked();
        };
        Closed += async (_, _) =>
        {
            _viewModel.PropertyChanged -= ViewModelChanged;
            await _viewModel.DisposeAsync();
            _closed.TrySetResult();
        };
        ShowDashboard();
    }

    private Control BuildLayout()
    {
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("250,*") };
        root.Children.Add(BuildSidebar());

        var workspace = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto"),
            Margin = new Thickness(28, 20, 28, 20)
        };
        Grid.SetColumn(workspace, 1);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var heading = new StackPanel { Spacing = 3 };
        heading.Children.Add(_title);
        heading.Children.Add(_subtitle);
        header.Children.Add(heading);
        var badge = new Border
        {
            Background = Brush(ProfessionalPalette.AccentSoft),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(12, 6),
            Child = new TextBlock { Text = ".NET 11 • JSON only", Foreground = Accent, FontWeight = FontWeight.SemiBold }
        };
        Grid.SetColumn(badge, 1);
        header.Children.Add(badge);
        Grid.SetRow(header, 0);
        workspace.Children.Add(header);

        var scroll = new ScrollViewer { Content = _content, Margin = new Thickness(0, 22, 0, 12) };
        Grid.SetRow(scroll, 1);
        workspace.Children.Add(scroll);

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        footer.Children.Add(_status);
        var platform = new TextBlock { Text = "Windows + Linux  •  System theme", Foreground = Muted, FontSize = 12 };
        Grid.SetColumn(platform, 1);
        footer.Children.Add(platform);
        Grid.SetRow(footer, 2);
        workspace.Children.Add(footer);

        root.Children.Add(workspace);
        return root;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*,Auto")
        };
        var brand = new StackPanel { Spacing = 3, Margin = new Thickness(8, 0, 0, 28) };
        brand.Children.Add(new TextBlock { Text = "ALL-IN-1", Foreground = SidebarText, FontSize = 22, FontWeight = FontWeight.Bold });
        brand.Children.Add(new TextBlock { Text = "ASSET WORKSTATION", Foreground = SidebarMuted, FontSize = 11 });
        sidebar.Children.Add(brand);

        var nav = new StackPanel { Spacing = 7 };
        nav.Children.Add(NavButton("⌂  Dashboard", ShowDashboard, isDefault: true));
        nav.Children.Add(NavButton("▣  Asset Workspace", ShowAssetWorkspace));
        nav.Children.Add(NavButton("↓  Habbo Original", () => ShowCategory(OperationCategory.HabboOriginal), OperationCategory.HabboOriginal));
        nav.Children.Add(NavButton("◆  Nitro Custom", () => ShowCategory(OperationCategory.NitroCustom), OperationCategory.NitroCustom));
        nav.Children.Add(NavButton("⚒  Hotel Tools", () => ShowCategory(OperationCategory.HotelTools), OperationCategory.HotelTools));
        nav.Children.Add(NavButton("▤  Database", () => ShowCategory(OperationCategory.Database), OperationCategory.Database));
        nav.Children.Add(NavButton("ⓘ  About", () => ShowCategory(OperationCategory.General), OperationCategory.General));
        Grid.SetRow(nav, 1);
        sidebar.Children.Add(nav);

        var footer = new StackPanel { Spacing = 12, Margin = new Thickness(8, 0) };
        var switchInterface = new Button
        {
            Content = "Switch interface",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(10, 8),
            Background = CallToAction,
            Foreground = Brush(ProfessionalPalette.CallToActionText),
            BorderBrush = CallToActionBorder,
            BorderThickness = new Thickness(1),
            FontWeight = FontWeight.SemiBold
        };
        switchInterface.PointerEntered += (_, _) => switchInterface.Background = CallToActionHover;
        switchInterface.PointerExited += (_, _) => switchInterface.Background = CallToAction;
        switchInterface.Click += async (_, _) => await SwitchInterfaceAsync();
        footer.Children.Add(switchInterface);
        footer.Children.Add(new TextBlock
        {
            Text = "PROFESSIONAL UI\nAdaptive • MVVM",
            Foreground = SidebarMuted,
            FontSize = 11,
            LineHeight = 18
        });
        Grid.SetRow(footer, 2);
        sidebar.Children.Add(footer);
        return new Border
        {
            Background = Navy,
            Padding = new Thickness(18, 22),
            Child = sidebar
        };
    }

    private Button NavButton(
        string label,
        Action action,
        OperationCategory? category = null,
        bool isDefault = false)
    {
        var button = new Button
        {
            Content = label,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(14, 11),
            Background = Brushes.Transparent,
            Foreground = SidebarText,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(8),
            FontSize = 14
        };
        button.PointerEntered += (_, _) =>
        {
            if (button != _activeNavigation) button.Background = NavigationHover;
        };
        button.PointerExited += (_, _) =>
        {
            if (button != _activeNavigation) button.Background = Brushes.Transparent;
        };
        button.Click += (_, _) =>
        {
            if (_viewModel.IsRunning) return;
            ActivateNavigation(button);
            action();
        };
        _navigationButtons.Add(button);
        if (category is not null) _categoryNavigation[category.Value] = button;
        if (isDefault) ActivateNavigation(button);
        return button;
    }

    private void ActivateNavigation(Button selected)
    {
        _activeNavigation = selected;
        foreach (Button button in _navigationButtons)
        {
            bool isSelected = button == selected;
            button.Background = isSelected ? NavigationSelected : Brushes.Transparent;
            button.BorderBrush = isSelected ? SelectionIndicator : Brushes.Transparent;
            button.FontWeight = isSelected ? FontWeight.SemiBold : FontWeight.Normal;
        }
    }

    private async Task SwitchInterfaceAsync()
    {
        if (_viewModel.IsRunning) return;
        var selector = new InterfaceSelectorWindow
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        _ = selector.ShowDialog(this);
        RunMode selected = await selector.ResultTask;
        if (selected is RunMode.Quit or RunMode.Professional) return;
        MenuHost.RequestSwitch(selected);
        Close();
    }

    private void ShowDashboard()
    {
        _viewModel.ShowDashboard();
        _content.Children.Clear();
        _content.Children.Add(BuildOverviewCards());
        _content.Children.Add(BuildQuickActions());
        _content.Children.Add(BuildGettingStarted());
        RefreshHeader();
    }

    private async void ShowAssetWorkspace()
    {
        _viewModel.ShowAssetWorkspace();
        _workspacePath.Text = _workspaceViewModel.RootPath;
        _content.Children.Clear();
        _content.Children.Add(_workspacePanel);
        RefreshHeader();
        await _workspaceViewModel.LoadAsync();
        RefreshWorkspacePage();
    }

    private Control BuildWorkspacePanel()
    {
        var section = new StackPanel { Spacing = 16 };
        section.Children.Add(SectionTitle("Nitro assets folder", "Choose the nitro-assets root. Known folders are detected automatically."));

        var pathRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
        pathRow.Children.Add(_workspacePath);
        var browse = new Button { Content = "Browse…", Padding = new Thickness(18, 8) };
        browse.Click += async (_, _) =>
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select your nitro-assets folder",
                AllowMultiple = false
            });
            string? selected = folders.FirstOrDefault()?.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(selected)) return;
            _workspacePath.Text = selected;
            _workspaceViewModel.RootPath = selected;
        };
        Grid.SetColumn(browse, 1);
        pathRow.Children.Add(browse);
        var save = new Button { Content = "Save and verify", Padding = new Thickness(18, 8) };
        ApplyPrimaryButtonColors(save);
        save.Click += async (_, _) =>
        {
            _workspaceViewModel.RootPath = _workspacePath.Text ?? string.Empty;
            await _workspaceViewModel.SaveAndRefreshAsync();
            RefreshWorkspacePage();
        };
        Grid.SetColumn(save, 2);
        pathRow.Children.Add(save);
        section.Children.Add(Card(pathRow));

        _workspaceStatus.Text = _workspaceViewModel.StatusText;
        section.Children.Add(_workspaceStatus);
        RefreshWorkspaceFolderCards();
        section.Children.Add(_workspaceFolders);

        var safety = new StackPanel { Spacing = 7 };
        safety.Children.Add(new TextBlock { Text = "Safe file interaction", FontSize = 17, FontWeight = FontWeight.SemiBold });
        safety.Children.Add(new TextBlock
        {
            Text = "Compatible operations use these folders directly. Existing files are backed up under backups/<date-time> before replacement; paths outside this workspace are rejected.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Muted
        });
        _openWorkspace.HorizontalAlignment = HorizontalAlignment.Left;
        _openWorkspace.IsEnabled = _workspaceViewModel.Paths is not null;
        safety.Children.Add(_openWorkspace);
        section.Children.Add(Card(safety));
        return section;
    }

    private void RefreshWorkspacePage()
    {
        _workspaceStatus.Text = _workspaceViewModel.StatusText;
        _openWorkspace.IsEnabled = _workspaceViewModel.Paths is not null;
        RefreshWorkspaceFolderCards();
        _status.Text = _workspaceViewModel.StatusText;
    }

    private void RefreshWorkspaceFolderCards()
    {
        _workspaceFolders.Children.Clear();
        IReadOnlyList<AssetWorkspaceFolder> folders = _workspaceViewModel.Folders;
        if (folders.Count == 0)
        {
            _workspaceFolders.Children.Add(Card(new TextBlock { Text = "No workspace verified yet.", Foreground = Muted }));
            return;
        }

        foreach (AssetWorkspaceFolder folder in folders)
        {
            var content = new StackPanel { Spacing = 5 };
            content.Children.Add(new TextBlock { Text = folder.Name, FontSize = 16, FontWeight = FontWeight.SemiBold });
            content.Children.Add(new TextBlock
            {
                Text = folder.Exists ? $"Ready · {folder.FileCount:N0} files" : "Folder not found",
                Foreground = folder.Exists ? new SolidColorBrush(Color.Parse("#15803D")) : new SolidColorBrush(Color.Parse("#A66000"))
            });
            content.Children.Add(new TextBlock { Text = folder.Path, Foreground = Muted, FontSize = 11, TextWrapping = TextWrapping.Wrap, MaxWidth = 280 });
            Border card = Card(content, new Thickness(0, 0, 12, 12));
            card.Width = 320;
            _workspaceFolders.Children.Add(card);
        }
    }

    private void OpenWorkspaceFolder()
    {
        string? path = _workspaceViewModel.Paths?.Root;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void ShowCategory(OperationCategory category)
    {
        if (_categoryNavigation.TryGetValue(category, out Button? navigation))
            ActivateNavigation(navigation);
        _viewModel.ShowCategory(category);
        _content.Children.Clear();
        _content.Children.Add(BuildOperationGrid());
        _content.Children.Add(_activityPanel);
        RefreshHeader();
        RefreshActivity();
    }

    private Control BuildOverviewCards()
    {
        var grid = new UniformGrid { Columns = 4, Rows = 1 };
        grid.Children.Add(StatCard("13", "Official downloads", "Habbo Original", "#536DFE"));
        grid.Children.Add(StatCard("2", "Custom imports", "Nitro Custom", "#8B5CF6"));
        grid.Children.Add(StatCard("10", "Conversion tools", "Hotel Tools", "#0EA5A4"));
        grid.Children.Add(StatCard("5", "Maintenance jobs", "Database", "#F59E0B"));
        return grid;
    }

    private Border StatCard(string value, string caption, string category, string color)
    {
        var panel = new StackPanel { Spacing = 5 };
        panel.Children.Add(new TextBlock { Text = value, FontSize = 30, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse(color)) });
        panel.Children.Add(new TextBlock { Text = caption, FontSize = 14, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock { Text = category, FontSize = 12, Foreground = Muted });
        return Card(panel, new Thickness(0, 0, 12, 0));
    }

    private Control BuildQuickActions()
    {
        var section = new StackPanel { Spacing = 10 };
        section.Children.Add(SectionTitle("Quick actions", "Common operations available in one click"));
        var grid = new UniformGrid { Columns = 3 };
        foreach (OperationDefinition operation in new[]
                 {
                     OperationCatalog.All.Single(x => x.Id == "habbo.all"),
                     OperationCatalog.All.Single(x => x.Id == "tools.generate-sql"),
                     OperationCatalog.All.Single(x => x.Id == "database.info")
                 })
            grid.Children.Add(OperationCard(operation, compact: true));
        section.Children.Add(grid);
        return section;
    }

    private Control BuildGettingStarted()
    {
        var panel = new StackPanel { Spacing = 7 };
        panel.Children.Add(new TextBlock { Text = "Ready for your workflow", FontSize = 17, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock
        {
            Text = "Choose a module on the left. Every existing converter function is available as a native operation card, with live progress, logs and interactive input in this window.",
            TextWrapping = TextWrapping.Wrap,
            Foreground = Muted
        });
        return Card(panel);
    }

    private Control BuildOperationGrid()
    {
        var section = new StackPanel { Spacing = 10 };
        section.Children.Add(SectionTitle("Operations", $"{_viewModel.VisibleOperations.Count} tools available"));
        var grid = new WrapPanel { Orientation = Orientation.Horizontal };
        foreach (OperationDefinition operation in _viewModel.VisibleOperations)
            grid.Children.Add(OperationCard(operation));
        section.Children.Add(grid);
        return section;
    }

    private Border OperationCard(OperationDefinition operation, bool compact = false)
    {
        var panel = new StackPanel { Spacing = 8 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(new TextBlock { Text = operation.Title, FontSize = 15, FontWeight = FontWeight.SemiBold });
        if (operation.IsDestructive)
            titleRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#FFF0D6")),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(7, 2),
                Child = new TextBlock { Text = "CAUTION", Foreground = new SolidColorBrush(Color.Parse("#A66000")), FontSize = 10 }
            });
        panel.Children.Add(titleRow);
        panel.Children.Add(new TextBlock { Text = operation.Description, Foreground = Muted, TextWrapping = TextWrapping.Wrap, MaxWidth = compact ? 300 : 330 });
        var open = new Button { Content = "Open operation  →", HorizontalAlignment = HorizontalAlignment.Left, Foreground = Accent };
        open.Click += (_, _) =>
        {
            if (_viewModel.IsRunning) return;
            ShowCategory(operation.Category);
            _viewModel.SelectOperation(operation);
            RefreshActivity();
        };
        panel.Children.Add(open);
        var card = Card(panel, new Thickness(0, 0, 12, 12));
        card.Width = compact ? 350 : 370;
        return card;
    }

    private Border BuildActivityPanel()
    {
        var grid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), MinHeight = 310 };
        var heading = new TextBlock { Name = "ActivityHeading", FontSize = 18, FontWeight = FontWeight.SemiBold };
        grid.Children.Add(heading);
        var description = new TextBlock { Name = "ActivityDescription", Foreground = Muted, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 6, 0, 12) };
        Grid.SetRow(description, 1);
        grid.Children.Add(description);
        Grid.SetRow(_log, 2);
        grid.Children.Add(_log);

        var actions = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), Margin = new Thickness(0, 10, 0, 0) };
        _input.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            SubmitInput();
            e.Handled = true;
        };
        actions.Children.Add(_input);
        Grid.SetColumn(_send, 1);
        _send.Click += (_, _) => SubmitInput();
        actions.Children.Add(_send);
        var run = new Button
        {
            Name = "RunButton",
            Content = "Run operation",
            Background = CallToAction,
            Foreground = Brush(ProfessionalPalette.CallToActionText),
            BorderBrush = CallToActionBorder,
            Padding = new Thickness(22, 9),
            Margin = new Thickness(10, 0, 0, 0)
        };
        run.Click += async (_, _) => await RunSelectedAsync();
        ApplyPrimaryButtonColors(run);
        Grid.SetColumn(run, 2);
        actions.Children.Add(run);
        Grid.SetRow(actions, 3);
        grid.Children.Add(actions);
        return Card(grid);
    }

    private async Task RunSelectedAsync()
    {
        OperationDefinition? operation = _viewModel.SelectedOperation;
        if (operation is null || !_viewModel.CanRun) return;
        if (AssetWorkspaceRuntime.Router.IsConfigured && WorkspaceOperationIds.Contains(operation.Id) &&
            !await ConfirmWorkspaceAccessAsync(operation)) return;
        if (operation.IsDestructive && !await ConfirmDestructiveAsync(operation.Title)) return;
        await _viewModel.RunSelectedAsync();
    }

    private async Task<bool> ConfirmWorkspaceAccessAsync(OperationDefinition operation)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is null) return true;
        var dialog = new Window
        {
            Title = "Review workspace access",
            Width = 620,
            Height = 300,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = false;
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 12 };
        panel.Children.Add(new TextBlock { Text = operation.Title, FontSize = 20, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock
        {
            Text = $"This operation will interact with the configured Nitro workspace:\n{paths.Root}",
            TextWrapping = TextWrapping.Wrap
        });
        panel.Children.Add(new TextBlock
        {
            Text = "Existing files written by compatible operations are backed up automatically before replacement.",
            Foreground = Muted,
            TextWrapping = TextWrapping.Wrap
        });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(18, 8) };
        cancel.Click += (_, _) => dialog.Close();
        var confirm = new Button { Content = "Continue with workspace", Padding = new Thickness(18, 8) };
        ApplyPrimaryButtonColors(confirm);
        confirm.Click += (_, _) => { result = true; dialog.Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(confirm);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        await dialog.ShowDialog(this);
        return result;
    }

    private async Task<bool> ConfirmDestructiveAsync(string title)
    {
        var dialog = new Window
        {
            Title = "Confirm operation",
            Width = 470,
            Height = 230,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var result = false;
        var panel = new StackPanel { Margin = new Thickness(24), Spacing = 14 };
        panel.Children.Add(new TextBlock { Text = "Database change", FontSize = 20, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock { Text = $"{title} can modify your database. Confirm only if your configuration and backups are ready.", TextWrapping = TextWrapping.Wrap });
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(18, 8) };
        cancel.Click += (_, _) => dialog.Close();
        var confirm = new Button { Content = "Confirm and run", Padding = new Thickness(18, 8) };
        ApplyPrimaryButtonColors(confirm);
        confirm.Click += (_, _) => { result = true; dialog.Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(confirm);
        panel.Children.Add(buttons);
        dialog.Content = panel;
        await dialog.ShowDialog(this);
        return result;
    }

    private void SubmitInput()
    {
        _viewModel.InputText = _input.Text ?? string.Empty;
        _viewModel.SubmitInput();
        _input.Text = string.Empty;
    }

    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ProfessionalShellViewModel.LogText))
        {
            _log.Text = _viewModel.LogText;
            _log.CaretIndex = _log.Text?.Length ?? 0;
        }
        RefreshHeader();
        RefreshActivity();
    }

    private void RefreshHeader()
    {
        _title.Text = _viewModel.PageTitle;
        _subtitle.Text = _viewModel.PageSubtitle;
        _status.Text = _viewModel.StatusText;
    }

    private void RefreshActivity()
    {
        if (_activityPanel.Child is not Grid grid) return;
        OperationDefinition? operation = _viewModel.SelectedOperation;
        if (grid.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "ActivityHeading") is { } heading)
            heading.Text = operation?.Title ?? "Select an operation";
        if (grid.Children.OfType<TextBlock>().FirstOrDefault(x => x.Name == "ActivityDescription") is { } description)
            description.Text = operation is null
                ? "Choose a card above to inspect and run it."
                : operation.Description + WorkspaceTargetSummary(operation);
        if (grid.Children.OfType<Grid>().SelectMany(x => x.Children).OfType<Button>().FirstOrDefault(x => x.Name == "RunButton") is { } run)
            run.IsEnabled = _viewModel.CanRun;
        _input.IsVisible = operation?.RequiresInput == true;
        _send.IsVisible = operation?.RequiresInput == true;
    }

    private static StackPanel SectionTitle(string title, string subtitle)
    {
        var panel = new StackPanel { Spacing = 2 };
        panel.Children.Add(new TextBlock { Text = title, FontSize = 19, FontWeight = FontWeight.SemiBold });
        panel.Children.Add(new TextBlock { Text = subtitle, Foreground = Muted, FontSize = 13 });
        return panel;
    }

    private static string WorkspaceTargetSummary(OperationDefinition operation)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is null || !WorkspaceOperationIds.Contains(operation.Id)) return string.Empty;
        return $"\n\nWorkspace enabled: {paths.Root}";
    }

    private static Border Card(Control content, Thickness? margin = null) => new()
    {
        Child = content,
        Padding = new Thickness(18),
        Margin = margin ?? new Thickness(0),
        CornerRadius = new CornerRadius(12),
        BorderBrush = Brush(ProfessionalPalette.CardBorder),
        BorderThickness = new Thickness(1),
        Background = new SolidColorBrush(Color.Parse("#0DFFFFFF"))
    };

    private static IBrush Brush(Color color) => new SolidColorBrush(color);

    private static void ApplyPrimaryButtonColors(Button button)
    {
        button.Background = CallToAction;
        button.Foreground = Brush(ProfessionalPalette.CallToActionText);
        button.BorderBrush = CallToActionBorder;
        button.PointerEntered += (_, _) => button.Background = CallToActionHover;
        button.PointerExited += (_, _) => button.Background = CallToAction;
    }
}
