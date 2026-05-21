using System.Threading.Tasks;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// Static facade that routes a menu request to the presenter matching the
    /// current run mode (set by App.RunAsync). Sub-menus call this without
    /// caring whether they will be drawn in Console plain or Terminal.Gui mainframe style.
    /// </summary>
    public static class MenuHost
    {
        public static RunMode Mode { get; set; } = RunMode.Cli;

        public static Task ShowAsync(string title, MenuItem[] items, bool isTopLevel = false)
            => Mode switch
            {
                RunMode.Tui => TuiMenuPresenter.ShowAsync(title, items, isTopLevel),
                _           => ConsoleMenuPresenter.ShowAsync(title, items, isTopLevel)
            };
    }
}
