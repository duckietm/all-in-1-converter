using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace Habbo_Downloader.IO
{
    internal static class ManifestWriter
    {
        private static readonly JsonSerializerOptions IndentedOpts = new()
        {
            WriteIndented = true
        };

        public static async Task WriteRootManifestAsync(string outDir, bool asJson5)
        {
            var rootManifest = new { tiers = new[] { "core", "custom", "seasonal" } };
            var body = JsonSerializer.Serialize(rootManifest, IndentedOpts);
            var ext = asJson5 ? "json5" : "json";
            var content = asJson5
                ? "// Root manifest - load order of tiers (later overrides earlier by id/classname).\n" +
                  $"// Drop a custom/manifest.{ext} and/or seasonal/manifest.{ext} to add\n" +
                  "// override tiers without touching core/.\n" +
                  body + "\n"
                : body + "\n";
            await File.WriteAllTextAsync(Path.Combine(outDir, $"manifest.{ext}"), content);
        }

        public static async Task WriteTierManifestAsync(string tierDir, IList<string> files, bool asJson5, string headerComment = null)
        {
            var tierManifest = new { files };
            var body = JsonSerializer.Serialize(tierManifest, IndentedOpts);
            var ext = asJson5 ? "json5" : "json";
            string content;
            if (asJson5 && !string.IsNullOrEmpty(headerComment))
                content = $"// {headerComment}\n{body}\n";
            else
                content = body + "\n";
            await File.WriteAllTextAsync(Path.Combine(tierDir, $"manifest.{ext}"), content);
        }

        public static async Task WritePartAsync(string filePath, object content, bool asJson5, string headerComment = null)
        {
            var body = JsonSerializer.Serialize(content, IndentedOpts);
            string output = asJson5 && !string.IsNullOrEmpty(headerComment)
                ? $"// {headerComment}\n{body}\n"
                : body + "\n";
            await File.WriteAllTextAsync(filePath, output);
        }
    }
}
