using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Habbo_Downloader.App.Gui;

namespace Habbo_Downloader.App.Professional.Views;

public sealed class InterfaceSelectorWindow : Window
{
    private readonly TaskCompletionSource<RunMode> _result = new();
    public Task<RunMode> ResultTask => _result.Task;

    public InterfaceSelectorWindow()
    {
        Title = "All-in-1 Converter — Choose interface";
        Width = 980;
        Height = 650;
        MinWidth = 820;
        MinHeight = 580;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Default;
        Content = BuildContent();
        Closed += (_, _) => _result.TrySetResult(RunMode.Quit);
    }

    private Control BuildContent()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(42, 34) };
        var header = new StackPanel { Spacing = 7 };
        header.Children.Add(new TextBlock { Text = "Choose your workspace", FontSize = 32, FontWeight = FontWeight.Bold });
        header.Children.Add(new TextBlock { Text = "All four interfaces use the same converter functions and configuration.", FontSize = 15, Foreground = Brushes.Gray });
        root.Children.Add(header);

        var choices = new UniformGrid { Columns = 2, Rows = 2, Margin = new Thickness(0, 28, 0, 24) };
        choices.Children.Add(Choice("Professional", "Modern MVVM dashboard with native operation pages, live logs and system theme.", RunMode.Professional, true));
        choices.Children.Add(Choice("Desktop GUI", "The original Avalonia menu interface with Mainframe and Matrix themes.", RunMode.Gui));
        choices.Children.Add(Choice("Terminal UI", "Mouse-driven Terminal.Gui interface for local terminals and SSH.", RunMode.Tui));
        choices.Children.Add(Choice("Classic CLI", "Keyboard-only numbered menus for simple console workflows.", RunMode.Cli));
        Grid.SetRow(choices, 1);
        root.Children.Add(choices);

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        footer.Children.Add(new TextBlock { Text = ".NET 11 • Windows + Linux • Strict JSON", Foreground = Brushes.Gray, VerticalAlignment = VerticalAlignment.Center });
        var close = new Button { Content = "Exit", Padding = new Thickness(22, 9) };
        close.Click += (_, _) => Select(RunMode.Quit);
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 2);
        root.Children.Add(footer);
        return root;
    }

    private Border Choice(string title, string description, RunMode mode, bool recommended = false)
    {
        var panel = new StackPanel { Spacing = 10 };
        var titleRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        titleRow.Children.Add(new TextBlock { Text = title, FontSize = 21, FontWeight = FontWeight.SemiBold });
        if (recommended)
            titleRow.Children.Add(new Border
            {
                Background = new SolidColorBrush(Color.Parse("#536DFE")),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3),
                Child = new TextBlock { Text = "RECOMMENDED", Foreground = Brushes.White, FontSize = 10 }
            });
        panel.Children.Add(titleRow);
        panel.Children.Add(new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap, Foreground = Brushes.Gray, MinHeight = 52 });
        if (mode == RunMode.Gui)
        {
            var themes = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var mainframe = new Button { Content = "Mainframe", Padding = new Thickness(15, 8) };
            mainframe.Click += (_, _) => SelectGui(GuiTheme.Mainframe());
            var matrix = new Button { Content = "Matrix", Padding = new Thickness(15, 8) };
            matrix.Click += (_, _) => SelectGui(GuiTheme.Matrix());
            themes.Children.Add(mainframe);
            themes.Children.Add(matrix);
            panel.Children.Add(themes);
        }
        else
        {
            var open = new Button { Content = "Open", Padding = new Thickness(18, 8), HorizontalAlignment = HorizontalAlignment.Left };
            open.Click += (_, _) => Select(mode);
            panel.Children.Add(open);
        }
        return new Border
        {
            Child = panel,
            Padding = new Thickness(22),
            Margin = new Thickness(7),
            BorderBrush = recommended ? new SolidColorBrush(Color.Parse("#536DFE")) : new SolidColorBrush(Color.Parse("#CBD2DF")),
            BorderThickness = new Thickness(recommended ? 2 : 1),
            CornerRadius = new CornerRadius(14),
            Background = new SolidColorBrush(Color.Parse("#0DFFFFFF"))
        };
    }

    private void Select(RunMode mode)
    {
        _result.TrySetResult(mode);
        Close();
    }

    private void SelectGui(GuiTheme theme)
    {
        GuiMenuPresenter.QueueTheme(theme);
        Select(RunMode.Gui);
    }
}
