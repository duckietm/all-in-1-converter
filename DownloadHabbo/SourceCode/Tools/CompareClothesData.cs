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
                var originalPalettes = originalJson["palettes"].ToDictionary(p => p["id"].ToString());
                var importPalettes = importJson["palettes"].ToDictionary(p => p["id"].ToString());

                foreach (var importPalette in importPalettes)
                {
                    if (originalPalettes.ContainsKey(importPalette.Key))
                    {
                        var originalPalette = originalPalettes[importPalette.Key];
                        var importPaletteColors = importPalette.Value["colors"] as JArray;

                        if (importPaletteColors != null)
                        {
                            var originalColors = originalPalette["colors"] as JArray;
                            if (originalColors == null)
                            {
                                originalPalette["colors"] = new JArray();
                                originalColors = originalPalette["colors"] as JArray;
                            }

                            foreach (var importColor in importPaletteColors)
                            {
                                string colorId = importColor["id"].ToString();
                                if (!originalColors.Any(c => c["id"].ToString() == colorId))
                                {
                                    originalColors.Add(importColor);
                                    importedCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                         ((JArray)originalJson["palettes"]).Add(importPalette.Value);
                        importedCount++;
                    }
                }
            }

            if (importJson["setTypes"] != null)
            {
                var originalSetTypes = originalJson["setTypes"].ToDictionary(s => s["type"].ToString());
                var importSetTypes = importJson["setTypes"].ToDictionary(s => s["type"].ToString());

                foreach (var importSetType in importSetTypes)
                {
                    if (originalSetTypes.ContainsKey(importSetType.Key))
                    {
                        var originalSetType = originalSetTypes[importSetType.Key];
                        var importSets = importSetType.Value["sets"] as JArray;

                        if (importSets != null)
                        {
                            var originalSets = originalSetType["sets"] as JArray;
                            if (originalSets == null)
                            {
                                originalSetType["sets"] = new JArray();
                                originalSets = originalSetType["sets"] as JArray;
                            }

                            foreach (var importSet in importSets)
                            {
                                string setId = importSet["id"].ToString();
                                if (!originalSets.Any(s => s["id"].ToString() == setId))
                                {
                                    originalSets.Add(importSet);
                                    importedCount++;
                                }
                            }
                        }
                    }
                    else
                    {
                        ((JArray)originalJson["setTypes"]).Add(importSetType.Value);
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
            var importLibraries = importJson["libraries"].ToDictionary(l => l["id"].ToString());

            foreach (var importLibrary in importLibraries)
            {
                if (!originalLibraries.ContainsKey(importLibrary.Key))
                {
                    ((JArray)originalJson["libraries"]).Add(importLibrary.Value);
                    importedCount++;
                }
            }

            return importedCount;
        }
    }
}