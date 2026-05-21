using System;
using System.Diagnostics;
using System.IO;

namespace Habbo_Downloader.Tools
{
    /// <summary>
    /// Picks the correct way to spawn FFDec for the current OS.
    /// Windows ships a native ffdec-cli.exe; on Linux / macOS we launch the
    /// shipped ffdec-cli.jar through `java -jar`.
    /// </summary>
    internal static class FfdecInvocation
    {
        public static ProcessStartInfo BuildStartInfo(string ffdecArguments)
        {
            var toolsDir = Path.Combine("Tools", "ffdec");

            if (OperatingSystem.IsWindows())
            {
                var exe = Path.Combine(toolsDir, "ffdec-cli.exe");
                return new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = ffdecArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            // Linux / macOS: there is no native binary, fall back to the jar.
            var jar = Path.Combine(toolsDir, "ffdec-cli.jar");
            return new ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{jar}\" {ffdecArguments}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
        }
    }
}
