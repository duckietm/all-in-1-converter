using System;
using System.Threading.Tasks;
using Habbo_Downloader.App.Runners;
using Terminal.Gui;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// Mainframe-styled sub-menu: same green/black/cyan theme as the main TUI,
    /// FrameView with one button per item plus a [BACK] button.
    /// When the user picks an item, the TUI is shut down, the (Console-based)
    /// action runs full-screen, then the menu is re-entered.
    /// </summary>
    internal static class TuiMenuPresenter
    {
        public static async Task ShowAsync(string title, MenuItem[] items, bool isTopLevel = false)
        {
            while (true)
            {
                var (picked, exit) = ShowMenuOnce(title, items, isTopLevel);
                if (exit) return;
                if (picked == null) return;

                if (picked.IsSubMenu)
                {
                    // The action opens another MenuHost.ShowAsync internally; let it
                    // render its own mainframe screen instead of trapping it in the
                    // output-capture window (which would just show an empty TextView
                    // while the nested TUI is on screen).
                    await picked.Action();
                }
                else
                {
                    // Leaf action: capture its Console output in the mainframe-styled
                    // TUI window so it matches the look of every menu screen.
                    await TuiOutputWindow.RunAsync($"{title} -> {picked.Label}", picked.Action);
                }
            }
        }

        private static (MenuItem? picked, bool exit) ShowMenuOnce(string title, MenuItem[] items, bool isTopLevel)
        {
            MenuItem? pickedItem = null;
            bool exitRequested = false;

            Application.Init();
            try
            {
                var top = Application.Top;
                top.ColorScheme = TuiTheme.Base();

                var header = BuildHeader(title);
                top.Add(header);

                var frame = new FrameView($" {title.ToUpperInvariant()} ")
                {
                    X = 0, Y = 3,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(1),
                    ColorScheme = TuiTheme.Base()
                };

                int row = 1;
                foreach (var item in items)
                {
                    if (string.Equals(item.Key, "back", StringComparison.OrdinalIgnoreCase)) continue;
                    var captured = item;
                    var btn = new Button($"[{item.Key}] {item.Label.ToUpperInvariant()}")
                    {
                        X = Pos.Center(),
                        Y = row,
                        Width = 60,
                        ColorScheme = TuiTheme.Button()
                    };
                    btn.Clicked += () =>
                    {
                        pickedItem = captured;
                        Application.RequestStop();
                    };
                    frame.Add(btn);
                    row += 2;
                }

                var exitLabel = isTopLevel ? "[EXIT] QUIT APPLICATION" : "[BACK] RETURN TO PREVIOUS MENU";
                var backBtn = new Button(exitLabel)
                {
                    X = Pos.Center(),
                    Y = row + 1,
                    Width = 60,
                    ColorScheme = TuiTheme.ErrorBox()
                };
                backBtn.Clicked += () =>
                {
                    exitRequested = true;
                    Application.RequestStop();
                };
                frame.Add(backBtn);

                top.Add(frame);

                var sbLabel = isTopLevel ? "~ESC~ EXIT" : "~ESC~ BACK";
                var sb = new StatusBar(new StatusItem[]
                {
                    new StatusItem(Key.Esc, sbLabel, () =>
                    {
                        exitRequested = true;
                        Application.RequestStop();
                    }),
                })
                {
                    ColorScheme = TuiTheme.StatusBar()
                };
                top.Add(sb);

                Application.Run();
            }
            finally
            {
                Application.Shutdown();
            }

            return (pickedItem, exitRequested);
        }

        private static View BuildHeader(string title)
        {
            var header = new View
            {
                X = 0, Y = 0,
                Width = Dim.Fill(),
                Height = 3,
                ColorScheme = TuiTheme.Header()
            };
            var titleLeft = new Label("ALL-IN-1 CONVERTER")
            {
                X = 1, Y = 0
            };
            var titleCenter = new Label(title.ToUpperInvariant())
            {
                X = Pos.Center(), Y = 0
            };
            var titleRight = new Label($"DATE: {DateTime.UtcNow:yyyy-MM-dd}")
            {
                X = Pos.AnchorEnd(18), Y = 0
            };
            var separator = new Label(new string('=', 200))
            {
                X = 0, Y = 2,
                Width = Dim.Fill()
            };
            header.Add(titleLeft, titleCenter, titleRight, separator);
            return header;
        }
    }
}
