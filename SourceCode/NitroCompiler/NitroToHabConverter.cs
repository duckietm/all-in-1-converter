using System.IO.Compression;
using System.Text;
using Habbo_Downloader.App.Workspaces;

namespace Habbo_Downloader.Compiler
{
    /// <summary>
    /// Hotel Tools "Nitro ... to HAB": repacks the .nitro bundles of one asset type as .hab, with the same json
    /// and a lossless WebP sheet (a PNG sheet is converted, pixel-checked). The .nitro files are left untouched.
    /// Default: SWFCompiler/&lt;type&gt;/nitro -> SWFCompiler/&lt;type&gt;/hab.
    /// </summary>
    public static class NitroToHabConverter
    {
        public static Task FurnitureAsync() => ConvertAsync("Furniture", WorkspaceAssetKind.Furniture, Path.Combine("SWFCompiler", "furniture"));

        public static Task ClothesAsync() => ConvertAsync("Clothes", WorkspaceAssetKind.Clothing, Path.Combine("SWFCompiler", "clothes"));

        public static Task PetsAsync() => ConvertAsync("Pets", WorkspaceAssetKind.Pets, Path.Combine("SWFCompiler", "pets"));

        public static Task EffectsAsync() => ConvertAsync("Effects", WorkspaceAssetKind.Effects, Path.Combine("SWFCompiler", "effects"));

        private static async Task ConvertAsync(string label, WorkspaceAssetKind kind, string fallback)
        {
            string baseDir = AssetWorkspaceRuntime.Router.AssetDirectory(kind, fallback);
            string defaultSource = AssetBundleWriter.Folder(baseDir, ".nitro");

            Console.WriteLine($"Nitro {label} to HAB: json + WebP Lossless, output {AssetBundleWriter.Folder(baseDir, ".hab")}");
            Console.WriteLine($" [1] {defaultSource}");
            Console.WriteLine(" [2] Custom folder path");
            Console.Write("Select source [Default is 1]: ");

            string sourceDir = defaultSource;
            if (Console.ReadLine()?.Trim() == "2")
            {
                Console.Write("Enter custom directory path: ");
                string? input = Console.ReadLine()?.Trim().Trim('"', '\'');
                if (!string.IsNullOrWhiteSpace(input)) sourceDir = input;
            }

            AssetBundleWriter.Extension = ".hab";
            await ConvertDirectoryAsync(label, sourceDir, baseDir);
        }

        private static async Task ConvertDirectoryAsync(string label, string sourceDir, string baseDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                Directory.CreateDirectory(sourceDir);
                Console.WriteLine($"📁 Created source folder: {sourceDir}");
                Console.WriteLine($"ℹ️ Drop your .nitro files into '{sourceDir}' and run this tool again.");
                return;
            }

            string[] nitroFiles = Directory.GetFiles(sourceDir, "*.nitro", SearchOption.AllDirectories);
            if (nitroFiles.Length == 0)
            {
                Console.WriteLine($"⚠️ No .nitro files found in '{sourceDir}'.");
                return;
            }

            string outputDir = AssetBundleWriter.Folder(baseDir, ".hab");
            Directory.CreateDirectory(outputDir);

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int converted = 0, skipped = 0, failed = 0;
            long originalBytes = 0, outputBytes = 0;

            await Parallel.ForEachAsync(nitroFiles, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount - 1) }, async (nitroFile, _) =>
            {
                string name = Path.GetFileNameWithoutExtension(nitroFile);
                string habPath = Path.Combine(outputDir, name + ".hab");

                if (File.Exists(habPath))
                {
                    Interlocked.Increment(ref skipped);
                    return;
                }

                try
                {
                    byte[] nitro = await File.ReadAllBytesAsync(nitroFile);
                    byte[] hab = await AssetBundleWriter.BuildAsync(".hab", name, ReadNitro(nitro));

                    await WorkspaceOutput.WriteAllBytesAsync(habPath, hab);

                    Interlocked.Increment(ref converted);
                    Interlocked.Add(ref originalBytes, nitro.Length);
                    Interlocked.Add(ref outputBytes, hab.Length);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failed);
                    Console.WriteLine($"❌ {name}.nitro: {ex.Message}");
                }
            });

            stopwatch.Stop();

            Tools.ConversionSummaryPrinter.PrintSummary(
                processTitle: $"Nitro {label} -> HAB",
                totalFiles: nitroFiles.Length,
                convertedFiles: converted,
                skippedFiles: skipped,
                failedFiles: failed,
                totalOriginalBytes: originalBytes,
                totalOutputBytes: outputBytes,
                elapsed: stopwatch.Elapsed,
                outputDirectory: outputDir,
                formatName: AssetBundleWriter.Label);
        }

        /// <summary>Every file of a .nitro; a damaged bundle throws instead of giving a .hab with files missing.</summary>
        public static List<KeyValuePair<string, byte[]>> ReadNitro(byte[] data)
        {
            var files = new List<KeyValuePair<string, byte[]>>();
            int position = 0;

            int count = ReadUInt16BigEndian(data, ref position);
            for (int i = 0; i < count; i++)
            {
                int nameLength = ReadUInt16BigEndian(data, ref position);
                string fileName = Encoding.UTF8.GetString(Take(data, ref position, nameLength));

                int length = ReadInt32BigEndian(data, ref position);
                if (length < 0) throw new InvalidDataException($"Invalid length for {fileName}.");

                files.Add(new KeyValuePair<string, byte[]>(fileName, Decompress(Take(data, ref position, length))));
            }

            if (files.Count == 0) throw new InvalidDataException("The .nitro bundle is empty.");
            return files;
        }

        private static int ReadUInt16BigEndian(byte[] data, ref int position)
        {
            byte[] bytes = Take(data, ref position, 2);
            return (bytes[0] << 8) | bytes[1];
        }

        private static int ReadInt32BigEndian(byte[] data, ref int position)
        {
            byte[] bytes = Take(data, ref position, 4);
            return (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
        }

        private static byte[] Take(byte[] data, ref int position, int length)
        {
            if (length < 0 || position + length > data.Length) throw new InvalidDataException("The .nitro bundle is cut short.");
            byte[] bytes = data.AsSpan(position, length).ToArray();
            position += length;
            return bytes;
        }

        /// <summary>Nitro bundles hold gzip (our converter) or zlib (the original Nitro tools) streams.</summary>
        private static byte[] Decompress(byte[] data)
        {
            Stream input = new MemoryStream(data);
            Stream stream = data.Length > 1 && data[0] == 0x1F && data[1] == 0x8B
                ? new GZipStream(input, CompressionMode.Decompress)
                : data.Length > 0 && data[0] == 0x78
                    ? new ZLibStream(input, CompressionMode.Decompress)
                    : new DeflateStream(input, CompressionMode.Decompress);

            using (stream)
            using (var output = new MemoryStream())
            {
                stream.CopyTo(output);
                return output.ToArray();
            }
        }
    }
}
