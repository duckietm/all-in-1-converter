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
    /// Reads/writes FurnitureData in flat (single JSON) or split (manifest.json5 + tier/) mode.
    /// Schema: { roomitemtypes: { furnitype: [...] }, wallitemtypes: { furnitype: [...] } }
    /// Split mode mirrors Nitro-V3/scripts/split-gamedata.mjs (chunks of 300 by default).
    /// </summary>
    public static class FurnidataIO
    {
        public const string FlatFileName = "FurnitureData.json";

        /// <summary>
        /// Auto-detects flat (file path) vs split (directory containing manifest.json5).
        /// </summary>
        public static async Task<JObject> LoadAsync(string path)
        {
            if (File.Exists(path))
                return await JsonReadHelper.LoadJObjectAsync(path);

            if (Directory.Exists(path))
                return await LoadSplitAsync(path);

            // Path may point to a directory that contains FurnitureData.json
            var flatProbe = Path.Combine(path, FlatFileName);
            if (File.Exists(flatProbe))
                return await JsonReadHelper.LoadJObjectAsync(flatProbe);

            throw new FileNotFoundException($"FurnitureData not found at: {path}");
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

            if (!File.Exists(rootManifestPath))
                throw new FileNotFoundException($"Root manifest not found in split directory: {dirPath}");

            var rootManifest = await JsonReadHelper.LoadJObjectAsync(rootManifestPath);
            var tiers = rootManifest["tiers"]?.Select(t => t.ToString()).ToArray()
                ?? new[] { "core", "custom", "seasonal" };

            JObject merged = NewEmptyFurnidata();

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
                        Console.WriteLine($"  warn: manifest references missing file '{fileName}' in tier '{tier}'");
                        continue;
                    }
                    var part = await JsonReadHelper.LoadJObjectAsync(filePath);
                    MergePart(merged, part);
                }
            }

            return merged;
        }

        private static JObject NewEmptyFurnidata() => new JObject
        {
            ["roomitemtypes"] = new JObject { ["furnitype"] = new JArray() },
            ["wallitemtypes"] = new JObject { ["furnitype"] = new JArray() }
        };

        // Later wins by id (Nitro-V3 split-gamedata convention).
        private static void MergePart(JObject merged, JObject part)
        {
            foreach (var kind in new[] { "roomitemtypes", "wallitemtypes" })
            {
                var partFurni = part[kind]?["furnitype"] as JArray;
                if (partFurni == null) continue;

                var mergedFurni = (JArray)merged[kind]["furnitype"];
                var byId = mergedFurni.Cast<JObject>()
                    .Where(j => j["id"] != null)
                    .ToDictionary(j => j["id"].Value<int>(), j => j);

                foreach (var item in partFurni.Cast<JObject>())
                {
                    if (item["id"] == null)
                    {
                        mergedFurni.Add(item);
                        continue;
                    }
                    var id = item["id"].Value<int>();
                    if (byId.TryGetValue(id, out var existing))
                    {
                        var idx = mergedFurni.IndexOf(existing);
                        mergedFurni[idx] = item;
                        byId[id] = item;
                    }
                    else
                    {
                        mergedFurni.Add(item);
                        byId[id] = item;
                    }
                }
            }
        }

        public static async Task SaveAsync(JObject data, string path, GamedataFormat format, int chunkSize = 300)
        {
            if (format == GamedataFormat.Flat)
                await SaveFlatAsync(data, path);
            else
                await SaveSplitAsync(data, path, chunkSize);
        }

        private static async Task SaveFlatAsync(JObject data, string path)
        {
            // If path is a directory, write FurnitureData.json inside it.
            var finalPath = Directory.Exists(path) ? Path.Combine(path, FlatFileName) : path;
            Directory.CreateDirectory(Path.GetDirectoryName(finalPath));
            await File.WriteAllTextAsync(finalPath, data.ToString(Formatting.None));
        }

        private static async Task SaveSplitAsync(JObject data, string outDir, int chunkSize)
        {
            Directory.CreateDirectory(outDir);
            var coreDir = Path.Combine(outDir, "core");
            if (Directory.Exists(coreDir)) Directory.Delete(coreDir, true);
            Directory.CreateDirectory(coreDir);

            var floor = (data["roomitemtypes"]?["furnitype"] as JArray)?.Cast<JObject>().ToList() ?? new List<JObject>();
            var wall  = (data["wallitemtypes"]?["furnitype"] as JArray)?.Cast<JObject>().ToList() ?? new List<JObject>();

            var coreFiles = new List<string>();

            // Floor chunks
            var floorChunks = ChunkList(floor, chunkSize);
            for (int i = 0; i < floorChunks.Count; i++)
            {
                var chunk = floorChunks[i];
                var fname = $"floor-{(i + 1):000}.json5";
                var content = new JObject
                {
                    ["roomitemtypes"] = new JObject { ["furnitype"] = new JArray(chunk) }
                };
                var header = $"Floor furniture {i * chunkSize + 1}..{i * chunkSize + chunk.Count} of {floor.Count}";
                await ManifestWriter.WritePartAsync(Path.Combine(coreDir, fname), content, asJson5: true, headerComment: header);
                coreFiles.Add(fname);
            }

            // Wall chunks
            var wallChunks = ChunkList(wall, chunkSize);
            for (int i = 0; i < wallChunks.Count; i++)
            {
                var chunk = wallChunks[i];
                var fname = $"wall-{(i + 1):000}.json5";
                var content = new JObject
                {
                    ["wallitemtypes"] = new JObject { ["furnitype"] = new JArray(chunk) }
                };
                var header = $"Wall furniture {i * chunkSize + 1}..{i * chunkSize + chunk.Count} of {wall.Count}";
                await ManifestWriter.WritePartAsync(Path.Combine(coreDir, fname), content, asJson5: true, headerComment: header);
                coreFiles.Add(fname);
            }

            var coreHeader = $"Auto-generated split of FurnitureData ({coreFiles.Count} files, floor={floor.Count} wall={wall.Count})";
            await ManifestWriter.WriteTierManifestAsync(coreDir, coreFiles, asJson5: true, headerComment: coreHeader);
            await ManifestWriter.WriteRootManifestAsync(outDir, asJson5: true);
        }

        private static List<List<T>> ChunkList<T>(List<T> source, int size)
        {
            var chunks = new List<List<T>>();
            for (int i = 0; i < source.Count; i += size)
                chunks.Add(source.GetRange(i, Math.Min(size, source.Count - i)));
            return chunks;
        }
    }
}
