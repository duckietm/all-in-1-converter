using System;
using System.Runtime.InteropServices;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Detects whether the binary was launched from an interactive terminal
    /// (cmd / powershell / bash / ssh) or double-clicked from a file manager
    /// (Explorer on Windows, Nautilus / .desktop file on Linux).
    ///
    /// On Windows we use GetConsoleProcessList: only the current process attached
    /// to the console = Explorer allocated a fresh console for us; two or more =
    /// a parent shell is sharing the console.
    ///
    /// On Linux / macOS we use isatty(stdin): if stdin is a TTY we are inside a
    /// terminal; if not (e.g. .desktop launch, GNOME Files double-click) we are
    /// detached.
    /// </summary>
    internal static class LaunchContext
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern uint GetConsoleProcessList(uint[] processList, uint processCount);

        [DllImport("libc", EntryPoint = "isatty", SetLastError = true)]
        private static extern int IsATty(int fd);

        public static bool IsFromTerminal
        {
            get
            {
                try
                {
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        var buf = new uint[8];
                        uint count = GetConsoleProcessList(buf, (uint)buf.Length);
                        return count > 1;
                    }

                    // Linux / macOS: stdin is a TTY iff isatty(0) returns 1.
                    return IsATty(0) == 1;
                }
                catch
                {
                    return true; // be conservative: show the prompt
                }
            }
        }
    }
}
