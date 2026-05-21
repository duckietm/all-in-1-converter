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
            MenuHost.ShowAsync("Main Menu: Select a Topic", MainMenuFactory.Build(), isTopLevel: true);
    }
}
