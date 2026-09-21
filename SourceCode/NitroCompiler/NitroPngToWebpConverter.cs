using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace Habbo_Downloader.Compiler
{
    public static class NitroPngToWebpConverter
    {
        private static readonly string BaseInputDir = Path.Combine("NitroCompiler", "convert_webp");
        private static readonly string BaseOutputDir = Path.Combine("NitroCompiler", "converted_webp");

        public static async Task ConvertAsync()
        {
            try
            {
                Console.WriteLine("=================================================");
                Console.WriteLine("      NITRO PNG -> WEBP LOSSLESS CONVERTER       ");
                Console.WriteLine("=================================================");
                Console.WriteLine("Converts existing .nitro files containing PNG to ");
                Console.WriteLine("WebP Lossless (100% alpha transparency, 25-40% smaller).");
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine("Options:");
                Console.WriteLine(" [1] Auto: NitroCompiler/convert_webp/ (recursive)");
                Console.WriteLine(" [2] SWFCompiler/furniture/ (converted furniture)");
                Console.WriteLine(" [3] SWFCompiler/clothes/ (converted clothes)");
                Console.WriteLine(" [4] SWFCompiler/effects/ (converted effects)");
                Console.WriteLine(" [5] SWFCompiler/pets/ (converted pets)");
                Console.WriteLine(" [6] NitroCompiler/compiled/ (compiled bundles)");
                Console.WriteLine(" [7] Custom folder path");
                Console.Write("Select source [Default is 1]: ");

                string choice = Console.ReadLine()?.Trim() ?? "1";
                string sourceDir = choice switch
                {
                    "2" => Path.Combine("SWFCompiler", "furniture"),
                    "3" => Path.Combine("SWFCompiler", "clothes"),
                    "4" => Path.Combine("SWFCompiler", "effects"),
                    "5" => Path.Combine("SWFCompiler", "pets"),
                    "6" => Path.Combine("NitroCompiler", "compiled"),
                    "7" => AskCustomDirectory(),
                    _ => BaseInputDir
                };

                if (!Directory.Exists(sourceDir))
                {
                    Directory.CreateDirectory(sourceDir);
                    Console.WriteLine($"📁 Created source folder: {sourceDir}");
                    Console.WriteLine($"ℹ️ Please drop your .nitro files into '{sourceDir}' and run this tool again.");
                    return;
                }

                string[] nitroFiles = Directory.GetFiles(sourceDir, "*.nitro", SearchOption.AllDirectories);
                if (nitroFiles.Length == 0)
                {
                    Console.WriteLine($"⚠️ No .nitro files found in '{sourceDir}'.");
                    Console.WriteLine($"ℹ️ Place .nitro files inside '{sourceDir}' to convert them to WebP.");
                    return;
                }

                Console.WriteLine($"🔍 Found {nitroFiles.Length} .nitro file(s). Converting PNG textures to WebP Lossless...");
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int maxParallelism = Math.Max(1, (int)(Environment.ProcessorCount * 0.8));
                int convertedCount = 0;
                int skippedCount = 0;
                int failedCount = 0;
                long totalOriginalBytes = 0;
                long totalNewBytes = 0;
                int processedCount = 0;

                await Parallel.ForEachAsync(nitroFiles, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, async (nitroFile, _) =>
                {
                    string relativePath = Path.GetRelativePath(sourceDir, nitroFile);
                    string relativeDir = Path.GetDirectoryName(relativePath) ?? "";
                    string fileName = Path.GetFileName(nitroFile);

                    string outputDir = Path.Combine(BaseOutputDir, relativeDir);
                    Directory.CreateDirectory(outputDir);
                    string outputPath = Path.Combine(outputDir, fileName);

                    var result = await ConvertSingleNitroAsync(nitroFile, outputPath);
                    if (result.Success)
                    {
                        Interlocked.Increment(ref convertedCount);
                        Interlocked.Add(ref totalOriginalBytes, result.OriginalSize);
                        Interlocked.Add(ref totalNewBytes, result.NewSize);

                        double savingPercent = result.OriginalSize > 0
                            ? (1.0 - (double)result.NewSize / result.OriginalSize) * 100.0
                            : 0;

                        Console.WriteLine($"✅ {fileName}: {result.OriginalSize / 1024.0:F1} KB -> {result.NewSize / 1024.0:F1} KB (Saved {savingPercent:F1}%)");
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                    }

                    int current = Interlocked.Increment(ref processedCount);
                    if (current % 25 == 0 || current == nitroFiles.Length)
                    {
                        double pct = (double)current / nitroFiles.Length * 100.0;
                        double speed = current / Math.Max(0.001, stopwatch.Elapsed.TotalSeconds);
                        Console.WriteLine($"⚡ Progress: [{current}/{nitroFiles.Length}] ({pct:F1}%) | {speed:F1} items/sec");
                    }
                });

                stopwatch.Stop();

                Habbo_Downloader.Tools.ConversionSummaryPrinter.PrintSummary(
                    processTitle: "Nitro PNG -> Nitro WebP Lossless",
                    totalFiles: nitroFiles.Length,
                    convertedFiles: convertedCount,
                    skippedFiles: skippedCount,
                    failedFiles: failedCount,
                    totalOriginalBytes: totalOriginalBytes,
                    totalOutputBytes: totalNewBytes,
                    elapsed: stopwatch.Elapsed,
                    outputDirectory: BaseOutputDir,
                    formatName: "WebP Lossless (100% Quality, Method 6, Alpha Preserved)"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during Nitro WebP conversion: {ex.Message}");
            }
        }

        private static string AskCustomDirectory()
        {
            Console.Write("Enter custom directory path: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) return BaseInputDir;
            return input.Trim('"', '\'');
        }

        private record ConversionResult(bool Success, long OriginalSize, long NewSize);

        private static async Task<ConversionResult> ConvertSingleNitroAsync(string inputPath, string outputPath)
        {
            try
            {
                byte[] rawData = await File.ReadAllBytesAsync(inputPath);
                var filesInBundle = ExtractRawBundle(rawData);

                if (filesInBundle.Count == 0)
                {
                    return new ConversionResult(false, 0, 0);
                }

                var bundler = new NitroBundler();
                bool hasConvertedImage = false;

                // First pass: convert PNG images to WebP Lossless
                var newFiles = new Dictionary<string, byte[]>();

                foreach (var (name, bytes) in filesInBundle)
                {
                    if (name.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using var image = Image.Load<Rgba32>(bytes);
                            using var ms = new MemoryStream();
                            await image.SaveAsWebpAsync(ms, Tools.ConverterSettings.OptimalWebpEncoder);

                            string newName = Path.ChangeExtension(name, ".webp");
                            newFiles[newName] = ms.ToArray();
                            hasConvertedImage = true;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"⚠️ Failed to convert texture {name} in {Path.GetFileName(inputPath)}: {ex.Message}");
                            newFiles[name] = bytes;
                        }
                    }
                    else
                    {
                        newFiles[name] = bytes;
                    }
                }

                // Second pass: update JSON references from .png to .webp
                foreach (var (name, bytes) in newFiles)
                {
                    if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                    {
                        string jsonText = Encoding.UTF8.GetString(bytes);

                        // Replace image references in JSON metadata (e.g., "image": "foo.png" -> "image": "foo.webp")
                        string updatedJson = Regex.Replace(
                            jsonText,
                            @"(""image""\s*:\s*""[^""]+?)\.png""",
                            "$1.webp\"",
                            RegexOptions.IgnoreCase);

                        // Also replace any general name references
                        updatedJson = Regex.Replace(
                            updatedJson,
                            @"(""meta""\s*:\s*\{[^\}]*?""image""\s*:\s*""[^""]+?)\.png""",
                            "$1.webp\"",
                            RegexOptions.IgnoreCase);

                        byte[] updatedBytes = Encoding.UTF8.GetBytes(updatedJson);
                        bundler.AddFile(name, updatedBytes);
                    }
                    else
                    {
                        bundler.AddFile(name, bytes);
                    }
                }

                byte[] newNitroBytes = await bundler.ToBufferAsync();
                await File.WriteAllBytesAsync(outputPath, newNitroBytes);

                return new ConversionResult(true, rawData.Length, newNitroBytes.Length);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error converting {Path.GetFileName(inputPath)}: {ex.Message}");
                return new ConversionResult(false, 0, 0);
            }
        }

        private static Dictionary<string, byte[]> ExtractRawBundle(byte[] arrayBuffer)
        {
            var result = new Dictionary<string, byte[]>();

            using var memoryStream = new MemoryStream(arrayBuffer);
            using var binaryReader = new System.IO.BinaryReader(memoryStream);

            if (memoryStream.Length < 2) return result;

            short fileCount = BitConverter.ToInt16(binaryReader.ReadBytes(2).Reverse().ToArray(), 0);

            while (fileCount > 0 && memoryStream.Position < memoryStream.Length)
            {
                if (memoryStream.Position + 2 > memoryStream.Length) break;
                short fileNameLength = BitConverter.ToInt16(binaryReader.ReadBytes(2).Reverse().ToArray(), 0);
                if (fileNameLength <= 0 || fileNameLength > memoryStream.Length - memoryStream.Position) break;

                string fileName = Encoding.UTF8.GetString(binaryReader.ReadBytes(fileNameLength));

                if (memoryStream.Position + 4 > memoryStream.Length) break;
                int fileLength = BitConverter.ToInt32(binaryReader.ReadBytes(4).Reverse().ToArray(), 0);
                if (fileLength < 0 || fileLength > memoryStream.Length - memoryStream.Position) break;

                byte[] buffer = binaryReader.ReadBytes(fileLength);

                if (fileLength > 0)
                {
                    try
                    {
                        byte[] decompressed = Decompress(buffer);
                        result[fileName] = decompressed;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Failed to decompress {fileName}: {ex.Message}");
                    }
                }

                fileCount--;
            }

            return result;
        }

        private static byte[] Decompress(byte[] data)
        {
            if (data.Length > 2 && data[0] == 0x78 && data[1] == 0x9C)
            {
                byte[] rawDeflateData = data.Skip(2).ToArray();
                using var inputStream = new MemoryStream(rawDeflateData);
                using var outputStream = new MemoryStream();
                using (var deflateStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    deflateStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }

            try
            {
                using var inputStream = new MemoryStream(data);
                using var outputStream = new MemoryStream();
                using (var zlibStream = new DeflateStream(inputStream, CompressionMode.Decompress))
                {
                    zlibStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }
            catch
            {
                using var inputStream = new MemoryStream(data);
                using var outputStream = new MemoryStream();
                using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                {
                    gzipStream.CopyTo(outputStream);
                }
                return outputStream.ToArray();
            }
        }
    }
}
