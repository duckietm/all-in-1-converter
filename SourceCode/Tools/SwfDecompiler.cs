using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Habbo_Downloader.Tools
{
    public static class SwfDecompiler
    {
        private static readonly string BaseDecompileDir = Path.Combine("SWFCompiler", "decompile");
        private static readonly string BaseOutputDir = Path.Combine("SWFCompiler", "decompiled");

        public static async Task DecompileAsync()
        {
            try
            {
                Console.WriteLine("=================================================");
                Console.WriteLine("             SWF DECOMPILER                      ");
                Console.WriteLine("=================================================");
                Console.WriteLine("Extracts raw images, binaryData (XMLs), symbolClass,");
                Console.WriteLine("ActionScript source code (.as), and sounds from SWF.");
                Console.WriteLine("-------------------------------------------------");
                Console.WriteLine("Options:");
                Console.WriteLine(" [1] Auto: SWFCompiler/decompile/ (recursive)");
                Console.WriteLine(" [2] Imported Furniture (SWFCompiler/import/furniture)");
                Console.WriteLine(" [3] Imported Clothes (SWFCompiler/import/clothes)");
                Console.WriteLine(" [4] Imported Effects (SWFCompiler/import/effects)");
                Console.WriteLine(" [5] Imported Pets (SWFCompiler/import/pets)");
                Console.WriteLine(" [6] Habbo Default hof_furni (Habbo_Default/hof_furni)");
                Console.WriteLine(" [7] Custom folder path");
                Console.Write("Select source [Default is 1]: ");

                string choice = Console.ReadLine()?.Trim() ?? "1";
                string sourceDir = choice switch
                {
                    "2" => Path.Combine("SWFCompiler", "import", "furniture"),
                    "3" => Path.Combine("SWFCompiler", "import", "clothes"),
                    "4" => Path.Combine("SWFCompiler", "import", "effects"),
                    "5" => Path.Combine("SWFCompiler", "import", "pets"),
                    "6" => Path.Combine("Habbo_Default", "hof_furni"),
                    "7" => AskCustomDirectory(),
                    _ => BaseDecompileDir
                };

                if (!Directory.Exists(sourceDir))
                {
                    Directory.CreateDirectory(sourceDir);
                    Console.WriteLine($"📁 Created source folder: {sourceDir}");
                    Console.WriteLine($"ℹ️ Please drop your .swf files into '{sourceDir}' and run this tool again.");
                    return;
                }

                string[] swfFiles = Directory.GetFiles(sourceDir, "*.swf", SearchOption.AllDirectories);
                if (swfFiles.Length == 0)
                {
                    Console.WriteLine($"⚠️ No .swf files found in '{sourceDir}'.");
                    Console.WriteLine($"ℹ️ Place your SWF files inside '{sourceDir}' to decompile them.");
                    return;
                }

                Console.WriteLine($"🔍 Found {swfFiles.Length} SWF file(s) in {sourceDir}. Starting decompilation...");
                Directory.CreateDirectory(BaseOutputDir);

                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int maxParallelism = Math.Max(1, (int)(Environment.ProcessorCount * 0.8));
                int decompiledCount = 0;
                int failedCount = 0;

                await Parallel.ForEachAsync(swfFiles, new ParallelOptions { MaxDegreeOfParallelism = maxParallelism }, async (swfFile, _) =>
                {
                    string relativePath = Path.GetRelativePath(sourceDir, swfFile);
                    string relativeDir = Path.GetDirectoryName(relativePath) ?? "";
                    string swfName = Path.GetFileNameWithoutExtension(swfFile);

                    string targetFolder = Path.Combine(BaseOutputDir, relativeDir, swfName);
                    Directory.CreateDirectory(targetFolder);

                    if (await DecompileSingleSwfAsync(swfFile, targetFolder))
                    {
                        Interlocked.Increment(ref decompiledCount);
                        Console.WriteLine($"✅ Decompiled: {Path.GetFileName(swfFile)} -> {targetFolder}");
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                    }
                });

                stopwatch.Stop();

                ConversionSummaryPrinter.PrintSummary(
                    processTitle: "SWF Decompiler & Asset Extractor",
                    totalFiles: swfFiles.Length,
                    convertedFiles: decompiledCount,
                    skippedFiles: 0,
                    failedFiles: failedCount,
                    totalOriginalBytes: 0,
                    totalOutputBytes: 0,
                    elapsed: stopwatch.Elapsed,
                    outputDirectory: BaseOutputDir,
                    formatName: "Raw Sprites + XML + AS3 + Audio"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error during SWF decompilation: {ex.Message}");
            }
        }

        private static string AskCustomDirectory()
        {
            Console.Write("Enter custom directory path: ");
            string? input = Console.ReadLine()?.Trim();
            if (string.IsNullOrWhiteSpace(input)) return BaseDecompileDir;
            return input.Trim('"', '\'');
        }

        private static async Task<bool> DecompileSingleSwfAsync(string swfFilePath, string outputDir)
        {
            try
            {
                // First: fast native extraction for images and binary data
                try
                {
                    NativeSwfExtractor.Extract(swfFilePath, outputDir);
                }
                catch
                {
                    // Fallback handled by FFDec
                }

                // Second: run FFDec to extract all scripts (ActionScript), sounds, and complete symbolClass
                string ffdecArgs = $"-onerror ignore -export image,binarydata,symbolClass,script,sound \"{outputDir}\" \"{swfFilePath}\"";
                await RunFfdecCommandAsync(ffdecArgs);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to decompile {Path.GetFileName(swfFilePath)}: {ex.Message}");
                return false;
            }
        }

        private static async Task RunFfdecCommandAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = FfdecInvocation.BuildStartInfo(command)
            };

            process.Start();

            _ = Task.Run(async () => await process.StandardOutput.ReadToEndAsync());
            _ = Task.Run(async () => await process.StandardError.ReadToEndAsync());

            bool exited = await Task.Run(() => process.WaitForExit(90000)); // 90 seconds timeout
            if (!exited)
            {
                try { process.Kill(true); } catch { }
            }
        }
    }
}
