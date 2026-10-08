using Habbo_Downloader.App.Workspaces;
using Habbo_Downloader.Tools;

namespace Habbo_Downloader.Compiler
{
    /// <summary>
    /// Hotel Tools (de)compile of .nitro and .hab bundles:
    ///   decompile  NitroCompiler/extract/&lt;type&gt;/{nitro,hab}/*  -> NitroCompiler/extracted/&lt;type&gt;/&lt;name&gt;/
    ///   compile    NitroCompiler/compile/&lt;type&gt;/&lt;name&gt;/     -> NitroCompiler/compiled/&lt;type&gt;/{nitro,hab}/
    /// Types: furni, clothing, effects, pets and generic (room, place_holder, ...).
    /// </summary>
    public static class BundleTools
    {
        private static readonly string[] AssetTypes = { "furni", "clothing", "effects", "pets", "generic" };

        public static async Task DecompileAsync(string extension)
        {
            Console.WriteLine($"Decompiling {extension} bundles...");
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int done = 0, failed = 0;
            long inputBytes = 0, outputBytes = 0;

            foreach (string assetType in AssetTypes)
            {
                string extractRoot = AssetDirectory(assetType, Path.Combine("NitroCompiler", "extract", assetType));
                string inputFolder = AssetBundleWriter.Folder(extractRoot, extension);
                Directory.CreateDirectory(inputFolder);

                // A file dropped straight into extract/<type>/ (the old layout) counts too.
                string[] files = Directory.GetFiles(inputFolder, "*" + extension)
                    .Concat(inputFolder == extractRoot ? Array.Empty<string>() : Directory.GetFiles(extractRoot, "*" + extension))
                    .ToArray();
                if (files.Length == 0) continue;

                Console.WriteLine($"Extracting {files.Length} {assetType} files...");

                foreach (string file in files)
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    try
                    {
                        byte[] data = await File.ReadAllBytesAsync(file);
                        var entries = HabBundle.IsHab(data) ? HabBundle.Read(data).Files : NitroToHabConverter.ReadNitro(data);

                        string outputFolder = Path.Combine("NitroCompiler", "extracted", assetType, name);
                        Directory.CreateDirectory(outputFolder);

                        // The files exactly as stored in the bundle.
                        foreach (var (entryName, bytes) in entries)
                        {
                            await File.WriteAllBytesAsync(Path.Combine(outputFolder, Path.GetFileName(entryName)), bytes);
                            outputBytes += bytes.Length;
                        }

                        inputBytes += data.Length;
                        done++;
                        Console.WriteLine($"✅ Extracted: {name}{extension} -> {outputFolder}");
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Console.WriteLine($"❌ {name}{extension}: {ex.Message}");
                    }
                }
            }

            stopwatch.Stop();
            if (done + failed == 0)
            {
                Console.WriteLine($"⚠️ No {extension} files found. Put them in NitroCompiler/extract/<type>/{extension.TrimStart('.')}/");
                Console.WriteLine($"   Types: {string.Join(", ", AssetTypes)}. The folders have been created for you.");
                return;
            }

            ConversionSummaryPrinter.PrintSummary(
                processTitle: $"Decompile {extension} bundles",
                totalFiles: done + failed,
                convertedFiles: done,
                skippedFiles: 0,
                failedFiles: failed,
                totalOriginalBytes: inputBytes,
                totalOutputBytes: outputBytes,
                elapsed: stopwatch.Elapsed,
                outputDirectory: Path.Combine("NitroCompiler", "extracted"),
                formatName: "json + sheet");
        }

        public static async Task CompileAsync(string extension)
        {
            AssetBundleWriter.Extension = extension;
            Console.WriteLine($"Compiling {AssetBundleWriter.Label} bundles...");

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            int compiled = 0, failed = 0;
            long outputBytes = 0;

            foreach (string assetType in AssetTypes)
            {
                string compileFolder = Path.Combine("NitroCompiler", "compile", assetType);
                string outputFolder = AssetDirectory(assetType, Path.Combine("NitroCompiler", "compiled", assetType));

                Directory.CreateDirectory(compileFolder);
                string[] items = Directory.GetDirectories(compileFolder);
                if (items.Length == 0) continue;

                Console.WriteLine($"Compiling {items.Length} {assetType} items...");

                foreach (string itemFolder in items)
                {
                    string name = Path.GetFileName(itemFolder);
                    try
                    {
                        var files = await AssetBundleWriter.ReadFilesAsync(Directory.GetFiles(itemFolder));
                        outputBytes += await AssetBundleWriter.WriteAsync(outputFolder, name, files,
                            (path, data) => WorkspaceOutput.WriteAllBytesAsync(path, data));
                        compiled++;
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Console.WriteLine($"❌ Error compiling {name} ({assetType}): {ex.Message}");
                    }
                }
            }

            stopwatch.Stop();
            if (compiled + failed == 0)
            {
                Console.WriteLine("⚠️ Nothing to compile. Put one folder per asset (json + sheet) in NitroCompiler/compile/<type>/<name>/");
                Console.WriteLine($"   Types: {string.Join(", ", AssetTypes)}. The folders have been created for you.");
                return;
            }

            ConversionSummaryPrinter.PrintSummary(
                processTitle: $"Compile {extension} bundles",
                totalFiles: compiled + failed,
                convertedFiles: compiled,
                skippedFiles: 0,
                failedFiles: failed,
                totalOriginalBytes: 0,
                totalOutputBytes: outputBytes,
                elapsed: stopwatch.Elapsed,
                outputDirectory: Path.Combine("NitroCompiler", "compiled"),
                formatName: AssetBundleWriter.Label);
        }

        /// <summary>The Asset Workspace folder of a type when one is set up, else the local folder.</summary>
        private static string AssetDirectory(string assetType, string fallback) => assetType switch
        {
            "furni" => AssetWorkspaceRuntime.Router.AssetDirectory(WorkspaceAssetKind.Furniture, fallback),
            "clothing" => AssetWorkspaceRuntime.Router.AssetDirectory(WorkspaceAssetKind.Clothing, fallback),
            "effects" => AssetWorkspaceRuntime.Router.AssetDirectory(WorkspaceAssetKind.Effects, fallback),
            "pets" => AssetWorkspaceRuntime.Router.AssetDirectory(WorkspaceAssetKind.Pets, fallback),
            _ => fallback
        };
    }
}
