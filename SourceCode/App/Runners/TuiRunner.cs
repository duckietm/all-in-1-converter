using System.Threading.Tasks;
using ConsoleApplication;
using Habbo_Downloader.App.Menus;

namespace Habbo_Downloader.App.Runners
{
    /// <summary>
    /// Mouse-driven TUI runner. The main menu is delegated to MenuHost just like every
    /// sub-menu, so the entire workstation uses one consistent mainframe theme.
    /// </summary>
    public sealed class TuiRunner : IAppRunner
    {
        public Task RunAsync(Args args) =>
            MenuHost.ShowAsync("Main Menu: Select a Topic", new MenuItem[]
            {
                new("1",       "Habbo Original Downloads",            HabboOriginalMenu.DisplayMenu, IsSubMenu: true),
                new("2",       "Nitro Custom Downloads",              NitroCustomMenu.DisplayMenu,   IsSubMenu: true),
                new("3",       "Hotel Tools",                         HotelToolsMenu.DisplayMenu,    IsSubMenu: true),
                new("4",       "Database Tools",                      DatabaseMenu.DisplayMenu,      IsSubMenu: true),
                new("version", "Fetch current Habbo client version",  CliRunner.DisplayVersionAsync),
            }, isTopLevel: true);
    }
}
