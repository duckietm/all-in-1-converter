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

            // Extract SWF assets (images, binary data, and symbolClass)
            string commandImages = $"-export image,binarydata,symbolClass \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(commandImages);

            // Remove unnecessary images (e.g., _32_ variants)
            RemoveUnwantedImages(outputDirectory, "_32_");

            // Parse assets before rebuilding images
            string csvFile = Path.Combine(outputDirectory, "symbolClass", "symbols.csv");
            var assetMappings = await AssetsMapper.LoadImageSourcesFromCSV(csvFile);

            // Rebuild images AFTER asset sources are mapped
            await RebuildImagesAsync(outputDirectory, csvFile, assetMappings);
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

        public static async Task<Dictionary<string, string>> RebuildImagesAsync(string imageDir, string csvFile, Dictionary<string, string> imageSources)
        {
            var assetMappings = new Dictionary<string, string>();

            if (!File.Exists(csvFile))
            {
                Console.WriteLine($"CSV not found: {csvFile}");
                return assetMappings;
            }

            // Find the manifest file dynamically.
            var manifestFiles = Directory.GetFiles(Path.Combine(imageDir, "binaryData"), "*_manifest.bin", SearchOption.TopDirectoryOnly);
            if (manifestFiles.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ ERROR: Manifest file not found in {Path.Combine(imageDir, "binaryData")}");
                Console.ResetColor();
                return assetMappings;
            }

            string manifestFilePath = manifestFiles[0];
            var manifestOrder = await ParseManifestFileAsync(manifestFilePath);

            // Build mapping dictionaries from the CSV.
            var lines = await File.ReadAllLinesAsync(csvFile);
            var idToNamesMap = new Dictionary<string, List<string>>();
            var nameToCsvLineMap = new Dictionary<string, string>();

            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;

                string id = parts[0].Trim();
                string name = parts[1].Trim();

                // Ignore unwanted rows.
                if (id == "0" || name == "*" || name.Contains("_32_"))
                    continue;

                if (!idToNamesMap.ContainsKey(id))
                {
                    idToNamesMap[id] = new List<string>();
                }
                idToNamesMap[id].Add(name);
                nameToCsvLineMap[name] = line;
            }

            // Reorder the CSV lines based on the manifest order.
            var orderedCsvLines = new List<string>();
            foreach (var assetName in manifestOrder)
            {
                if (nameToCsvLineMap.ContainsKey(assetName))
                {
                    orderedCsvLines.Add(nameToCsvLineMap[assetName]);
                }
            }

            await File.WriteAllLinesAsync(csvFile, orderedCsvLines);

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

            // Process each group of asset names for a given ID.
            foreach (var kvp in idToNamesMap)
            {
                string id = kvp.Key;
                List<string> requestedNames = kvp.Value;

                // Locate the original file using the ID as key.
                fileLookup.TryGetValue(id, out string? originalFilePath);
                if (originalFilePath == null)
                {
                    var possibleMatches = tmpFiles
                        .Where(f => Path.GetFileNameWithoutExtension(f).StartsWith(id + "_", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (possibleMatches.Count > 0)
                    {
                        originalFilePath = possibleMatches[0];
                    }
                }
                if (originalFilePath == null)
                {
                    // No file found for this ID.
                    continue;
                }

                string originalExt = Path.GetExtension(originalFilePath);
                // Choose the preferred asset name.
                string preferredName = requestedNames.FirstOrDefault(n => n.Contains("0_0")) ?? requestedNames[0];

                // Helper function to clean up asset names.
                string CleanName(string name)
                {
                    string cleaned = name;
                    if (cleaned.StartsWith($"{id}_"))
                    {
                        cleaned = cleaned.Substring(id.Length + 1);
                    }
                    cleaned = Regex.Replace(cleaned, "(_{2,})", "_");
                    return cleaned;
                }

                string preferredCleanName = CleanName(preferredName);
                string sourceFilePath = Path.Combine(targetImagesFolder, $"{preferredCleanName}{originalExt}");

                // Copy the source file once.
                try
                {
                    File.Copy(originalFilePath, sourceFilePath, overwrite: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to copy {originalFilePath} to {sourceFilePath}: {ex.Message}");
                    continue;
                }

                // Update mapping for each asset name for this ID.
                foreach (var name in requestedNames)
                {
                    assetMappings[name] = preferredCleanName;
                }
            }

            return assetMappings;
        }
    }
}
