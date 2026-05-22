using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Habbo_Downloader.IO
{
    /// <summary>
    /// Reads/writes FigureMap in flat or split mode.
    /// Schema: { libraries: [...] }
    /// Split: chunks of 500 by default.
    /// </summary>
    public static class FigureMapIO
    {
        public const string FlatFileName = "FigureMap.json";

        public static async Task<JObject> LoadAsync(string path)
        {
            if (File.Exists(path)) return await JsonReadHelper.LoadJObjectAsync(path);
            if (Directory.Exists(path))
            {
                if (IsSplitDirectory(path)) return await LoadSplitAsync(path);
                var probe = Path.Combine(path, FlatFileName);
                if (File.Exists(probe)) return await JsonReadHelper.LoadJObjectAsync(probe);
            }
            throw new FileNotFoundException($"FigureMap not found at: {path}");
        }

        public static bool IsSplitDirectory(string dirPath)
        {
            if (!Directory.Exists(dirPath)) return false;
            return File.Exists(Path.Combine(dirPath, "manifest.json5"))
                || File.Exists(Path.Combine(dirPath, "manifest.json"));
        }

        private static async Task<JObject> LoadSplitAsync(string dirPath)
        {
            var rootManifestPath = File.Exists(Path.Combine(dirPath, "manifest.json5"))
                ? Path.Combine(dirPath, "manifest.json5")
                : Path.Combine(dirPath, "manifest.json");
            var rootManifest = await JsonReadHelper.LoadJObjectAsync(rootManifestPath);
            var tiers = rootManifest["tiers"]?.Select(t => t.ToString()).ToArray()
                ?? new[] { "core", "custom", "seasonal" };

            JObject merged = new JObject { ["libraries"] = new JArray() };

            foreach (var tier in tiers)
            {
                var tierDir = Path.Combine(dirPath, tier);
                if (!Directory.Exists(tierDir)) continue;
                var tierManifestPath = File.Exists(Path.Combine(tierDir, "manifest.json5"))
                    ? Path.Combine(tierDir, "manifest.json5")
                    : File.Exists(Path.Combine(tierDir, "manifest.json"))
                        ? Path.Combine(tierDir, "manifest.json")
                        : null;
                if (tierManifestPath == null) continue;
                var tierManifest = await JsonReadHelper.LoadJObjectAsync(tierManifestPath);
                var files = tierManifest["files"]?.Select(f => f.ToString()) ?? Enumerable.Empty<string>();

                foreach (var fileName in files)
                {
                    var filePath = Path.Combine(tierDir, fileName);
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine($"  warn: missing file '{fileName}' in tier '{tier}'");
                        continue;
                    }
                    var part = await JsonReadHelper.LoadJObjectAsync(filePath);
                    MergePart(merged, part);
                }
            }
            return merged;
        }

        // Libraries identity = id.
        private static void MergePart(JObject merged, JObject part)
        {
            if (part["libraries"] is not JArray partLibs) return;
            var mergedLibs = (JArray)merged["libraries"];
            var byId = mergedLibs.Cast<JObject>()
                .Where(j => j["id"] != null)
                .ToDictionary(j => j["id"].ToString(), j => j);
            foreach (var item in partLibs.Cast<JObject>())
            {
                if (item["id"] == null) { mergedLibs.Add(item); continue; }
                var id = item["id"].ToString();
                if (byId.TryGetValue(id, out var existing))
                {
                    mergedLibs[mergedLibs.IndexOf(existing)] = item;
                    byId[id] = item;
                }
                else { mergedLibs.Add(item); byId[id] = item; }
            }
        }

        public static async Task SaveAsync(JObject data, string path, GamedataFormat format, int chunkSize = 500)
        {
            if (format == GamedataFormat.Flat)
            {
                var finalPath = Directory.Exists(path) ? Path.Combine(path, FlatFileName) : path;
                Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
                await File.WriteAllTextAsync(finalPath, data.ToString(Formatting.None));
                return;
            }

            Directory.CreateDirectory(path);
            var coreDir = Path.Combine(path, "core");
            if (Directory.Exists(coreDir)) Directory.Delete(coreDir, true);
            Directory.CreateDirectory(coreDir);

            var libs = (data["libraries"] as JArray)?.Cast<JObject>().ToList() ?? new List<JObject>();
            var coreFiles = new List<string>();

            for (int i = 0; i < libs.Count; i += chunkSize)
            {
                var slice = libs.GetRange(i, Math.Min(chunkSize, libs.Count - i));
                var fname = $"libraries-{(i / chunkSize + 1):000}.json5";
                var content = new JObject { ["libraries"] = new JArray(slice) };
                var header = $"libraries {i + 1}..{i + slice.Count} of {libs.Count}";
                await ManifestWriter.WritePartAsync(Path.Combine(coreDir, fname), content, asJson5: true, headerComment: header);
                coreFiles.Add(fname);
            }

            await ManifestWriter.WriteTierManifestAsync(coreDir, coreFiles, asJson5: true,
                headerComment: $"Auto-generated split of FigureMap ({coreFiles.Count} files, {libs.Count} libraries)");
            await ManifestWriter.WriteRootManifestAsync(path, asJson5: true);
        }
    }
}
