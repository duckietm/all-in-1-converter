using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Habbo_Downloader.Compiler;
using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using Habbo_Downloader.SWFCompiler.Mapper.Spritesheets;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Tools
{
    public static class BenchmarkRunner
    {
        public static async Task RunAsync(int sampleCount = 30)
        {
            Console.WriteLine("===============================================================================");
            Console.WriteLine("        🚀 HABBO ALL-IN-1: WEBP LOSSLESS BENCHMARK & TEST SUITE                ");
            Console.WriteLine("===============================================================================");

            string sourceDir = Path.Combine("Habbo_Default", "hof_furni");
            if (!Directory.Exists(sourceDir))
            {
                Console.WriteLine($"❌ Source directory '{sourceDir}' does not exist!");
                return;
            }

            var allSwfFiles = Directory.GetFiles(sourceDir, "*.swf");
            if (allSwfFiles.Length == 0)
            {
                Console.WriteLine($"❌ No SWF files found in '{sourceDir}'!");
                return;
            }

            Console.WriteLine($"📦 Total SWF files in {sourceDir}: {allSwfFiles.Length:N0}");
            Console.WriteLine($"🔬 Benchmarking {sampleCount} diverse furniture assets (PNG vs WebP Lossless)...\n");

            // Select diverse sample across the whole alphabetical range
            var sampleFiles = allSwfFiles
                .OrderBy(f => new FileInfo(f).Length)
                .Where((_, index) => index % Math.Max(1, allSwfFiles.Length / sampleCount) == 0)
                .Take(sampleCount)
                .ToArray();

            long totalSwfBytes = 0;
            long totalPngNitroBytes = 0;
            long totalWebpNitroBytes = 0;
            long totalPngSheetBytes = 0;
            long totalWebpSheetBytes = 0;
            int pixelPerfectCount = 0;
            int testedPixelCount = 0;

            var tableRows = new List<(string Name, long SwfSize, long PngSheet, long WebpSheet, double SavingPct, bool PixelMatch)>();
            var sw = Stopwatch.StartNew();

            string testWorkDir = Path.Combine(Path.GetTempPath(), "habbo_benchmark_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(testWorkDir);

            for (int i = 0; i < sampleFiles.Length; i++)
            {
                string swfFile = sampleFiles[i];
                string name = Path.GetFileNameWithoutExtension(swfFile);
                long swfSize = new FileInfo(swfFile).Length;
                totalSwfBytes += swfSize;

                string furniDir = Path.Combine(testWorkDir, name);
                string binaryDir = Path.Combine(furniDir, $"{name}_binaryData");
                Directory.CreateDirectory(furniDir);

                try
                {
                    // 1. Extract SWF assets
                    await FfdecExtractor.ExtractSWFAsync(swfFile, binaryDir);
                    if (!Directory.Exists(Path.Combine(binaryDir, "binaryData")))
                        continue;

                    string csvPath = Path.Combine(binaryDir, "symbolClass", "symbols.csv");
                    var canonicalMapping = AssetNameMapper.BuildCanonicalMapping(csvPath);

                    string imagesDirectory = Path.Combine(binaryDir, "images");
                    string tmpDirectory = Path.Combine(binaryDir, "tmp");
                    await ImageRestorer.RestoreImagesFromTmpAsync(tmpDirectory, imagesDirectory, AssetsMapper.LatestImageMapping);
                    var images = LoadImages(imagesDirectory);

                    if (images.Count == 0) continue;

                    // 2. Generate PNG Spritesheet
                    ConverterSettings.SpritesheetFormat = "png";
                    string pngDir = Path.Combine(furniDir, "png_out");
                    Directory.CreateDirectory(pngDir);
                    var (pngSheetPath, _) = SpriteSheetMapper.GenerateSpriteSheet(
                        images, pngDir, name, canonicalMapping, false, 10, 11266, 12800);
                    long pngSheetSize = pngSheetPath != null && File.Exists(pngSheetPath) ? new FileInfo(pngSheetPath).Length : 0;

                    // 3. Generate WebP Spritesheet
                    ConverterSettings.SpritesheetFormat = "webp";
                    string webpDir = Path.Combine(furniDir, "webp_out");
                    Directory.CreateDirectory(webpDir);
                    var (webpSheetPath, _) = SpriteSheetMapper.GenerateSpriteSheet(
                        images, webpDir, name, canonicalMapping, false, 10, 11266, 12800);
                    long webpSheetSize = webpSheetPath != null && File.Exists(webpSheetPath) ? new FileInfo(webpSheetPath).Length : 0;

                    // 4. Pixel match check (Bit-for-bit RGBA pixel comparison)
                    bool pixelMatch = true;
                    if (pngSheetPath != null && File.Exists(pngSheetPath) && webpSheetPath != null && File.Exists(webpSheetPath))
                    {
                        using var imgPng = Image.Load<Rgba32>(pngSheetPath);
                        using var imgWebp = Image.Load<Rgba32>(webpSheetPath);
                        testedPixelCount++;

                        if (imgPng.Width != imgWebp.Width || imgPng.Height != imgWebp.Height)
                        {
                            pixelMatch = false;
                        }
                        else
                        {
                            for (int y = 0; y < imgPng.Height && pixelMatch; y++)
                            {
                                for (int x = 0; x < imgPng.Width; x++)
                                {
                                    var p1 = imgPng[x, y];
                                    var p2 = imgWebp[x, y];
                                    if (p1.R != p2.R || p1.G != p2.G || p1.B != p2.B || p1.A != p2.A)
                                    {
                                        pixelMatch = false;
                                        break;
                                    }
                                }
                            }
                        }

                        if (pixelMatch) pixelPerfectCount++;
                    }

                    // Estimate Nitro bundle sizes (JSON metadata ~1.5 KB + texture)
                    long pngNitroSize = pngSheetSize + 1500;
                    long webpNitroSize = webpSheetSize + 1500;

                    totalPngNitroBytes += pngNitroSize;
                    totalWebpNitroBytes += webpNitroSize;
                    totalPngSheetBytes += pngSheetSize;
                    totalWebpSheetBytes += webpSheetSize;

                    double savingPct = pngSheetSize > 0 ? (1.0 - (double)webpSheetSize / pngSheetSize) * 100.0 : 0.0;
                    tableRows.Add((name, swfSize, pngSheetSize, webpSheetSize, savingPct, pixelMatch));

                    Console.WriteLine($"  [{i + 1:D2}/{sampleFiles.Length:D2}] {name,-28} | PNG: {pngSheetSize / 1024.0,6:F1} KB -> WebP: {webpSheetSize / 1024.0,6:F1} KB | -{savingPct,5:F1}% | {(pixelMatch ? "✅ 100% Lossless RGBA" : "❌ Diff")}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  [{i + 1:D2}/{sampleFiles.Length:D2}] {name,-28} | ❌ Error: {ex.Message}");
                }
            }

            sw.Stop();

            // Reset back to webp
            ConverterSettings.SpritesheetFormat = "webp";

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine("                            📊 BENCHMARK RESULTS                               ");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"Sample furniture tested:      {tableRows.Count}");
            Console.WriteLine($"Total Sample PNG size:        {totalPngSheetBytes / (1024.0 * 1024.0):F2} MB");
            Console.WriteLine($"Total Sample WebP size:       {totalWebpSheetBytes / (1024.0 * 1024.0):F2} MB");

            long sheetSavedBytes = totalPngSheetBytes - totalWebpSheetBytes;
            double sheetSavedPct = totalPngSheetBytes > 0 ? ((double)sheetSavedBytes / totalPngSheetBytes) * 100.0 : 0.0;
            Console.WriteLine($"Texture Space Saved:          {sheetSavedBytes / (1024.0 * 1024.0):F2} MB (-{sheetSavedPct:F1}%)");
            Console.WriteLine($"Pixel Perfection Check:       {pixelPerfectCount}/{testedPixelCount} ({(testedPixelCount > 0 ? (double)pixelPerfectCount / testedPixelCount * 100.0 : 100):F1}% bit-for-bit exact RGBA match)");
            Console.WriteLine($"Speed:                        {sw.Elapsed.TotalSeconds:F2}s total ({sw.Elapsed.TotalMilliseconds / Math.Max(1, tableRows.Count):F0} ms/item)");

            // Extrapolate for full 13,719 items
            int totalFurniCount = allSwfFiles.Length;
            double avgPngBytes = tableRows.Count > 0 ? (double)totalPngSheetBytes / tableRows.Count : 0;
            double avgWebpBytes = tableRows.Count > 0 ? (double)totalWebpSheetBytes / tableRows.Count : 0;
            double projectedPngTotalMb = (avgPngBytes * totalFurniCount) / (1024.0 * 1024.0);
            double projectedWebpTotalMb = (avgWebpBytes * totalFurniCount) / (1024.0 * 1024.0);
            double projectedSavingMb = projectedPngTotalMb - projectedWebpTotalMb;

            Console.WriteLine("\n===============================================================================");
            Console.WriteLine($"      🔮 PROJECTION FOR FULL CATALOG ({totalFurniCount:N0} FURNITURE ITEMS)      ");
            Console.WriteLine("===============================================================================");
            Console.WriteLine($"Full Catalog with PNG:        ~{projectedPngTotalMb:F1} MB (~{projectedPngTotalMb / 1024.0:F2} GB)");
            Console.WriteLine($"Full Catalog with WebP:       ~{projectedWebpTotalMb:F1} MB (~{projectedWebpTotalMb / 1024.0:F2} GB)");
            Console.WriteLine($"⚡ Total Bandwidth & Disk Saved: ~{projectedSavingMb:F1} MB (~{projectedSavingMb / 1024.0:F2} GB Saved! -{sheetSavedPct:F1}%)");
            Console.WriteLine("===============================================================================\n");

            try { Directory.Delete(testWorkDir, true); } catch { }
        }

        private static Dictionary<string, Image<Rgba32>> LoadImages(string dir)
        {
            var dict = new Dictionary<string, Image<Rgba32>>();
            if (!Directory.Exists(dir)) return dict;
            foreach (var f in Directory.GetFiles(dir, "*.png"))
            {
                try
                {
                    string name = Path.GetFileNameWithoutExtension(f);
                    dict[name] = Image.Load<Rgba32>(f);
                }
                catch { }
            }
            return dict;
        }
    }
}
