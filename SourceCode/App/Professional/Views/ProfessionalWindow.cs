using System.ComponentModel;
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

namespace Habbo_Downloader.App.Professional.Views;

public sealed class ProfessionalWindow : Window
{
    private readonly TaskCompletionSource _closed = new();
    public Task ClosedTask => _closed.Task;
    private static readonly IBrush Navy = new SolidColorBrush(Color.Parse("#101B34"));
    private static readonly IBrush NavyHover = new SolidColorBrush(Color.Parse("#1B2A4A"));
    private static readonly IBrush Accent = new SolidColorBrush(Color.Parse("#536DFE"));
    private static readonly IBrush Muted = new SolidColorBrush(Color.Parse("#718096"));
    private static readonly IBrush Success = new SolidColorBrush(Color.Parse("#19A974"));
    private readonly ProfessionalShellViewModel _viewModel = new();
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
    private readonly Border _activityPanel;

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

        _activityPanel = BuildActivityPanel();
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
            Background = new SolidColorBrush(Color.Parse("#E8ECFF")),
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
        brand.Children.Add(new TextBlock { Text = "ALL-IN-1", Foreground = Brushes.White, FontSize = 22, FontWeight = FontWeight.Bold });
        brand.Children.Add(new TextBlock { Text = "ASSET WORKSTATION", Foreground = new SolidColorBrush(Color.Parse("#9EACCB")), FontSize = 11 });
        sidebar.Children.Add(brand);

        var nav = new StackPanel { Spacing = 7 };
        nav.Children.Add(NavButton("⌂  Dashboard", ShowDashboard));
        nav.Children.Add(NavButton("↓  Habbo Original", () => ShowCategory(OperationCategory.HabboOriginal)));
        nav.Children.Add(NavButton("◆  Nitro Custom", () => ShowCategory(OperationCategory.NitroCustom)));
        nav.Children.Add(NavButton("⚒  Hotel Tools", () => ShowCategory(OperationCategory.HotelTools)));
        nav.Children.Add(NavButton("▤  Database", () => ShowCategory(OperationCategory.Database)));
        nav.Children.Add(NavButton("ⓘ  About", () => ShowCategory(OperationCategory.General)));
        Grid.SetRow(nav, 1);
        sidebar.Children.Add(nav);

        var footer = new StackPanel { Spacing = 12, Margin = new Thickness(8, 0) };
        var switchInterface = new Button
        {
            Content = "Switch interface",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Padding = new Thickness(10, 8)
        };
        switchInterface.Click += async (_, _) => await SwitchInterfaceAsync();
        footer.Children.Add(switchInterface);
        footer.Children.Add(new TextBlock
        {
            Text = "PROFESSIONAL UI\nAdaptive • MVVM",
            Foreground = new SolidColorBrush(Color.Parse("#9EACCB")),
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

    private Button NavButton(string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Padding = new Thickness(14, 11),
            Background = Brushes.Transparent,
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            FontSize = 14
        };
        button.Click += (_, _) =>
        {
            if (!_viewModel.IsRunning) action();
        };
        return button;
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

    private void ShowCategory(OperationCategory category)
    {
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
            Background = Accent,
            Foreground = Brushes.White,
            Padding = new Thickness(22, 9),
            Margin = new Thickness(10, 0, 0, 0)
        };
        run.Click += async (_, _) => await RunSelectedAsync();
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
        if (operation.IsDestructive && !await ConfirmDestructiveAsync(operation.Title)) return;
        await _viewModel.RunSelectedAsync();
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
        var confirm = new Button { Content = "Confirm and run", Background = Accent, Foreground = Brushes.White, Padding = new Thickness(18, 8) };
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
            description.Text = operation?.Description ?? "Choose a card above to inspect and run it.";
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

    private static Border Card(Control content, Thickness? margin = null) => new()
    {
        Child = content,
        Padding = new Thickness(18),
        Margin = margin ?? new Thickness(0),
        CornerRadius = new CornerRadius(12),
        BorderBrush = new SolidColorBrush(Color.Parse("#D8DEE9")),
        BorderThickness = new Thickness(1),
        Background = new SolidColorBrush(Color.Parse("#0DFFFFFF"))
    };
}
