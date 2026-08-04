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
        // FFDec is a short-lived JVM launched once per furni, so its cold start
        // (JIT tiering + parallel-GC thread startup) dominates - not steady-state
        // throughput. C1-only JIT and the single-threaded serial GC cut both the
        // per-furni CPU cost and the startup latency a lot across thousands of
        // runs. Passed via JAVA_TOOL_OPTIONS so it reaches both the Windows
        // bundled-JVM exe and the `java -jar` fallback. If the bundled JVM ever
        // rejects a flag (conversions start failing immediately), drop this line.
        private const string JvmFastStartOptions = "-XX:TieredStopAtLevel=1 -XX:+UseSerialGC";

        public static ProcessStartInfo BuildStartInfo(string ffdecArguments)
        {
            var toolsDir = Path.Combine("Tools", "ffdec");

            ProcessStartInfo startInfo;

            if (OperatingSystem.IsWindows())
            {
                var exe = Path.Combine(toolsDir, "ffdec-cli.exe");
                startInfo = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = ffdecArguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }
            else
            {
                // Linux / macOS: there is no native binary, fall back to the jar.
                var jar = Path.Combine(toolsDir, "ffdec-cli.jar");
                startInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar \"{jar}\" {ffdecArguments}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
            }

            startInfo.Environment["JAVA_TOOL_OPTIONS"] = JvmFastStartOptions;

            return startInfo;
        }
    }
}
