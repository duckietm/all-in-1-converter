using System.Text.Json;
using System.Text.Json.Nodes;
using Habbo_Downloader.App.Workspaces;
using Habbo_Downloader.Tools;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Habbo_Downloader.Compiler
{
    /// <summary>
    /// Hotel Tools "Add size 32 to HAB furniture": Habbo's .hab furniture has no size 32 (the zoomed-out room).
    /// When a .nitro (or .hab) of the same furni has it, its size 32 sprites, assets and visualization are copied in.
    /// The 64 sprites and the rest of the json stay as they are; the 32 sprites are added below the sheet.
    /// Default: Habbo_Default/hof_furni/hab + SWFCompiler/furniture/nitro -> SWFCompiler/furniture/hab.
    /// </summary>
    public static class HabSize32Merger
    {
        private const int Gap = 1;

        public static async Task FurnitureAsync()
        {
            string baseDir = AssetWorkspaceRuntime.Router.AssetDirectory(WorkspaceAssetKind.Furniture, Path.Combine("SWFCompiler", "furniture"));
            string habDir = Ask("Habbo .hab furniture (without size 32)", HabboAssetFolder.Furniture.Hab);
            string donorDir = Ask("Furniture with size 32 (.nitro or .hab)", AssetBundleWriter.Folder(baseDir, ".nitro"));
            string outputDir = AssetBundleWriter.Folder(baseDir, ".hab");

            foreach (string folder in new[] { habDir, donorDir })
            {
                if (Directory.Exists(folder)) continue;
                Directory.CreateDirectory(folder);
                Console.WriteLine($"📁 Created {folder}; put the files there and run this tool again.");
                return;
            }

            var donors = Directory.GetFiles(donorDir, "*.*", SearchOption.AllDirectories)
                .Where(f => f.EndsWith(".nitro", StringComparison.OrdinalIgnoreCase) || f.EndsWith(".hab", StringComparison.OrdinalIgnoreCase))
                .GroupBy(f => Path.GetFileNameWithoutExtension(f), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            string[] habFiles = Directory.GetFiles(habDir, "*.hab", SearchOption.AllDirectories);
            if (habFiles.Length == 0)
            {
                Console.WriteLine($"⚠️ No .hab files found in '{habDir}'.");
                return;
            }

            Directory.CreateDirectory(outputDir);
            Console.WriteLine($"Adding size 32 to {habFiles.Length} .hab files from {donors.Count} files in {donorDir} -> {outputDir}");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int merged = 0, skipped = 0, failed = 0;
            long inputBytes = 0, outputBytes = 0;
            var reasons = new System.Collections.Concurrent.ConcurrentDictionary<string, int>();

            await Parallel.ForEachAsync(habFiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, async (habFile, _) =>
            {
                string name = Path.GetFileNameWithoutExtension(habFile);
                string outputPath = Path.Combine(outputDir, name + ".hab");
                try
                {
                    if (File.Exists(outputPath)) { Skip("already in the output folder"); return; }
                    if (!donors.TryGetValue(name, out string? donorFile)) { Skip("no file with size 32 of the same name"); return; }

                    byte[] hab = await File.ReadAllBytesAsync(habFile);
                    byte[]? result = Merge(hab, await File.ReadAllBytesAsync(donorFile), out string reason);
                    if (result == null) { Skip(reason); return; }

                    await WorkspaceOutput.WriteAllBytesAsync(outputPath, result);
                    Interlocked.Increment(ref merged);
                    Interlocked.Add(ref inputBytes, hab.Length);
                    Interlocked.Add(ref outputBytes, result.Length);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.WriteLine($"❌ {name}.hab: {ex.Message}");
                }

                void Skip(string why)
                {
                    Interlocked.Increment(ref skipped);
                    reasons.AddOrUpdate(why, 1, (_, n) => n + 1);
                }
            });

            stopwatch.Stop();
            foreach (var (why, count) in reasons.OrderByDescending(r => r.Value))
                Console.WriteLine($"   skipped {count}: {why}");

            ConversionSummaryPrinter.PrintSummary(
                processTitle: "Add size 32 to HAB furniture",
                totalFiles: habFiles.Length,
                convertedFiles: merged,
                skippedFiles: skipped,
                failedFiles: failed,
                totalOriginalBytes: inputBytes,
                totalOutputBytes: outputBytes,
                elapsed: stopwatch.Elapsed,
                outputDirectory: outputDir,
                formatName: "HAB with size 32");
        }

        private static string Ask(string what, string defaultPath)
        {
            Console.WriteLine($"{what}:");
            Console.WriteLine($" [1] {defaultPath}");
            Console.WriteLine(" [2] Custom folder path");
            Console.Write("Select [Default is 1]: ");
            if (Console.ReadLine()?.Trim() != "2") return defaultPath;
            Console.Write("Enter custom directory path: ");
            string? input = Console.ReadLine()?.Trim().Trim('"', '\'');
            return string.IsNullOrWhiteSpace(input) ? defaultPath : input;
        }

        /// <summary>
        /// The .hab with the size 32 of the donor bundle (.nitro or .hab) added, or null with the reason when it
        /// cannot be: the .hab has a size 32 already, the donor has none, or their 64 graphics differ.
        /// </summary>
        public static byte[]? Merge(byte[] hab, byte[] donor, out string reason)
        {
            var (bundleName, files) = HabBundle.Read(hab);
            var donorFiles = HabBundle.IsHab(donor) ? HabBundle.Read(donor).Files : NitroToHabConverter.ReadNitro(donor);

            int jsonIndex = files.FindIndex(f => f.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            var donorJsonFile = donorFiles.FirstOrDefault(f => f.Key.EndsWith(".json", StringComparison.OrdinalIgnoreCase));
            if (jsonIndex < 0 || donorJsonFile.Value == null) { reason = "no json in the bundle"; return null; }

            JsonObject json = JsonNode.Parse(files[jsonIndex].Value)!.AsObject();
            JsonObject donorJson = JsonNode.Parse(donorJsonFile.Value)!.AsObject();
            string name = json["name"]?.GetValue<string>() ?? bundleName;
            string donorName = donorJson["name"]?.GetValue<string>() ?? name;

            if (Visualization(json, 32) != null) { reason = "already has size 32"; return null; }
            JsonObject? donor32 = Visualization(donorJson, 32);
            JsonObject? donor64 = Visualization(donorJson, 64);
            JsonObject? hab64 = Visualization(json, 64);
            if (donor32 == null || donor64 == null || hab64 == null) { reason = "the other file has no size 32"; return null; }

            // Same furni version: the 32 art must belong to these 64 graphics.
            JsonObject assets = json["assets"]!.AsObject();
            JsonObject donorAssets = donorJson["assets"]!.AsObject();
            var assets64 = assets.Select(a => a.Key).Where(k => k.Contains("_64_")).ToHashSet();
            var donorAssets64 = donorAssets.Select(a => a.Key).Where(k => k.Contains("_64_")).ToHashSet();
            if (!assets64.SetEquals(donorAssets64) || LayerCount(hab64) != LayerCount(donor64) || LayerCount(donor32) != LayerCount(hab64))
            {
                reason = "the 64 graphics differ (another version of the furni)";
                return null;
            }

            int imageIndex = files.FindIndex(f => f.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.Key.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
            var donorImageFile = donorFiles.FirstOrDefault(f => f.Key.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || f.Key.EndsWith(".webp", StringComparison.OrdinalIgnoreCase));
            if (imageIndex < 0 || donorImageFile.Value == null) { reason = "no sprite sheet in the bundle"; return null; }

            JsonObject frames = json["spritesheet"]!["frames"]!.AsObject();
            JsonObject donorFrames = donorJson["spritesheet"]!["frames"]!.AsObject();
            var assets32 = donorAssets.Where(a => a.Key.Contains("_32_")).ToList();

            // The 32 sprites with their own image; the others point at one of these with "source".
            var sprites = new List<(string Asset, Rectangle Area)>();
            foreach (var (assetName, asset) in assets32)
            {
                if (asset?["source"] != null) continue;
                if (donorFrames[donorName + "_" + assetName]?["frame"] is not JsonObject frame) { reason = $"{assetName} has no sprite"; return null; }
                sprites.Add((assetName, new Rectangle(frame["x"]!.GetValue<int>(), frame["y"]!.GetValue<int>(), frame["w"]!.GetValue<int>(), frame["h"]!.GetValue<int>())));
            }

            using var sheet = Image.Load<Rgba32>(files[imageIndex].Value);
            using var donorSheet = Image.Load<Rgba32>(donorImageFile.Value);

            // Shelf-pack the 32 sprites below the existing sheet, tallest first.
            int width = Math.Max(sheet.Width, sprites.Count == 0 ? 0 : sprites.Max(s => s.Area.Width));
            var placed = new Dictionary<string, Point>();
            int x = 0, y = sheet.Height + Gap, rowHeight = 0;
            foreach (var (assetName, area) in sprites.OrderByDescending(s => s.Area.Height).ThenBy(s => s.Asset, StringComparer.Ordinal))
            {
                if (x > 0 && x + area.Width > width) { x = 0; y += rowHeight + Gap; rowHeight = 0; }
                placed[assetName] = new Point(x, y);
                x += area.Width + Gap;
                rowHeight = Math.Max(rowHeight, area.Height);
            }
            int height = sprites.Count == 0 ? sheet.Height : y + rowHeight;

            using var merged = new Image<Rgba32>(width, height);
            merged.Mutate(c => c.DrawImage(sheet, new Point(0, 0), 1f));
            foreach (var (assetName, area) in sprites)
            {
                using var sprite = donorSheet.Clone(c => c.Crop(area));
                merged.Mutate(c => c.DrawImage(sprite, placed[assetName], 1f));
                frames[name + "_" + assetName] = Frame(placed[assetName], area.Width, area.Height);
            }

            foreach (var (assetName, asset) in assets32) assets[assetName] = asset!.DeepClone();
            json["visualizations"]!.AsArray().Add(donor32.DeepClone());

            // Same image format as before: a WebP sheet stays WebP (lossless, pixel-checked).
            string imageName = files[imageIndex].Key;
            byte[] png;
            using (var output = new MemoryStream())
            {
                merged.SaveAsPng(output);
                png = output.ToArray();
            }
            byte[] image = png;
            if (imageName.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) && SheetWebp.TryConvert(png, name, out byte[] webp, out _)) image = webp;
            else imageName = Path.ChangeExtension(imageName, ".png");

            JsonObject meta = json["spritesheet"]!["meta"]!.AsObject();
            meta["image"] = Path.GetFileName(imageName);
            meta["size"] = new JsonObject { ["w"] = width, ["h"] = height };

            files[jsonIndex] = new(files[jsonIndex].Key, JsonSerializer.SerializeToUtf8Bytes(json));
            files[imageIndex] = new(imageName, image);

            reason = $"added {assets32.Count} size 32 assets";
            return HabBundle.Write(bundleName, files);
        }

        private static JsonObject? Visualization(JsonObject json, int size) =>
            json["visualizations"]?.AsArray().OfType<JsonObject>().FirstOrDefault(v => v["size"]?.GetValue<int>() == size);

        private static int LayerCount(JsonObject visualization) => visualization["layerCount"]?.GetValue<int>() ?? 0;

        private static JsonObject Frame(Point at, int w, int h) => new()
        {
            ["frame"] = new JsonObject { ["x"] = at.X, ["y"] = at.Y, ["w"] = w, ["h"] = h },
            ["rotated"] = false,
            ["trimmed"] = false,
            ["spriteSourceSize"] = new JsonObject { ["x"] = 0, ["y"] = 0, ["w"] = w, ["h"] = h },
            ["sourceSize"] = new JsonObject { ["w"] = w, ["h"] = h },
            ["pivot"] = new JsonObject { ["x"] = 0.5, ["y"] = 0.5 }
        };
    }
}
