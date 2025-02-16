using System.Diagnostics;

namespace Habbo_Downloader.Tools
{
    public static class FfdecExtractorClothes
    {
        private const string ToolsDirectory = @"Tools\ffdec\ffdec.jar";

        public static async Task ExtractSWFAsync(string swfFilePath, string outputDirectory)
        {
            ClearOutputDirectory(outputDirectory);

            string commandExport = $"-export image,binarydata,symbolClass \"{outputDirectory}\" \"{swfFilePath}\"";
            await RunFfdecCommandAsync(commandExport);

            // Use the CSV mapping symbolClass/symbols.csv.
            string csvPath = Path.Combine(outputDirectory, "symbolClass", "symbols.csv");
            var assetMappings = await RebuildImagesFromCsvAsync(outputDirectory, csvPath);
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
                    try { File.Delete(file); }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"❌ Error deleting {file}: {ex.Message}");
                    }
                }
            }
        }

        // CSV rules for the furni
        // - Skip any line with ID 0 As this is the Furni name
        // - Skip any mapping whose name contains "_32_" or whose comment contains any of:
        //   "manifest", "assets", "logic", "visualization", or "index".
        // - For a given CSV ID:
        //     • If there are multiple rows (e.g. ID 1), the extracted file is named "{ID}.png".
        //       The first row (marked "source" or the first row if none is marked) is used as the physical image.
        //       The alias (Main) rows simply point to that file.
        //     • If there is a single row, the extracted file is named "{ID}_{MappingName}.png".
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

            // Build a lookup of extracted files.
            // For SWFs with multiple images: key = "{ID}" (e.g. "1").
            // For single-image SWFs: key = "{ID}_{MappingName}" (e.g. "17_pura_mdl3_pura_mdl3_64_a_2_0").
            string[] tmpFiles = Directory.GetFiles(tmpDir, "*.png", SearchOption.AllDirectories);
            var fileLookup = tmpFiles.ToDictionary(
                f => Path.GetFileNameWithoutExtension(f),
                f => f);

            // Here we prepare the target folder.
            string targetImagesFolder = Path.Combine(imageDir, "images");
            Directory.CreateDirectory(targetImagesFolder);

            // Parse the CSV and group mappings by ID.
            var csvMappings = ParseCsv(csvFilePath);
            var groups = csvMappings.GroupBy(m => m.Id);
            foreach (var group in groups)
            {
                int id = group.Key;
                var mappings = group.ToList();

                // Determine expected lookup key:
                // - If multiple mappings: expect file named "{ID}.png"
                // - If single mapping: expect file named "{ID}_{MappingName}.png"
                string lookupKey = mappings.Count > 1
                    ? id.ToString()
                    : $"{id}_{mappings.First().Name}";

                if (!fileLookup.TryGetValue(lookupKey, out string? originalFilePath))
                {
                    continue;
                }
                string ext = Path.GetExtension(originalFilePath);

                // Determine the target file name:
                // For multiple mappings, choose the source mapping (first marked Source if available, else first).
                // For single mapping, use its CSV name.
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