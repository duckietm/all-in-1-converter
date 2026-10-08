using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using static Habbo_Downloader.App.Professional.ProfessionalTheme;

namespace Habbo_Downloader.App.Professional.Views;

public sealed class InterfaceSelectorWindow : Window
{
    private readonly TaskCompletionSource<RunMode> _result = new();
    public Task<RunMode> ResultTask => _result.Task;

    public InterfaceSelectorWindow()
    {
        ProfessionalTheme.Install(Application.Current!);
        Title = "All-in-1 Converter";
        Width = 860;
        Height = 440;
        MinWidth = 760;
        MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        RequestedThemeVariant = ThemeVariant.Default;
        this.Bind(BackgroundProperty, "Pro.Surface");
        Content = BuildContent();
        Closed += (_, _) => _result.TrySetResult(RunMode.Quit);
    }

    private Control BuildContent()
    {
        var root = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto"), Margin = new Thickness(40, 34, 40, 28) };

        var brand = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, Margin = new Thickness(0, 0, 0, 22) };
        var logo = Icon(Icons.Cube, 16);
        logo.Foreground = Brushes.White;
        brand.Children.Add(new Border { Width = 30, Height = 30, CornerRadius = new CornerRadius(8), Background = Solid(Brand), Child = logo });
        var name = Text("All-in-1 Converter", 14, "Pro.Muted", FontWeight.SemiBold);
        name.VerticalAlignment = VerticalAlignment.Center;
        brand.Children.Add(name);
        root.Children.Add(brand);

        var header = new StackPanel { Spacing = 6 };
        header.Children.Add(Text("Choose your workspace", 28, weight: FontWeight.Bold));
        header.Children.Add(Text("Both interfaces use the same converter functions and configuration.", 14, "Pro.Muted"));
        Grid.SetRow(header, 1);
        root.Children.Add(header);

        var choices = new UniformGrid { Columns = 2, Rows = 1, Margin = new Thickness(-8, 24, -8, 20), VerticalAlignment = VerticalAlignment.Top };
        choices.Children.Add(Choice("Professional", "Dashboard with operation pages, live output and the light or dark system theme.", Icons.Home, RunMode.Professional, true));
        choices.Children.Add(Choice("Classic CLI", "Keyboard-only numbered menus for console workflows, scripts and SSH.", Icons.Swap, RunMode.Cli));
        Grid.SetRow(choices, 2);
        root.Children.Add(choices);

        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        var hint = Text("You can switch later from the sidebar, or with 's' in the CLI menu.", 12, "Pro.Muted");
        hint.VerticalAlignment = VerticalAlignment.Center;
        footer.Children.Add(hint);
        var close = new Button { Content = "Exit", Padding = new Thickness(20, 8), Classes = { "secondary" } };
        close.Click += (_, _) => Select(RunMode.Quit);
        Grid.SetColumn(close, 1);
        footer.Children.Add(close);
        Grid.SetRow(footer, 3);
        root.Children.Add(footer);
        return root;
    }

    private Button Choice(string title, string description, string icon, RunMode mode, bool recommended = false)
    {
        var panel = new StackPanel { Spacing = 8 };
        var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), Margin = new Thickness(0, 0, 0, 6) };
        var glyph = Icon(icon, 20).Bind(PathIcon.ForegroundProperty, "Pro.AccentText");
        top.Children.Add(new Border { Width = 42, Height = 42, CornerRadius = new CornerRadius(11), Child = glyph }
            .Bind(Border.BackgroundProperty, "Pro.AccentSoft"));
        if (recommended)
        {
            var chip = Chip("RECOMMENDED", "Pro.AccentSoft", "Pro.AccentText");
            chip.VerticalAlignment = VerticalAlignment.Top;
            Grid.SetColumn(chip, 2);
            top.Children.Add(chip);
        }
        panel.Children.Add(top);
        panel.Children.Add(Text(title, 18, weight: FontWeight.SemiBold));
        panel.Children.Add(Text(description, 13, "Pro.Muted"));
        var open = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(0, 8, 0, 0) };
        open.Children.Add(Text("Open", 13, "Pro.AccentText", FontWeight.SemiBold));
        open.Children.Add(Icon(Icons.ChevronRight, 16).Bind(PathIcon.ForegroundProperty, "Pro.AccentText"));
        panel.Children.Add(open);

        var button = new Button
        {
            Name = mode == RunMode.Professional ? "ChooseProfessional" : "ChooseCli",
            Content = panel,
            Padding = new Thickness(22, 20),
            Margin = new Thickness(8, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            CornerRadius = new CornerRadius(14),
            BorderThickness = new Thickness(recommended ? 2 : 1),
            Classes = { "tile" }
        };
        if (recommended) button.BorderBrush = Solid(Brand);
        button.Click += (_, _) => Select(mode);
        return button;
    }

    private void Select(RunMode mode)
    {
        _result.TrySetResult(mode);
        Close();
    }
}
