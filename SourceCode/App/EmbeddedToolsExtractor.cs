using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Unpacks the FFDec bundle (and anything else under Compiled/Tools/) that
    /// is embedded in the binary as <c>Habbo_Downloader.EmbeddedTools.zip</c>.
    /// Extraction happens once into <c>./Tools/</c> next to the executable, so
    /// every existing path (e.g. <c>Tools/ffdec/ffdec-cli.exe</c>) keeps
    /// working without any runtime indirection.
    /// </summary>
    internal static class EmbeddedToolsExtractor
    {
        private const string ResourceName  = "Habbo_Downloader.EmbeddedTools.zip";
        private const string SentinelPath  = "Tools/ffdec/ffdec-cli.jar"; // relative

        public static void EnsureExtracted()
        {
            var baseDir = AppContext.BaseDirectory;
            var sentinel = Path.Combine(baseDir, SentinelPath);
            if (File.Exists(sentinel)) return; // already unpacked on this machine

            var asm = typeof(EmbeddedToolsExtractor).Assembly;
            using var stream = asm.GetManifestResourceStream(ResourceName);
            if (stream == null)
            {
                Console.Error.WriteLine(
                    $"[embedded-tools] '{ResourceName}' missing from assembly. " +
                    "Either rebuild with the ZipDirectory target enabled or drop a Tools/ folder next to the binary manually.");
                return;
            }

            try
            {
                using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
                archive.ExtractToDirectory(baseDir, overwriteFiles: true);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[embedded-tools] extraction failed: {ex.Message}");
                return;
            }

            // Restore the executable bit on the unix shell launchers that FFDec
            // ships with - ZipArchive doesn't preserve unix file modes.
            if (!OperatingSystem.IsWindows())
            {
                MakeExecutable(Path.Combine(baseDir, "Tools", "ffdec", "ffdec.sh"));
                MakeExecutable(Path.Combine(baseDir, "Tools", "ffdec", "soleditor.sh"));
                MakeExecutable(Path.Combine(baseDir, "Tools", "ffdec", "translator.sh"));
            }
        }

        [System.Runtime.Versioning.UnsupportedOSPlatform("windows")]
        private static void MakeExecutable(string path)
        {
            if (!File.Exists(path)) return;
            try
            {
                File.SetUnixFileMode(path,
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                    UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                    UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
            }
            catch
            {
                // Non-fatal: ffdec-cli.jar runs through `java` regardless of script perms.
            }
        }
    }
}
