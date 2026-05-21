using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Habbo_Downloader.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    /// <summary>
    /// Downloads a Nitro V3 split-mode gamedata directory
    /// (root manifest.json5 + core/custom/seasonal/ tier manifests + chunked
    /// .json5 parts, as produced by Nitro-V3/scripts/split-gamedata.mjs).
    /// The merged result is written back as a single flat JSON file so the
    /// rest of the pipeline can stay unchanged.
    /// </summary>
    internal static class NitroSplitDownloader
    {
        public static bool LooksLikeSplitUrl(string url)
            => !string.IsNullOrEmpty(url) && url.EndsWith("/", StringComparison.Ordinal);

        /// <summary>
        /// Mirrors a remote split-mode gamedata directory into <paramref name="localBaseDir"/>
        /// (so the file layout matches what FurnidataIO / FigureDataIO etc. expect),
        /// then returns the merged JObject.
        /// </summary>
        public static async Task<JObject> FetchSplitAsync(HttpClient http, string baseUrl, string localBaseDir, string dataset)
        {
            if (!baseUrl.EndsWith("/", StringComparison.Ordinal))
                baseUrl += "/";

            if (Directory.Exists(localBaseDir)) Directory.Delete(localBaseDir, true);
            Directory.CreateDirectory(localBaseDir);

            // 1) root manifest
            var rootManifestText = await FetchTextWithExtFallbackAsync(http, baseUrl + "manifest", new[] { ".json5", ".json" });
            await File.WriteAllTextAsync(Path.Combine(localBaseDir, "manifest.json5"), rootManifestText);
            var rootManifest = ParseJson5(rootManifestText);
            var tiers = rootManifest["tiers"]?.Select(t => t.ToString()).ToArray()
                ?? new[] { "core", "custom", "seasonal" };

            // 2) for every tier, manifest + files
            foreach (var tier in tiers)
            {
                Console.WriteLine($"  tier '{tier}' ...");
                string tierManifestText;
                try
                {
                    tierManifestText = await FetchTextWithExtFallbackAsync(http, $"{baseUrl}{tier}/manifest", new[] { ".json5", ".json" });
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine($"    tier '{tier}' has no manifest, skipping (this is fine for custom/seasonal).");
                    continue;
                }

                var tierDir = Path.Combine(localBaseDir, tier);
                Directory.CreateDirectory(tierDir);
                await File.WriteAllTextAsync(Path.Combine(tierDir, "manifest.json5"), tierManifestText);

                var tierManifest = ParseJson5(tierManifestText);
                var files = tierManifest["files"]?.Select(f => f.ToString()).ToArray() ?? Array.Empty<string>();
                foreach (var fileName in files)
                {
                    var partText = await http.GetStringAsync($"{baseUrl}{tier}/{fileName}");
                    await File.WriteAllTextAsync(Path.Combine(tierDir, fileName), partText);
                }
                Console.WriteLine($"    {files.Length} part(s) downloaded.");
            }

            // 3) re-use the existing IO classes to load + merge in load order.
            return dataset switch
            {
                "furnidata"   => await FurnidataIO.LoadAsync(localBaseDir),
                "productdata" => await ProductDataIO.LoadAsync(localBaseDir),
                "figuredata"  => await FigureDataIO.LoadAsync(localBaseDir),
                "figuremap"   => await FigureMapIO.LoadAsync(localBaseDir),
                _             => throw new ArgumentException($"Unknown dataset: {dataset}", nameof(dataset))
            };
        }

        private static async Task<string> FetchTextWithExtFallbackAsync(HttpClient http, string baseUrlNoExt, string[] exts)
        {
            HttpRequestException? last = null;
            foreach (var ext in exts)
            {
                try { return await http.GetStringAsync(baseUrlNoExt + ext); }
                catch (HttpRequestException ex) { last = ex; }
            }
            throw last ?? new HttpRequestException($"Could not fetch {baseUrlNoExt} with any of: {string.Join(", ", exts)}");
        }

        private static JObject ParseJson5(string raw)
        {
            using var sr = new StringReader(raw);
            using var jr = new JsonTextReader(sr);
            return JObject.Load(jr, new JsonLoadSettings
            {
                CommentHandling = CommentHandling.Ignore,
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Replace
            });
        }
    }
}
