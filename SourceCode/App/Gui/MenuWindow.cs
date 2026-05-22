using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using MenuItem = Habbo_Downloader.App.Menus.MenuItem;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// Renders a menu (main or sub) inside an Avalonia window with the chosen
    /// mainframe / matrix theme. Each button click resolves a TaskCompletionSource
    /// with either the picked MenuItem or null (back / exit).
    /// </summary>
    public sealed class MenuWindow : Window
    {
        private readonly TaskCompletionSource<MenuItem?> _result = new();

        public Task<MenuItem?> ResultTask => _result.Task;

        public MenuWindow(string title, MenuItem[] items, bool isTopLevel, GuiTheme theme)
        {
            Title = $"ALL-IN-1 CONVERTER - {title.ToUpperInvariant()}";
            Width = 900;
            Height = 640;
            Background = theme.Background;
            FontFamily = new FontFamily(theme.FontFamily);
            FontSize = 16;
            CanResize = true;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Background = theme.Kind == "MATRIX" ? Brushes.Transparent : theme.Background
            };

            grid.Children.Add(BuildHeader(title, theme));

            var body = BuildBody(title, items, isTopLevel, theme);
            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            var footer = BuildFooter(isTopLevel, theme);
            Grid.SetRow(footer, 2);
            grid.Children.Add(footer);

            // Matrix theme gets a digital-rain background behind everything.
            // IsHitTestVisible is false on the rain itself so clicks pass through
            // to the buttons / scroll viewer above.
            if (theme.Kind == "MATRIX")
            {
                var rootStack = new Grid();
                var rain = new MatrixRain();
                rootStack.Children.Add(rain);
                rootStack.Children.Add(grid);
                Content = rootStack;
            }
            else
            {
                Content = grid;
            }

            Closed += (_, __) =>
            {
                if (!_result.Task.IsCompleted)
                    _result.TrySetResult(null);
            };
        }

        private Control BuildHeader(string title, GuiTheme theme)
        {
            var headerGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                Background = theme.HeaderBg,
                Margin = new Avalonia.Thickness(0)
            };

            var left = new TextBlock
            {
                Text = "ALL-IN-1 CONVERTER",
                Foreground = theme.HeaderFg,
                Padding = new Avalonia.Thickness(12, 8),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold
            };
            Grid.SetColumn(left, 0);

            var center = new TextBlock
            {
                Text = $"*** {title.ToUpperInvariant()} ***",
                Foreground = theme.HeaderFg,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.Bold
            };
            Grid.SetColumn(center, 1);

            var right = new TextBlock
            {
                Text = $"DATE: {DateTime.UtcNow:yyyy-MM-dd}  |  THEME: {theme.Kind}",
                Foreground = theme.HeaderFg,
                Padding = new Avalonia.Thickness(12, 8),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(right, 2);

            headerGrid.Children.Add(left);
            headerGrid.Children.Add(center);
            headerGrid.Children.Add(right);
            return headerGrid;
        }

        private Control BuildBody(string title, MenuItem[] items, bool isTopLevel, GuiTheme theme)
        {
            var border = new Border
            {
                BorderBrush = theme.FrameBorder,
                BorderThickness = new Avalonia.Thickness(2),
                Margin = new Avalonia.Thickness(16, 12, 16, 12),
                Padding = new Avalonia.Thickness(16),
                Background = theme.Kind == "MATRIX"
                    ? new SolidColorBrush(Color.FromArgb(0xB0, 0, 0, 0))   // 70% black so rain peeks through
                    : theme.Background
            };

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                Spacing = 8
            };

            var titleLabel = new TextBlock
            {
                Text = title.ToUpperInvariant(),
                Foreground = theme.Foreground,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Avalonia.Thickness(0, 0, 0, 16)
            };
            stack.Children.Add(titleLabel);

            foreach (var item in items)
            {
                if (string.Equals(item.Key, "back", StringComparison.OrdinalIgnoreCase)) continue;
                var captured = item;
                var btn = new Button
                {
                    Content = $"[{item.Key}] {item.Label.ToUpperInvariant()}",
                    Width = 520,
                    Background = theme.ButtonBg,
                    Foreground = theme.ButtonFg,
                    BorderBrush = theme.FrameBorder,
                    BorderThickness = new Avalonia.Thickness(1),
                    Padding = new Avalonia.Thickness(12, 8),
                    HorizontalContentAlignment = HorizontalAlignment.Left,
                    FontFamily = new FontFamily(theme.FontFamily)
                };
                btn.Click += (_, __) =>
                {
                    _result.TrySetResult(captured);
                    Close();
                };
                stack.Children.Add(btn);
            }

            var exitBtn = new Button
            {
                Content = isTopLevel ? "[EXIT] QUIT APPLICATION" : "[BACK] RETURN TO PREVIOUS MENU",
                Width = 520,
                Background = theme.ExitBg,
                Foreground = theme.ExitFg,
                BorderBrush = theme.ExitFg,
                BorderThickness = new Avalonia.Thickness(1),
                Padding = new Avalonia.Thickness(12, 8),
                Margin = new Avalonia.Thickness(0, 16, 0, 0),
                HorizontalContentAlignment = HorizontalAlignment.Left,
                FontWeight = FontWeight.Bold,
                FontFamily = new FontFamily(theme.FontFamily)
            };
            exitBtn.Click += (_, __) =>
            {
                _result.TrySetResult(null);
                Close();
            };
            stack.Children.Add(exitBtn);

            var scroll = new ScrollViewer
            {
                Content = stack,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
                VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
            };
            border.Child = scroll;
            return border;
        }

        private Control BuildFooter(bool isTopLevel, GuiTheme theme)
        {
            var footer = new TextBlock
            {
                Text = isTopLevel
                    ? " Click a menu item to enter, or [EXIT] to quit the application "
                    : " Click a menu item to run, or [BACK] to return to the previous menu ",
                Background = theme.FooterBg,
                Foreground = theme.FooterFg,
                Padding = new Avalonia.Thickness(12, 6)
            };
            return footer;
        }
    }
}
