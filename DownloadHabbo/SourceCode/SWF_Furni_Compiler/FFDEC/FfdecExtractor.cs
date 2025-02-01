using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using Habbo_Downloader.SWFCompiler.Mapper.Assests;
using System.Xml.Linq;

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

            // Parse Debug.xml for asset mappings
            var assetMappings = DebugXmlParser.ParseDebugXml(debugXmlPath);

            // Rebuild images using the asset mappings from Debug.xml
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

        private static async Task<List<string>> ParseManifestFileAsync(string manifestFilePath)
        {
            if (!File.Exists(manifestFilePath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ERROR: Manifest file not found: {manifestFilePath}");
                Console.ResetColor();
                return new List<string>();
            }

            try
            {
                string manifestContent = await File.ReadAllTextAsync(manifestFilePath);
                XElement manifestElement = XElement.Parse(manifestContent);

                var assetOrder = manifestElement
                    .Descendants("asset")
                    .Where(a => a.Attribute("mimeType")?.Value == "image/png")
                    .Select(a => $"pura_mdl1_{a.Attribute("name")?.Value.ToLowerInvariant()}")
                    .ToList();

                return assetOrder;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ERROR: Failed to parse manifest file: {ex.Message}");
                Console.ResetColor();
                return new List<string>();
            }
        }

        public static async Task<Dictionary<string, string>> RebuildImagesAsync(string imageDir, Dictionary<string, string> assetMappings)
        {
            var outputMappings = new Dictionary<string, string>();

            // Move all PNG files to a temporary folder for processing.
            string tmpDir = Path.Combine(imageDir, "tmp");
            if (Directory.Exists(tmpDir))
            {
                Directory.Delete(tmpDir, recursive: true);
            }
            Directory.CreateDirectory(tmpDir);

            var allPngFiles = Directory.GetFiles(imageDir, "*.png", SearchOption.AllDirectories);
            foreach (var file in allPngFiles)
            {
                string relativePath = Path.GetRelativePath(imageDir, file);
                string destinationPath = Path.Combine(tmpDir, relativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Move(file, destinationPath);
            }

            string[] tmpFiles = Directory.GetFiles(tmpDir, "*.png", SearchOption.AllDirectories);
            var fileLookup = tmpFiles.ToDictionary(
                f => Path.GetFileNameWithoutExtension(f),
                f => f
            );

            string targetImagesFolder = Path.Combine(imageDir, "images");
            Directory.CreateDirectory(targetImagesFolder);

            // Process each asset mapping
            foreach (var kvp in assetMappings)
            {
                string tag = kvp.Key;
                string name = kvp.Value;

                // Locate the original file using the tag as key.
                fileLookup.TryGetValue(tag, out string? originalFilePath);
                if (originalFilePath == null)
                {
                    var possibleMatches = tmpFiles
                        .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(tag + "_", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (possibleMatches.Count > 0)
                    {
                        originalFilePath = possibleMatches[0];
                    }
                }
                if (originalFilePath == null)
                {
                    // No file found for this tag.
                    continue;
                }

                string originalExt = Path.GetExtension(originalFilePath);
                string sourceFilePath = Path.Combine(targetImagesFolder, $"{name}{originalExt}");

                // Copy the source file once.
                try
                {
                    File.Copy(originalFilePath, sourceFilePath, overwrite: false);
                    outputMappings[name] = sourceFilePath;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to copy {originalFilePath} to {sourceFilePath}: {ex.Message}");
                    continue;
                }
            }

            return outputMappings;
        }
    }
}
