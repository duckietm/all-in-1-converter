using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Habbo_Downloader.SWFCompiler.Mapper.Assests;

namespace Habbo_Downloader.Tools
{
    public static class FfdecExtractor
    {
        private const string ToolsDirectory = @"Tools\ffdec\ffdec.jar";

        public static async Task ExtractSWFAsync(string swfFilePath, string outputDirectory)
        {
            // Ensure output directory is clean
            ClearOutputDirectory(outputDirectory);

            // First command: extract assets, binary data, and symbolClass CSV.
            string commandExport = $"-export image,binarydata,symbolClass \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(commandExport);

            // Second command: export the SWF structure as XML for debugging.
            string debugXmlPath = Path.Combine(outputDirectory, "debug.xml");
            string commandXml = $"-swf2xml \"{swfFilePath}\" \"{debugXmlPath}\"";
            await RunFfdecCommandAsync(commandXml);

            // Remove unnecessary images (e.g., _32_ variants)
            RemoveUnwantedImages(outputDirectory, "_32_");

            // Parse Debug.xml for asset mappings.
            var assetMappings = DebugXmlParser.ParseDebugXml(debugXmlPath);

            // Rebuild images using the asset mappings from Debug.xml.
            await RebuildImagesAsync(outputDirectory, assetMappings);
        }

        private static async Task RunFfdecCommandAsync(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "java",
                    Arguments = $"-jar {ToolsDirectory} {command}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            await process.StandardOutput.ReadToEndAsync();
            await process.StandardError.ReadToEndAsync();
            process.WaitForExit();
        }

        private static void ClearOutputDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                var pngFiles = Directory.GetFiles(outputDirectory, "*.png", SearchOption.AllDirectories);
                foreach (var file in pngFiles)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error deleting {file}: {ex.Message}");
                    }
                }
            }
        }

        private static void RemoveUnwantedImages(string imageDir, string pattern)
        {
            foreach (var file in Directory.GetFiles(imageDir, $"*{pattern}*.png", SearchOption.AllDirectories))
            {
                File.Delete(file);
            }
        }

        public static async Task<Dictionary<string, string>> RebuildImagesAsync(string imageDir, Dictionary<string, string> assetMappings)
        {
            var outputMappings = new Dictionary<string, string>();

            // Move all PNG files to a temporary folder.
            string tmpDir = Path.Combine(imageDir, "tmp");
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, recursive: true);
            }
            Directory.CreateDirectory(tmpDir);

            Console.WriteLine("\nDEBUG: Files in temporary directory:");
            var allPngFiles = Directory.GetFiles(imageDir, "*.png", SearchOption.AllDirectories);
            foreach (var file in allPngFiles)
            {
                string relativePath = Path.GetRelativePath(imageDir, file);
                string destinationPath = Path.Combine(tmpDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Move(file, destinationPath);
                Console.WriteLine("  " + relativePath);
            }
            Console.WriteLine($"DEBUG: Total {allPngFiles.Length} files found in temporary directory.");

            // Build a lookup dictionary: key = file name (without extension), value = full path.
            string[] tmpFiles = Directory.GetFiles(tmpDir, "*.png", SearchOption.AllDirectories);
            var fileLookup = tmpFiles.ToDictionary(
                f => Path.GetFileNameWithoutExtension(f),
                f => f);

            // Prepare the target folder where files will be copied.
            string targetImagesFolder = Path.Combine(imageDir, "images");
            Directory.CreateDirectory(targetImagesFolder);

            // Process each asset mapping from Debug.xml.
            foreach (var kvp in assetMappings)
            {
                string tag = kvp.Key;   // the key from Debug.xml (object name)
                string name = kvp.Value; // the corresponding source name

                // Try to locate the file in the temporary folder.
                if (!fileLookup.TryGetValue(tag, out string? originalFilePath))
                {
                    Console.WriteLine($"WARNING: No file found for tag '{tag}'.");
                    continue;
                }

                string originalExt = Path.GetExtension(originalFilePath);
                // The file should be renamed using the source name from Debug.xml.
                string sourceFilePath = Path.Combine(targetImagesFolder, $"{name}{originalExt}");

                try
                {
                    File.Copy(originalFilePath, sourceFilePath, overwrite: false);
                    outputMappings[name] = sourceFilePath;
                    Console.WriteLine($"Copied file for '{name}' (from tag '{tag}').");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to copy {originalFilePath} to {sourceFilePath}: {ex.Message}");
                    continue;
                }
            }

            return outputMappings;
        }
    }
}
