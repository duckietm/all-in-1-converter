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
using Avalonia.Threading;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using Habbo_Downloader.App.Professional.ViewModels;
using Habbo_Downloader.App.Workspaces;
using Habbo_Downloader.Tools;
using static Habbo_Downloader.App.Professional.ProfessionalTheme;

namespace Habbo_Downloader.App.Professional.Views;

public sealed class ProfessionalWindow : Window
{
    private readonly TaskCompletionSource _closed = new();
    public Task ClosedTask => _closed.Task;
    private static readonly HashSet<string> WorkspaceOperationIds =
    [
        "habbo.furnidata",
        "nitro.furniture",
        "nitro.clothes",
        "tools.decompile-nitro",
        "tools.compile-nitro",
        "tools.decompile-hab",
        "tools.compile-hab",
        "tools.decompile-swf",
        "tools.swf-furniture-nitro",
        "tools.swf-furniture-hab",
        "tools.nitro-furniture-hab",
        "tools.swf-clothes-nitro",
        "tools.swf-clothes-hab",
        "tools.nitro-clothes-hab",
        "tools.swf-pets-nitro",
        "tools.swf-pets-hab",
        "tools.nitro-pets-hab",
        "tools.swf-effects-nitro",
        "tools.swf-effects-hab",
        "tools.nitro-effects-hab",
        "database.offer-id",
        "database.item-settings",
        "database.sprite-id"
    ];

    /// <summary>Sidebar label, icon and colour of each module.</summary>
    private static readonly (OperationCategory Category, string Label, string Caption, string Icon, string Color)[] Modules =
    [
        (OperationCategory.HabboOriginal, "Habbo Original", "Official downloads", Icons.Download, "#4F5FE6"),
        (OperationCategory.NitroCustom, "Nitro Custom", "Custom imports", Icons.Star, "#8B5CF6"),
        (OperationCategory.HotelTools, "Hotel Tools", "Conversion tools", Icons.Wrench, "#0E9F9E"),
        (OperationCategory.Database, "Database", "Maintenance jobs", Icons.Database, "#E08A0B")
    ];

    private readonly ProfessionalShellViewModel _viewModel = new();
    private readonly AssetWorkspaceViewModel _workspaceViewModel = new(
        new WorkspaceSettingsStore(Path.Combine(Environment.CurrentDirectory, "config.ini")));

    // Shell
    private readonly ContentControl _pageHost = new();
    private readonly ScrollViewer _pageScroll = new() { HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
    private readonly StackPanel _content = new() { Spacing = 18, Margin = new Thickness(32, 0, 32, 18) };
    private readonly TextBlock _title = Text("", 26, weight: FontWeight.SemiBold);
    private readonly TextBlock _subtitle = Text("", 13.5, "Pro.Muted");
    private readonly Border _statusPill = new() { CornerRadius = new CornerRadius(14), Padding = new Thickness(11, 5), VerticalAlignment = VerticalAlignment.Center };
    private readonly Avalonia.Controls.Shapes.Ellipse _statusDot = Dot();
    private readonly TextBlock _statusPillText = new() { FontSize = 12, FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _status = Text("", 12, "Pro.Muted");
    private readonly TextBlock _workspaceFooter = Text("", 12, "Pro.Muted");
    private readonly List<Button> _navigationButtons = [];
    private readonly Dictionary<OperationCategory, Button> _categoryNavigation = [];
    private Button? _dashboardNavigation;
    private Button? _workspaceNavigation;
    private Button? _settingsNavigation;
    private readonly SettingsPage _settings;
    private HabboHotel? _hotel;
    private bool _hotelKnown;
    private readonly ContentControl _footerFlag = new() { VerticalAlignment = VerticalAlignment.Center };
    private Button? _switchButton;

    // Operation page
    private readonly Grid _operationPage;
    private readonly StackPanel _operationList = new() { Spacing = 2, Margin = new Thickness(0, 0, 12, 0) };
    private readonly TextBlock _operationCount = Text("", 12, "Pro.Muted");
    private readonly Dictionary<OperationDefinition, Button> _operationRows = [];
    private readonly TextBlock _activityHeading = Text("", 20, weight: FontWeight.SemiBold);
    private readonly TextBlock _activityDescription = Text("", 13, "Pro.Muted");
    private readonly WrapPanel _activityChips = new() { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
    private readonly TextBlock _workspaceNote = Text("", 12, "Pro.AccentText");
    private readonly ProgressBar _progress = new() { IsIndeterminate = true, Height = 3, MinHeight = 3, IsVisible = false, Foreground = Solid(Brand) };
    private readonly TextBox _log = new()
    {
        AcceptsReturn = true,
        IsReadOnly = true,
        TextWrapping = TextWrapping.NoWrap,
        FontFamily = new FontFamily("Cascadia Mono, Consolas, DejaVu Sans Mono, monospace"),
        FontSize = 12,
        Padding = new Thickness(14, 12),
        CornerRadius = new CornerRadius(8),
        PlaceholderText = "Output appears here when the operation runs."
    };
    private readonly Grid _inputRow = new() { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8, Margin = new Thickness(0, 10, 0, 0) };
    private readonly TextBox _input = new() { PlaceholderText = "Answer the prompt and press Enter (empty = default)" };
    private readonly Button _send = new() { Content = "Send", Padding = new Thickness(18, 8), Classes = { "secondary" } };
    private readonly Button _runButton = new() { Name = "RunButton", Padding = new Thickness(18, 9), Classes = { "primary" } };
    private readonly Button _doneButton = new() { Name = "DoneButton", Content = "Done", Padding = new Thickness(18, 9), IsVisible = false, Classes = { "secondary" } };

    // Asset workspace page
    private readonly Control _workspacePanel;
    private readonly TextBox _workspacePath = new() { PlaceholderText = @"E:\Users\you\Nitro-Files\nitro-assets" };
    private readonly TextBlock _workspaceStatus = Text("", 14, "Pro.AccentText", FontWeight.SemiBold);
    private readonly WrapPanel _workspaceFolders = new() { Orientation = Orientation.Horizontal };
    private readonly Button _openWorkspace = new() { Content = "Open workspace folder", Padding = new Thickness(16, 8), Classes = { "secondary" } };

    public ProfessionalWindow()
    {
        ProfessionalTheme.Install(Application.Current!);
        Title = "All-in-1 Converter";
        Width = 1380;
        Height = 860;
        MinWidth = 1040;
        MinHeight = 700;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Default;
        this.Bind(BackgroundProperty, "Pro.Surface");
        DataContext = _viewModel;
        _openWorkspace.Click += (_, _) => OpenWorkspaceFolder();

        _settings = new SettingsPage(message =>
        {
            _status.Text = message;
            RefreshHotel();
        });
        RefreshHotel();
        _operationPage = BuildOperationPage();
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

    // ---------------------------------------------------------------- shell

    private Control BuildLayout()
    {
        var root = new Grid { ColumnDefinitions = new ColumnDefinitions("236,*") };
        root.Children.Add(BuildSidebar());

        var main = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };
        Grid.SetColumn(main, 1);

        var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(32, 26, 32, 20) };
        var heading = new StackPanel { Spacing = 4 };
        heading.Children.Add(_title);
        heading.Children.Add(_subtitle);
        header.Children.Add(heading);
        var pill = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        pill.Children.Add(_statusDot);
        pill.Children.Add(_statusPillText);
        _statusPill.Child = pill;
        Grid.SetColumn(_statusPill, 1);
        header.Children.Add(_statusPill);
        main.Children.Add(header);

        _pageScroll.Content = _content;
        Grid.SetRow(_pageHost, 1);
        main.Children.Add(_pageHost);

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(32, 9) };
        _status.TextTrimming = TextTrimming.CharacterEllipsis;
        _status.TextWrapping = TextWrapping.NoWrap;
        footer.Children.Add(_status);
        _workspaceFooter.TextWrapping = TextWrapping.NoWrap;
        _workspaceFooter.VerticalAlignment = VerticalAlignment.Center;
        var footerRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 7 };
        footerRight.Children.Add(_footerFlag);
        footerRight.Children.Add(_workspaceFooter);
        Grid.SetColumn(footerRight, 1);
        footer.Children.Add(footerRight);
        var footerBar = new Border { BorderThickness = new Thickness(0, 1, 0, 0), Child = footer }
            .Bind(Border.BorderBrushProperty, "Pro.CardBorder")
            .Bind(Border.BackgroundProperty, "Pro.Card");
        Grid.SetRow(footerBar, 2);
        main.Children.Add(footerBar);

        root.Children.Add(main);
        return root;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto") };

        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 11, Margin = new Thickness(6, 0, 0, 26) };
        var logoIcon = Icon(Icons.Cube, 19);
        logoIcon.Foreground = Brushes.White;
        brand.Children.Add(new Border
        {
            Width = 36,
            Height = 36,
            CornerRadius = new CornerRadius(10),
            Background = new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = { new GradientStop(Color.Parse("#6D7BFF"), 0), new GradientStop(Color.Parse("#4352D4"), 1) }
            },
            Child = logoIcon
        });
        var brandText = new StackPanel { Spacing = 1, VerticalAlignment = VerticalAlignment.Center };
        brandText.Children.Add(new TextBlock { Text = "All-in-1 Converter", Foreground = Brushes.White, FontSize = 15, FontWeight = FontWeight.SemiBold });
        brandText.Children.Add(new TextBlock { Text = "Habbo asset workstation", Foreground = Solid(SidebarMuted), FontSize = 11.5 });
        brand.Children.Add(brandText);
        sidebar.Children.Add(brand);

        var nav = new StackPanel { Spacing = 3 };
        _dashboardNavigation = NavButton("Dashboard", Icons.Home, ShowDashboard);
        _workspaceNavigation = NavButton("Asset Workspace", Icons.Folder, ShowAssetWorkspace);
        nav.Children.Add(_dashboardNavigation);
        nav.Children.Add(_workspaceNavigation);
        nav.Children.Add(SidebarLabel("MODULES"));
        foreach (var module in Modules)
            nav.Children.Add(NavButton(module.Label, module.Icon, () => ShowCategory(module.Category), module.Category));
        nav.Children.Add(SidebarLabel("GENERAL"));
        _settingsNavigation = NavButton("Settings", Icons.Cog, ShowSettings);
        nav.Children.Add(_settingsNavigation);
        nav.Children.Add(NavButton("About", Icons.Info, () => ShowCategory(OperationCategory.General), OperationCategory.General));
        Grid.SetRow(nav, 1);
        sidebar.Children.Add(nav);

        var footer = new StackPanel { Spacing = 10 };
        footer.Children.Add(new Border { Height = 1, Background = Solid(SidebarHover), Margin = new Thickness(4, 0, 4, 6) });
        _switchButton = NavButton("Switch to classic CLI", Icons.Swap, () => { }, track: false);
        _switchButton.Click += async (_, _) => await SwitchInterfaceAsync();
        footer.Children.Add(_switchButton);
        Grid.SetRow(footer, 2);
        sidebar.Children.Add(footer);

        return new Border { Background = Solid(Sidebar), Padding = new Thickness(14, 22), Child = sidebar };
    }

    private static TextBlock SidebarLabel(string text) => new()
    {
        Text = text,
        Foreground = Solid(SidebarMuted),
        FontSize = 10.5,
        FontWeight = FontWeight.SemiBold,
        LetterSpacing = 1,
        Margin = new Thickness(12, 18, 0, 6)
    };

    private Button NavButton(string label, string icon, Action action, OperationCategory? category = null, bool track = true)
    {
        var content = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
        content.Children.Add(Icon(icon, 17));
        content.Children.Add(new TextBlock { Text = label, FontSize = 13.5, VerticalAlignment = VerticalAlignment.Center });
        var button = new Button
        {
            Content = content,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            Padding = new Thickness(12, 9),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            Classes = { "nav" }
        };
        if (!track) return button;

        button.Click += async (_, _) =>
        {
            if (_viewModel.IsRunning) return;
            if (button == _settingsNavigation && OnSettingsPage) return;
            if (!await LeaveSettingsAsync()) return;
            ActivateNavigation(button);
            action();
        };
        _navigationButtons.Add(button);
        if (category is not null) _categoryNavigation[category.Value] = button;
        return button;
    }

    private void ActivateNavigation(Button? selected)
    {
        foreach (Button button in _navigationButtons)
        {
            bool isSelected = button == selected;
            button.Classes.Set("selected", isSelected);
            if (button.Content is StackPanel { Children: [PathIcon icon, TextBlock text] })
            {
                if (isSelected) icon.Foreground = Solid(SidebarAccent);
                else icon.ClearValue(ForegroundProperty);
                text.FontWeight = isSelected ? FontWeight.SemiBold : FontWeight.Normal;
            }
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

    private void ShowScrollPage(params Control[] sections)
    {
        _content.Children.Clear();
        foreach (Control section in sections) _content.Children.Add(section);
        _pageHost.Content = _pageScroll;
        _pageScroll.Offset = default;
    }

    // ---------------------------------------------------------------- settings

    private bool OnSettingsPage => _pageHost.Content == _pageScroll && _content.Children.Contains(_settings.View);

    private void ShowSettings()
    {
        ActivateNavigation(_settingsNavigation);
        _viewModel.ShowSettings();
        _settings.Reload();
        ShowScrollPage(_settings.View);
        RefreshHeader();
    }

    /// <summary>Asks before unsaved config.ini edits are dropped. True when the page may be left.</summary>
    private async Task<bool> LeaveSettingsAsync()
    {
        if (!OnSettingsPage || !_settings.IsDirty) return true;
        bool discard = await ConfirmAsync("Unsaved changes", "Unsaved changes",
            "Your config.ini changes are not saved yet.",
            "Discard them, or cancel and press Save changes.",
            "Discard changes", Icons.Alert, "Pro.WarningText", "Pro.WarningBack");
        if (discard) _settings.Reload();
        return discard;
    }

    private void RefreshHotel()
    {
        try
        {
            ConfigIniFile file = ConfigIniFile.Load();
            _hotel = HabboHotels.Detect(file.Get);
            _hotelKnown = true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _hotel = null;
            _hotelKnown = false;
        }
        RefreshFooter();
    }

    // ---------------------------------------------------------------- dashboard

    private void ShowDashboard()
    {
        ActivateNavigation(_dashboardNavigation);
        _viewModel.ShowDashboard();
        var bottom = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 16 };
        bottom.Children.Add(BuildHotelSummary());
        Control workspace = BuildWorkspaceSummary();
        Grid.SetColumn(workspace, 1);
        bottom.Children.Add(workspace);
        ShowScrollPage(BuildModuleTiles(), BuildDashboardColumns(), bottom);
        RefreshHeader();
    }

    private Control BuildModuleTiles()
    {
        var grid = new UniformGrid { Columns = 4, Rows = 1, Margin = new Thickness(-7, 0) };
        foreach (var module in Modules)
        {
            Color color = Color.Parse(module.Color);
            var icon = Icon(module.Icon, 19);
            icon.Foreground = Solid(color);
            var panel = new StackPanel { Spacing = 3 };
            panel.Children.Add(new Border
            {
                Width = 38,
                Height = 38,
                CornerRadius = new CornerRadius(10),
                Background = Solid(Color.FromArgb(0x26, color.R, color.G, color.B)),
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 0, 0, 12),
                Child = icon
            });
            panel.Children.Add(Text(OperationCatalog.ForCategory(module.Category).Count.ToString(), 28, weight: FontWeight.Bold));
            panel.Children.Add(Text(module.Label, 14, weight: FontWeight.SemiBold));
            panel.Children.Add(Text(module.Caption, 12, "Pro.Muted"));
            var tile = new Button
            {
                Content = panel,
                Padding = new Thickness(20, 18),
                Margin = new Thickness(7, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                CornerRadius = new CornerRadius(12),
                BorderThickness = new Thickness(1),
                Classes = { "tile" }
            };
            OperationCategory category = module.Category;
            tile.Click += (_, _) =>
            {
                if (_viewModel.IsRunning) return;
                ShowCategory(category);
            };
            grid.Children.Add(tile);
        }
        return grid;
    }

    private Control BuildDashboardColumns()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("3*,2*"), ColumnSpacing = 16 };

        var quick = new StackPanel { Spacing = 2 };
        quick.Children.Add(CardHeading("Quick actions", "Frequently used operations"));
        foreach (string id in new[] { "habbo.all", "tools.swf-furniture-hab", "tools.generate-sql", "database.info" })
        {
            OperationDefinition operation = OperationCatalog.Get(id);
            Button row = OperationRow(operation, showChevron: true);
            row.Click += (_, _) => OpenOperation(operation);
            quick.Children.Add(row);
        }
        grid.Children.Add(Card(quick, new Thickness(12)));

        var recent = new StackPanel { Spacing = 2 };
        recent.Children.Add(CardHeading("Recent activity", "Runs in this session"));
        if (_viewModel.RecentRuns.Count == 0)
        {
            var empty = new StackPanel { Spacing = 8, Margin = new Thickness(12, 18), HorizontalAlignment = HorizontalAlignment.Center };
            var historyIcon = Icon(Icons.History, 26).Bind(PathIcon.ForegroundProperty, "Pro.Subtle");
            empty.Children.Add(historyIcon);
            empty.Children.Add(Text("No operations run yet.", 12.5, "Pro.Muted"));
            recent.Children.Add(empty);
        }
        foreach (RunHistoryItem run in _viewModel.RecentRuns)
        {
            var line = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 10, Margin = new Thickness(12, 7) };
            var dot = Dot().Bind(Avalonia.Controls.Shapes.Shape.FillProperty, run.Succeeded ? "Pro.SuccessText" : "Pro.ErrorText");
            line.Children.Add(dot);
            var title = Text(run.Title, 13);
            title.TextTrimming = TextTrimming.CharacterEllipsis;
            title.TextWrapping = TextWrapping.NoWrap;
            Grid.SetColumn(title, 1);
            line.Children.Add(title);
            var time = Text(run.FinishedAt.ToLocalTime().ToString("HH:mm"), 12, "Pro.Muted");
            Grid.SetColumn(time, 2);
            line.Children.Add(time);
            recent.Children.Add(line);
        }
        var recentCard = Card(recent, new Thickness(12));
        Grid.SetColumn(recentCard, 1);
        grid.Children.Add(recentCard);
        return grid;
    }

    /// <summary>The Habbo hotel config.ini points at, with its flag and language.</summary>
    private Control BuildHotelSummary()
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 16 };
        Control flag = Flags.For(_hotel, 48);
        flag.VerticalAlignment = VerticalAlignment.Center;
        grid.Children.Add(flag);
        var text = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(Text(_hotel?.Name ?? "Custom hotel URLs", 14.5, weight: FontWeight.SemiBold));
        if (_hotel is not null) titleRow.Children.Add(Chip(_hotel.Language.ToUpperInvariant(), "Pro.AccentSoft", "Pro.AccentText"));
        text.Children.Add(titleRow);
        text.Children.Add(Text(_hotel is not null
            ? $"{_hotel.Region}. Habbo Original downloads gamedata from this hotel."
            : _hotelKnown ? "The gamedata URLs in config.ini point at no single Habbo hotel." : "config.ini could not be read.", 12.5, "Pro.Muted"));
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        var change = new Button { Content = "Change", Padding = new Thickness(16, 8), VerticalAlignment = VerticalAlignment.Center, Classes = { "secondary" } };
        change.Click += (_, _) =>
        {
            if (_viewModel.IsRunning) return;
            ShowSettings();
        };
        Grid.SetColumn(change, 2);
        grid.Children.Add(change);
        return Card(grid, new Thickness(18));
    }

    private Control BuildWorkspaceSummary()
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 16 };
        var icon = Icon(Icons.Folder, 20).Bind(PathIcon.ForegroundProperty, "Pro.AccentText");
        grid.Children.Add(new Border { Width = 42, Height = 42, CornerRadius = new CornerRadius(10), Child = icon, VerticalAlignment = VerticalAlignment.Center }
            .Bind(Border.BackgroundProperty, "Pro.AccentSoft"));
        var text = new StackPanel { Spacing = 3, VerticalAlignment = VerticalAlignment.Center };
        text.Children.Add(Text("Asset workspace", 14.5, weight: FontWeight.SemiBold));
        text.Children.Add(Text(paths is null
            ? "Not connected. Operations write to their own output folders next to the converter."
            : $"Connected to {paths.Root}. Compatible operations read and write there, with backups.", 12.5, "Pro.Muted"));
        Grid.SetColumn(text, 1);
        grid.Children.Add(text);
        var configure = new Button
        {
            Content = paths is null ? "Connect folder" : "Manage",
            Padding = new Thickness(16, 8),
            VerticalAlignment = VerticalAlignment.Center,
            Classes = { "secondary" }
        };
        configure.Click += (_, _) =>
        {
            if (_viewModel.IsRunning) return;
            ShowAssetWorkspace();
        };
        Grid.SetColumn(configure, 2);
        grid.Children.Add(configure);
        return Card(grid, new Thickness(18));
    }

    private static Control CardHeading(string title, string subtitle)
    {
        var panel = new StackPanel { Spacing = 2, Margin = new Thickness(12, 8, 12, 10) };
        panel.Children.Add(Text(title, 15, weight: FontWeight.SemiBold));
        panel.Children.Add(Text(subtitle, 12, "Pro.Muted"));
        return panel;
    }

    // ---------------------------------------------------------------- asset workspace

    private async void ShowAssetWorkspace()
    {
        ActivateNavigation(_workspaceNavigation);
        _viewModel.ShowAssetWorkspace();
        _workspacePath.Text = _workspaceViewModel.RootPath;
        ShowScrollPage(_workspacePanel);
        RefreshHeader();
        await _workspaceViewModel.LoadAsync();
        RefreshWorkspacePage();
    }

    private Control BuildWorkspacePanel()
    {
        var section = new StackPanel { Spacing = 16 };

        var pathPanel = new StackPanel { Spacing = 12 };
        pathPanel.Children.Add(Text("Nitro assets folder", 15, weight: FontWeight.SemiBold));
        pathPanel.Children.Add(Text("Choose the nitro-assets root. Known folders are detected automatically.", 12.5, "Pro.Muted"));
        var pathRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"), ColumnSpacing = 8 };
        pathRow.Children.Add(_workspacePath);
        var browse = new Button { Content = "Browse…", Padding = new Thickness(16, 8), Classes = { "secondary" } };
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
        var save = new Button { Content = "Save and verify", Padding = new Thickness(16, 8), Classes = { "primary" } };
        save.Click += async (_, _) =>
        {
            _workspaceViewModel.RootPath = _workspacePath.Text ?? string.Empty;
            await _workspaceViewModel.SaveAndRefreshAsync();
            RefreshWorkspacePage();
        };
        Grid.SetColumn(save, 2);
        pathRow.Children.Add(save);
        pathPanel.Children.Add(pathRow);
        _workspaceStatus.Text = _workspaceViewModel.StatusText;
        pathPanel.Children.Add(_workspaceStatus);
        section.Children.Add(Card(pathPanel));

        RefreshWorkspaceFolderCards();
        section.Children.Add(_workspaceFolders);

        var safety = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 16 };
        var safetyText = new StackPanel { Spacing = 4 };
        safetyText.Children.Add(Text("Safe file interaction", 14.5, weight: FontWeight.SemiBold));
        safetyText.Children.Add(Text("Compatible operations use these folders directly. Existing files are backed up under backups/<date-time> before replacement; paths outside this workspace are rejected.", 12.5, "Pro.Muted"));
        safety.Children.Add(safetyText);
        _openWorkspace.VerticalAlignment = VerticalAlignment.Center;
        _openWorkspace.IsEnabled = _workspaceViewModel.Paths is not null;
        Grid.SetColumn(_openWorkspace, 1);
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
        RefreshFooter();
    }

    private void RefreshWorkspaceFolderCards()
    {
        _workspaceFolders.Children.Clear();
        IReadOnlyList<AssetWorkspaceFolder> folders = _workspaceViewModel.Folders;
        if (folders.Count == 0)
        {
            _workspaceFolders.Children.Add(Card(Text("No workspace verified yet.", 13, "Pro.Muted"), new Thickness(18, 14)));
            return;
        }

        foreach (AssetWorkspaceFolder folder in folders)
        {
            var content = new StackPanel { Spacing = 6 };
            var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            titleRow.Children.Add(Text(folder.Name, 14.5, weight: FontWeight.SemiBold));
            titleRow.Children.Add(folder.Exists
                ? Chip($"{folder.FileCount:N0} FILES", "Pro.SuccessBack", "Pro.SuccessText")
                : Chip("MISSING", "Pro.WarningBack", "Pro.WarningText"));
            content.Children.Add(titleRow);
            content.Children.Add(Text(folder.Path, 11.5, "Pro.Muted"));
            Border card = Card(content, new Thickness(16));
            card.Width = 300;
            card.Margin = new Thickness(0, 0, 12, 12);
            _workspaceFolders.Children.Add(card);
        }
    }

    private void OpenWorkspaceFolder()
    {
        string? path = _workspaceViewModel.Paths?.Root;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    // ---------------------------------------------------------------- operations

    private void ShowCategory(OperationCategory category)
    {
        if (_categoryNavigation.TryGetValue(category, out Button? navigation))
            ActivateNavigation(navigation);
        _viewModel.ShowCategory(category);
        _viewModel.ClearLog();
        BuildOperationList();
        _pageHost.Content = _operationPage;
        RefreshHeader();
        RefreshActivity();
    }

    private void OpenOperation(OperationDefinition operation)
    {
        if (_viewModel.IsRunning) return;
        ShowCategory(operation.Category);
        _viewModel.SelectOperation(operation);
        RefreshActivity();
    }

    private Grid BuildOperationPage()
    {
        var page = new Grid { ColumnSpacing = 16, Margin = new Thickness(32, 0, 32, 18) };
        page.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star) { MinWidth = 260, MaxWidth = 360 });
        page.ColumnDefinitions.Add(new ColumnDefinition(2, GridUnitType.Star));

        // Left: the operations of the module.
        var listPanel = new Grid { RowDefinitions = new RowDefinitions("Auto,*") };
        _operationCount.Margin = new Thickness(12, 6, 12, 6);
        listPanel.Children.Add(_operationCount);
        var listScroll = new ScrollViewer { Content = _operationList, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetRow(listScroll, 1);
        listPanel.Children.Add(listScroll);
        page.Children.Add(Card(listPanel, new Thickness(8)));

        // Right: the selected operation, its output and its answer box.
        var detail = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,Auto,*,Auto") };
        var header = new StackPanel { Spacing = 6 };
        var titleRow = new WrapPanel { Orientation = Orientation.Horizontal };
        _activityHeading.Margin = new Thickness(0, 0, 10, 0);
        titleRow.Children.Add(_activityHeading);
        titleRow.Children.Add(_activityChips);
        header.Children.Add(titleRow);
        header.Children.Add(_activityDescription);
        header.Children.Add(_workspaceNote);

        var runContent = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        runContent.Children.Add(Icon(Icons.Play, 14));
        runContent.Children.Add(new TextBlock { Text = "Run operation", FontWeight = FontWeight.SemiBold, VerticalAlignment = VerticalAlignment.Center });
        _runButton.Content = runContent;
        _runButton.Click += async (_, _) => await RunSelectedAsync();
        // After a run: clear the log, ready for the next one.
        _doneButton.Click += (_, _) =>
        {
            _viewModel.ClearLog();
            RefreshActivity();
        };
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Thickness(0, 10, 0, 0) };
        actions.Children.Add(_runButton);
        actions.Children.Add(_doneButton);
        header.Children.Add(actions);
        detail.Children.Add(header);

        _progress.Margin = new Thickness(0, 16, 0, 0);
        Grid.SetRow(_progress, 1);
        detail.Children.Add(_progress);

        var outputLabel = Text("OUTPUT", 10.5, "Pro.Subtle", FontWeight.SemiBold);
        outputLabel.LetterSpacing = 1;
        outputLabel.Margin = new Thickness(0, 18, 0, 8);
        Grid.SetRow(outputLabel, 2);
        detail.Children.Add(outputLabel);

        ConsoleStyle(_log);
        Grid.SetRow(_log, 3);
        detail.Children.Add(_log);

        _input.KeyDown += (_, e) =>
        {
            if (e.Key != Key.Enter) return;
            SubmitInput();
            e.Handled = true;
        };
        _inputRow.Children.Add(_input);
        _send.Click += (_, _) => SubmitInput();
        Grid.SetColumn(_send, 1);
        _inputRow.Children.Add(_send);
        Grid.SetRow(_inputRow, 4);
        detail.Children.Add(_inputRow);

        var detailCard = Card(detail, new Thickness(24, 22));
        Grid.SetColumn(detailCard, 1);
        page.Children.Add(detailCard);
        return page;
    }

    private void BuildOperationList()
    {
        _operationList.Children.Clear();
        _operationRows.Clear();
        _operationCount.Text = $"{_viewModel.VisibleOperations.Count} operations";
        string? group = null;
        foreach (OperationDefinition operation in _viewModel.VisibleOperations)
        {
            string? next = GroupOf(operation);
            if (next is not null && next != group)
            {
                var label = Text(next.ToUpperInvariant(), 10.5, "Pro.Subtle", FontWeight.SemiBold);
                label.LetterSpacing = 1;
                label.Margin = new Thickness(12, group is null ? 4 : 14, 12, 4);
                _operationList.Children.Add(label);
            }
            group = next;

            Button row = OperationRow(operation);
            row.Click += (_, _) =>
            {
                if (_viewModel.IsRunning) return;
                _viewModel.ClearLog();
                _viewModel.SelectOperation(operation);
                RefreshActivity();
            };
            _operationRows[operation] = row;
            _operationList.Children.Add(row);
        }
    }

    /// <summary>Sections of the long Hotel Tools list, in catalogue order.</summary>
    private static string? GroupOf(OperationDefinition operation)
    {
        if (operation.Category != OperationCategory.HotelTools) return null;
        string id = operation.Id;
        if (id.Contains("merge") || id.EndsWith("generate-sql")) return "Gamedata";
        if (id == "tools.decompile-swf") return "SWF";
        if (id.Contains("compile")) return "Bundles";
        if (id.Contains("furniture")) return "Furniture";
        if (id.Contains("clothes")) return "Clothes";
        if (id.Contains("pets")) return "Pets";
        if (id.Contains("effects")) return "Effects";
        return null;
    }

    private static Button OperationRow(OperationDefinition operation, bool showChevron = false)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 10 };
        var text = new StackPanel { Spacing = 2 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(Text(operation.Title, 13.5, weight: FontWeight.SemiBold));
        if (operation.IsDestructive) titleRow.Children.Add(Chip("CAUTION", "Pro.WarningBack", "Pro.WarningText"));
        text.Children.Add(titleRow);
        var description = Text(operation.Description, 12, "Pro.Muted");
        description.MaxLines = 2;
        description.TextTrimming = TextTrimming.CharacterEllipsis;
        text.Children.Add(description);
        grid.Children.Add(text);
        if (showChevron)
        {
            var chevron = Icon(Icons.ChevronRight, 16).Bind(PathIcon.ForegroundProperty, "Pro.Subtle");
            chevron.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(chevron, 1);
            grid.Children.Add(chevron);
        }
        return new Button
        {
            Content = grid,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(12, 9),
            BorderThickness = new Thickness(0),
            CornerRadius = new CornerRadius(8),
            Classes = { "row" }
        };
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

    // ---------------------------------------------------------------- dialogs

    private async Task<bool> ConfirmWorkspaceAccessAsync(OperationDefinition operation)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is null) return true;
        return await ConfirmAsync("Review workspace access", operation.Title,
            $"This operation will interact with the configured Nitro workspace:\n{paths.Root}",
            "Existing files written by compatible operations are backed up automatically before replacement.",
            "Continue with workspace", Icons.Folder, "Pro.AccentText", "Pro.AccentSoft");
    }

    private Task<bool> ConfirmDestructiveAsync(string title) =>
        ConfirmAsync("Confirm operation", "Database change",
            $"{title} can modify your database.",
            "Confirm only if your configuration and backups are ready.",
            "Confirm and run", Icons.Alert, "Pro.WarningText", "Pro.WarningBack");

    private async Task<bool> ConfirmAsync(string windowTitle, string heading, string message, string note, string confirmText,
        string icon, string iconColor, string iconBack)
    {
        var dialog = new Window
        {
            Title = windowTitle,
            Width = 520,
            SizeToContent = SizeToContent.Height,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        dialog.Bind(BackgroundProperty, "Pro.Card");
        var result = false;
        var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 16, Margin = new Thickness(26, 24) };
        var badge = new Border { Width = 40, Height = 40, CornerRadius = new CornerRadius(20), VerticalAlignment = VerticalAlignment.Top, Child = Icon(icon, 20).Bind(PathIcon.ForegroundProperty, iconColor) }
            .Bind(Border.BackgroundProperty, iconBack);
        layout.Children.Add(badge);
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(Text(heading, 18, weight: FontWeight.SemiBold));
        panel.Children.Add(Text(message, 13));
        panel.Children.Add(Text(note, 12.5, "Pro.Muted"));
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Margin = new Thickness(0, 14, 0, 0) };
        var cancel = new Button { Content = "Cancel", Padding = new Thickness(16, 8), Classes = { "secondary" } };
        cancel.Click += (_, _) => dialog.Close();
        var confirm = new Button { Content = confirmText, Padding = new Thickness(16, 8), Classes = { "primary" } };
        confirm.Click += (_, _) => { result = true; dialog.Close(); };
        buttons.Children.Add(cancel);
        buttons.Children.Add(confirm);
        panel.Children.Add(buttons);
        Grid.SetColumn(panel, 1);
        layout.Children.Add(panel);
        dialog.Content = layout;
        await dialog.ShowDialog(this);
        return result;
    }

    // ---------------------------------------------------------------- state

    private void SubmitInput()
    {
        _viewModel.InputText = _input.Text ?? string.Empty;
        _viewModel.SubmitInput();
        _input.Text = string.Empty;
    }

    private void ViewModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProfessionalShellViewModel.LogText))
        {
            _log.Text = _viewModel.LogText;
            _log.CaretIndex = _log.Text?.Length ?? 0;
            return;
        }

        RefreshHeader();
        RefreshActivity();
        if (e.PropertyName == nameof(ProfessionalShellViewModel.IsRunning) && _viewModel.IsRunning) _input.Focus();
    }

    private void RefreshHeader()
    {
        _title.Text = _viewModel.PageTitle;
        _subtitle.Text = _viewModel.PageSubtitle;
        _status.Text = _viewModel.StatusText;

        (string text, string back, string fore) = _viewModel.IsRunning ? ("Running", "Pro.InfoBack", "Pro.InfoText")
            : !_viewModel.HasFinishedRun ? ("Ready", "Pro.Hover", "Pro.Muted")
            : _viewModel.LastRunSucceeded ? ("Completed", "Pro.SuccessBack", "Pro.SuccessText")
            : ("Failed", "Pro.ErrorBack", "Pro.ErrorText");
        _statusPillText.Text = text;
        _statusPill.Bind(Border.BackgroundProperty, back);
        _statusPillText.Bind(TextBlock.ForegroundProperty, fore);
        _statusDot.Bind(Avalonia.Controls.Shapes.Shape.FillProperty, fore);
        RefreshFooter();
    }

    private void RefreshFooter()
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        string workspace = paths is null ? "No asset workspace" : $"Workspace: {paths.Root}";
        string hotel = _hotel is not null ? $"{_hotel.Name} ({_hotel.Language})" : "Custom hotel URLs";
        _workspaceFooter.Text = _hotelKnown ? $"{hotel}   •   {workspace}" : workspace;
        _footerFlag.Content = _hotelKnown ? Flags.For(_hotel, 18) : null;
    }

    private void RefreshActivity()
    {
        OperationDefinition? operation = _viewModel.SelectedOperation;
        bool running = _viewModel.IsRunning;
        _activityHeading.Text = operation?.Title ?? "Select an operation";
        _activityDescription.Text = operation?.Description ?? "Choose an operation on the left to run it.";
        _workspaceNote.Text = operation is null ? string.Empty : WorkspaceTargetSummary(operation);
        _workspaceNote.IsVisible = _workspaceNote.Text.Length > 0;

        _activityChips.Children.Clear();
        if (operation?.IsDestructive == true) _activityChips.Children.Add(Chip("CAUTION", "Pro.WarningBack", "Pro.WarningText"));
        if (operation?.RequiresInput == true) _activityChips.Children.Add(Chip("ASKS QUESTIONS", "Pro.InfoBack", "Pro.InfoText"));

        _runButton.IsEnabled = _viewModel.CanRun;
        _doneButton.IsVisible = _viewModel.HasFinishedRun && !running;
        _progress.IsVisible = running;
        _inputRow.IsVisible = operation?.RequiresInput == true;
        _input.IsEnabled = running;
        _send.IsEnabled = running;

        foreach (var (rowOperation, row) in _operationRows)
        {
            row.Classes.Set("selected", rowOperation == operation);
            row.IsEnabled = !running;
        }
        foreach (Button button in _navigationButtons) button.IsEnabled = !running;
        if (_switchButton is not null) _switchButton.IsEnabled = !running;
    }

    private static string WorkspaceTargetSummary(OperationDefinition operation)
    {
        AssetWorkspacePaths? paths = AssetWorkspaceRuntime.Router.Paths;
        if (paths is null || !WorkspaceOperationIds.Contains(operation.Id)) return string.Empty;
        return $"Uses the asset workspace: {paths.Root}";
    }
}
