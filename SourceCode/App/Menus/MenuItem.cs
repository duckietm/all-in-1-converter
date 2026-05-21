using System;
using System.Threading.Tasks;

namespace Habbo_Downloader.App.Menus
{
    /// <summary>
    /// A single entry in a menu. <see cref="Key"/> is the shortcut typed in CLI mode
    /// (e.g. "1", "back") and forms the button label hint in TUI mode.
    /// Set <see cref="IsSubMenu"/> to true when the action opens another <c>MenuHost</c>
    /// rather than performing an actual leaf operation - this prevents the TUI runner
    /// from wrapping it in the output-capture window.
    /// </summary>
    public sealed record MenuItem(string Key, string Label, Func<Task> Action, bool IsSubMenu = false);
}
