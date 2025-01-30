using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO;
using Habbo_Downloader.SWFCompiler.Mapper.Assests;

namespace Habbo_Downloader.Tools
{
    public static class FfdecExtractor
    {
        private const string ToolsDirectory = @"Tools\ffdec\ffdec.jar";

        public static async Task ExtractSWFAsync(string swfFilePath, string outputDirectory)
        {
            // 1️⃣ 🔥 **Ensure output directory is clean**
            ClearOutputDirectory(outputDirectory);

            // 2️⃣ 🏗 **Extract SWF assets (images, binary data, and symbolClass)**
            string commandImages = $"-export image,binarydata,symbolClass \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(commandImages);

            // 3️⃣ 🗑 **Remove unnecessary images (e.g., _32_ variants)**
            RemoveUnwantedImages(outputDirectory, "_32_");

            // 4️⃣ 📄 **Parse assets before rebuilding images**
            string csvFile = Path.Combine(outputDirectory, "symbolClass", "symbols.csv");
            var assetMappings = await AssetsMapper.LoadImageSourcesFromCSV(csvFile);

            // 5️⃣ 🎨 **Rebuild images AFTER asset sources are mapped**
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
                Console.WriteLine("✅ Cleared old PNG files from output directory.");
            }
        }

        private static void RemoveUnwantedImages(string imageDir, string pattern)
        {
            foreach (var file in Directory.GetFiles(imageDir, $"*{pattern}*.png", SearchOption.AllDirectories))
            {
                File.Delete(file);
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

            var lines = await File.ReadAllLinesAsync(csvFile);
            var idToNamesMap = new Dictionary<string, List<string>>();

            foreach (var line in lines)
            {
                var parts = line.Split(';');
                if (parts.Length < 2) continue;

                string id = parts[0].Trim();
                string name = parts[1].Trim();

                if (id == "0" || name == "*" || name.Contains("_32_"))
                    continue;

                if (!idToNamesMap.ContainsKey(id))
                {
                    idToNamesMap[id] = new List<string>();
                }
                idToNamesMap[id].Add(name);
            }

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

            foreach (var kvp in idToNamesMap)
            {
                string id = kvp.Key;
                List<string> requestedNames = kvp.Value;

                fileLookup.TryGetValue(id, out string? originalFilePath);

                if (originalFilePath == null)
                {
                    var possibleMatches = tmpFiles
                        .Where(f => Path.GetFileNameWithoutExtension(f)
                            .StartsWith(id + "_", StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (possibleMatches.Count > 0)
                    {
                        originalFilePath = possibleMatches[0];
                    }
                }

                if (originalFilePath == null)
                {
                    continue;
                }

                string originalExt = Path.GetExtension(originalFilePath);
                string? firstNewFilePath = null;

                for (int i = 0; i < requestedNames.Count; i++)
                {
                    string cleanName = requestedNames[i];

                    if (cleanName.StartsWith($"{id}_"))
                    {
                        cleanName = cleanName.Substring(id.Length + 1);
                    }

                    cleanName = Regex.Replace(cleanName, "(_{2,})", "_");

                    string newFilePath = Path.Combine(targetImagesFolder, $"{cleanName}{originalExt}");

                    if (i == 0)
                    {
                        try
                        {
                            File.Copy(originalFilePath, newFilePath, overwrite: false);
                            firstNewFilePath = newFilePath;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Failed to copy {originalFilePath} to {newFilePath}: {ex.Message}");
                        }
                    }
                    else
                    {
                        if (firstNewFilePath != null && File.Exists(firstNewFilePath))
                        {
                            try
                            {
                                File.Copy(firstNewFilePath, newFilePath, overwrite: false);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"❌ Copy failed for {newFilePath}: {ex.Message}");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"❌ Could not duplicate {id}. No source file found.");
                        }
                    }

                    assetMappings[id] = cleanName;
                }
            }
            return assetMappings;
        }
    }
}
