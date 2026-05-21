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
    /// Reads/writes FigureData in flat or split mode.
    /// Schema: { palettes: [...], setTypes: [...] }
    /// Split: 1 palettes file + N setType files (one per type).
    /// </summary>
    public static class FigureDataIO
    {
        public const string FlatFileName = "FigureData.json";

        public static async Task<JObject> LoadAsync(string path)
        {
            if (File.Exists(path)) return await JsonReadHelper.LoadJObjectAsync(path);
            if (Directory.Exists(path))
            {
                if (IsSplitDirectory(path)) return await LoadSplitAsync(path);
                var probe = Path.Combine(path, FlatFileName);
                if (File.Exists(probe)) return await JsonReadHelper.LoadJObjectAsync(probe);
            }
            throw new FileNotFoundException($"FigureData not found at: {path}");
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

            JObject merged = new JObject
            {
                ["palettes"] = new JArray(),
                ["setTypes"] = new JArray()
            };

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

        // Palettes identity = id. SetTypes identity = type.
        private static void MergePart(JObject merged, JObject part)
        {
            if (part["palettes"] is JArray partPalettes)
            {
                var mergedPalettes = (JArray)merged["palettes"];
                var byId = mergedPalettes.Cast<JObject>()
                    .Where(j => j["id"] != null)
                    .ToDictionary(j => j["id"].Value<int>(), j => j);
                foreach (var item in partPalettes.Cast<JObject>())
                {
                    if (item["id"] == null) { mergedPalettes.Add(item); continue; }
                    var id = item["id"].Value<int>();
                    if (byId.TryGetValue(id, out var existing))
                    {
                        mergedPalettes[mergedPalettes.IndexOf(existing)] = item;
                        byId[id] = item;
                    }
                    else { mergedPalettes.Add(item); byId[id] = item; }
                }
            }
            if (part["setTypes"] is JArray partSetTypes)
            {
                var mergedSetTypes = (JArray)merged["setTypes"];
                var byType = mergedSetTypes.Cast<JObject>()
                    .Where(j => j["type"] != null)
                    .ToDictionary(j => j["type"].ToString(), j => j);
                foreach (var item in partSetTypes.Cast<JObject>())
                {
                    if (item["type"] == null) { mergedSetTypes.Add(item); continue; }
                    var t = item["type"].ToString();
                    if (byType.TryGetValue(t, out var existing))
                    {
                        mergedSetTypes[mergedSetTypes.IndexOf(existing)] = item;
                        byType[t] = item;
                    }
                    else { mergedSetTypes.Add(item); byType[t] = item; }
                }
            }
        }

        public static async Task SaveAsync(JObject data, string path, GamedataFormat format)
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

            var coreFiles = new List<string>();

            var palettes = data["palettes"] as JArray;
            if (palettes != null && palettes.Count > 0)
            {
                var fname = "palettes.json5";
                var content = new JObject { ["palettes"] = palettes };
                var header = $"Color palettes ({palettes.Count})";
                await ManifestWriter.WritePartAsync(Path.Combine(coreDir, fname), content, asJson5: true, headerComment: header);
                coreFiles.Add(fname);
            }

            var setTypes = data["setTypes"] as JArray;
            if (setTypes != null)
            {
                foreach (var st in setTypes.Cast<JObject>())
                {
                    var t = (st["type"]?.ToString() ?? "unknown");
                    var safeT = new string(t.Select(c => char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '_').ToArray()).ToLowerInvariant();
                    var fname = $"settype-{safeT}.json5";
                    var content = new JObject { ["setTypes"] = new JArray(st) };
                    var paletteId = st["paletteId"]?.ToString() ?? "?";
                    var header = $"setType \"{t}\" (paletteId={paletteId})";
                    await ManifestWriter.WritePartAsync(Path.Combine(coreDir, fname), content, asJson5: true, headerComment: header);
                    coreFiles.Add(fname);
                }
            }

            await ManifestWriter.WriteTierManifestAsync(coreDir, coreFiles, asJson5: true,
                headerComment: $"Auto-generated split of FigureData ({coreFiles.Count} files)");
            await ManifestWriter.WriteRootManifestAsync(path, asJson5: true);
        }
    }
}
