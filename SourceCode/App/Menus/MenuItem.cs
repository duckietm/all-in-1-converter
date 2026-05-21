using System;
using System.Threading.Tasks;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// A single entry in a menu.
    /// <see cref="Key"/> is the shortcut typed in CLI mode (e.g. "1", "back") and forms the button label in TUI/GUI.
    /// <see cref="IsSubMenu"/> = true when the action opens another MenuHost.ShowAsync (so TUI/GUI don't trap it in an output window).
    /// <see cref="HowToUse"/> is a multi-line description rendered by the auto-generated "?" help item of each menu.
    /// </summary>
    public sealed record MenuItem(
        string Key,
        string Label,
        Func<Task> Action,
        bool IsSubMenu = false,
        string? HowToUse = null);
}
