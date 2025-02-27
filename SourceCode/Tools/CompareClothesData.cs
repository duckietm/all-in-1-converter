using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public static class CompareClothesData
    {
        public static async Task Compare()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Merge");
            string originalDir = Path.Combine(baseDir, "Original_ClothesData");
            string importDir = Path.Combine(baseDir, "Import_ClothesData");
            string mergedDir = Path.Combine(baseDir, "Merged_ClothesData");

            Directory.CreateDirectory(originalDir);
            Directory.CreateDirectory(importDir);
            Directory.CreateDirectory(mergedDir);

            string originalFigureDataPath = Path.Combine(originalDir, "FigureData.json");
            string originalFigureMapPath = Path.Combine(originalDir, "FigureMap.json");
            string mergedFigureDataPath = Path.Combine(mergedDir, "FigureData.json");
            string mergedFigureMapPath = Path.Combine(mergedDir, "FigureMap.json");

            if (!File.Exists(originalFigureDataPath) || !File.Exists(originalFigureMapPath))
            {
                Console.WriteLine("Original FigureData.json or FigureMap.json file is missing.");
                return;
            }

            try
            {
                JObject originalFigureData = JObject.Parse(await File.ReadAllTextAsync(originalFigureDataPath));
                JObject originalFigureMap = JObject.Parse(await File.ReadAllTextAsync(originalFigureMapPath));

                int totalImported = 0;

                var figureDataImportFiles = Directory.GetFiles(importDir, "FigureData*.json");
                var figureMapImportFiles = Directory.GetFiles(importDir, "FigureMap*.json");

                if (figureDataImportFiles.Length == 0 && figureMapImportFiles.Length == 0)
                {
                    Console.WriteLine("No import files found in the Import_ClothesData directory.");
                    return;
                }

                foreach (var importFile in figureDataImportFiles)
                {
                    Console.WriteLine($"Processing FigureData file: {Path.GetFileName(importFile)}");

                    JObject importJson = JObject.Parse(await File.ReadAllTextAsync(importFile));
                    int importedCount = MergeFigureData(originalFigureData, importJson);
                    totalImported += importedCount;
                    Console.WriteLine($"Imported {importedCount} items from {Path.GetFileName(importFile)} into FigureData.json");
                }

                foreach (var importFile in figureMapImportFiles)
                {
                    Console.WriteLine($"Processing FigureMap file: {Path.GetFileName(importFile)}");

                    JObject importJson = JObject.Parse(await File.ReadAllTextAsync(importFile));
                    int importedCount = MergeFigureMap(originalFigureMap, importJson);
                    totalImported += importedCount;
                    Console.WriteLine($"Imported {importedCount} items from {Path.GetFileName(importFile)} into FigureMap.json");
                }

                await File.WriteAllTextAsync(mergedFigureDataPath, originalFigureData.ToString());
                await File.WriteAllTextAsync(mergedFigureMapPath, originalFigureMap.ToString());

                Console.WriteLine($"Clothes data merged successfully and saved to {mergedDir}");
                Console.WriteLine($"Total items imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging clothes data: " + ex.Message);
            }
        }

        private static int MergeFigureData(JObject originalJson, JObject importJson)
        {
            int importedCount = 0;

            if (importJson["palettes"] != null)
            {
                var originalPalettes = originalJson["palettes"] as JArray;
                var importPalettes = importJson["palettes"] as JArray;

                foreach (var importPalette in importPalettes)
                {
                    string paletteId = importPalette["id"].ToString();
                    var existingPalette = originalPalettes.FirstOrDefault(p => p["id"].ToString() == paletteId);

                    if (existingPalette != null)
                    {
                        var importColors = importPalette["colors"] as JArray ?? new JArray();
                        var originalColors = existingPalette["colors"] as JArray ?? new JArray();

                        foreach (var importColor in importColors)
                        {
                            if (!originalColors.Any(c => c["id"].ToString() == importColor["id"].ToString()))
                            {
                                originalColors.Add(importColor);
                                importedCount++;
                            }
                        }
                    }
                    else
                    {
                        originalPalettes.Add(importPalette);
                        importedCount++;
                    }
                }
            }

            if (importJson["setTypes"] != null)
            {
                var originalSetTypes = originalJson["setTypes"] as JArray;
                var importSetTypes = importJson["setTypes"] as JArray;

                foreach (var importSetType in importSetTypes)
                {
                    string setTypeId = importSetType["type"].ToString();
                    var existingSetType = originalSetTypes.FirstOrDefault(s => s["type"].ToString() == setTypeId);

                    if (existingSetType != null)
                    {
                        var importSets = importSetType["sets"] as JArray ?? new JArray();
                        var originalSets = existingSetType["sets"] as JArray ?? new JArray();

                        foreach (var importSet in importSets)
                        {
                            if (!originalSets.Any(s => s["id"].ToString() == importSet["id"].ToString()))
                            {
                                originalSets.Add(importSet);
                                importedCount++;
                            }
                        }
                    }
                    else
                    {
                        originalSetTypes.Add(importSetType);
                        importedCount++;
                    }
                }
            }

            return importedCount;
        }

        private static int MergeFigureMap(JObject originalJson, JObject importJson)
        {
            int importedCount = 0;

            var originalLibraries = originalJson["libraries"].ToDictionary(l => l["id"].ToString());
            HashSet<string> processedLibraryIds = new HashSet<string>();

            foreach (var importLibrary in importJson["libraries"])
            {
                string libraryId = importLibrary["id"].ToString();

                if (processedLibraryIds.Contains(libraryId))
                {
                    continue;
                }
                processedLibraryIds.Add(libraryId);

                if (!originalLibraries.ContainsKey(libraryId))
                {
                    ((JArray)originalJson["libraries"]).Add(importLibrary);
                    importedCount++;
                }
            }

            return importedCount;
        }
    }
}
