using System.Diagnostics;
using System.Text;

namespace Habbo_Downloader.Tools
{
    public static class FfdecExtractorClothes
    {
        private const string FfdecExecutable = @"Tools\ffdec\ffdec-cli.exe";

        public static async Task ExtractSWFAsync(string swfFilePath, string outputDirectory)
        {
            ClearOutputDirectory(outputDirectory);

            string commandExport = $"-onerror ignore -export image,binarydata,symbolClass \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(commandExport);

            string csvPath = Path.Combine(outputDirectory, "symbolClass", "symbols.csv");
            var assetMappings = await RebuildImagesFromCsvAsync(outputDirectory, csvPath);
        }

        private static async Task RunFfdecCommandAsync(string command)
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = FfdecExecutable, // Updated to ffdec-cli.exe
                    Arguments = command,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read output and error streams asynchronously but discard them (silent execution)
            _ = Task.Run(async () => await process.StandardOutput.ReadToEndAsync());
            _ = Task.Run(async () => await process.StandardError.ReadToEndAsync());

            bool exited = await Task.Run(() => process.WaitForExit(20000)); // 20 seconds timeout

            if (!exited)
            {
                process.Kill(true); // Forcefully terminate if it hangs
            }
        }

        private static void ClearOutputDirectory(string outputDirectory)
        {
            if (Directory.Exists(outputDirectory))
            {
                var pngFiles = Directory.GetFiles(outputDirectory, "*.png", SearchOption.AllDirectories);
                foreach (var file in pngFiles)
                {
                    try { File.Delete(file); }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error deleting {file}: {ex.Message}");
                    }
                }
            }
        }

        public static async Task<Dictionary<string, string>> RebuildImagesFromCsvAsync(string imageDir, string csvFilePath)
        {
            var outputMappings = new Dictionary<string, string>();

            // Move first all extracted PNG files to a temporary folder.
            string tmpDir = Path.Combine(imageDir, "tmp");
            if (Directory.Exists(tmpDir))
                Directory.Delete(tmpDir, recursive: true);
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
                f => f);

            string targetImagesFolder = Path.Combine(imageDir, "images");
            Directory.CreateDirectory(targetImagesFolder);

            var csvMappings = ParseCsv(csvFilePath);
            var groups = csvMappings.GroupBy(m => m.Id);
            foreach (var group in groups)
            {
                int id = group.Key;
                var mappings = group.ToList();

                string lookupKey = mappings.Count > 1
                    ? id.ToString()
                    : $"{id}_{mappings.First().Name}";

                if (!fileLookup.TryGetValue(lookupKey, out string? originalFilePath))
                {
                    continue;
                }
                string ext = Path.GetExtension(originalFilePath);

                CsvMapping sourceMapping = mappings.Count > 1
                    ? (mappings.FirstOrDefault(m => m.Type == MappingType.Source) ?? mappings.First())
                    : mappings.First();

                string targetFileName = $"{sourceMapping.Name}{ext}";
                string targetPath = Path.Combine(targetImagesFolder, targetFileName);

                try
                {
                    File.Copy(originalFilePath, targetPath, overwrite: false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Failed to copy file for group with ID {id}: {ex.Message}");
                    continue;
                }

                foreach (var mapping in mappings)
                {
                    outputMappings[mapping.Name] = targetPath;
                }
            }

            await Task.CompletedTask;
            return outputMappings;
        }

        #region CSV Parsing Helpers

        private enum MappingType
        {
            Source,
            Main
        }

        private class CsvMapping
        {
            public int Id { get; set; }
            public string Name { get; set; } = "";
            public MappingType Type { get; set; }
        }

        private static List<CsvMapping> ParseCsv(string csvFilePath)
        {
            var result = new List<CsvMapping>();
            if (!File.Exists(csvFilePath))
            {
                Console.WriteLine($"❌ CSV file not found: {csvFilePath}");
                return result;
            }

            foreach (var line in File.ReadLines(csvFilePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(';');
                if (parts.Length < 2)
                    continue;

                if (!int.TryParse(parts[0].Trim(), out int id))
                    continue;
                if (id == 0)
                    continue;

                string namePart = parts[1].Trim();
                string name = namePart;
                string comment = "";
                int commentIndex = namePart.IndexOf(" <=");
                if (commentIndex >= 0)
                {
                    name = namePart.Substring(0, commentIndex).Trim();
                    comment = namePart.Substring(commentIndex + 3).Trim();
                }

                if (name.Contains("_32_"))
                    continue;

                string lowerComment = comment.ToLower();
                if (lowerComment.Contains("manifest") ||
                    lowerComment.Contains("assets") ||
                    lowerComment.Contains("logic") ||
                    lowerComment.Contains("visualization") ||
                    lowerComment.Contains("index"))
                {
                    continue;
                }

                MappingType type = MappingType.Main;
                if (lowerComment.Contains("source"))
                    type = MappingType.Source;
                else if (lowerComment.Contains("main"))
                    type = MappingType.Main;

                result.Add(new CsvMapping { Id = id, Name = name, Type = type });
            }
            return result;
        }
        #endregion
    }
}
