using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using Habbo_Downloader.App.Menus;

namespace Habbo_Downloader.App.Gui
{
    /// <summary>
    /// MenuHost adapter for the Avalonia GUI. Opens a MenuWindow for each call to
    /// MenuHost.ShowAsync; leaf actions are wrapped in an OutputWindow so their
    /// Console output renders inside the mainframe/matrix theme.
    /// </summary>
    internal static class GuiMenuPresenter
    {
        public static GuiTheme ActiveTheme { get; set; } = GuiTheme.Mainframe();

        public static async Task ShowAsync(string title, MenuItem[] items, bool isTopLevel = false)
        {
            while (true)
            {
                var window = await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    var w = new MenuWindow(title, items, isTopLevel, ActiveTheme);
                    w.Show();
                    return w;
                });

                var picked = await window.ResultTask;
                if (picked == null) return;

                if (picked.IsSubMenu)
                {
                    await picked.Action();
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(async () =>
                    {
                        var ow = new OutputWindow($"{title} -> {picked.Label}", picked.Action, ActiveTheme);
                        ow.Show();
                        await ow.ClosedTask;
                    });
                }

                if (MenuHost.SwitchRequested) return;
            }
        }
    }
}
