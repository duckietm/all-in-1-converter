using Habbo_Downloader.IO;
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

            JObject originalFigureData;
            JObject originalFigureMap;
            try
            {
                var figureDataPath = Path.Combine(originalDir, FigureDataIO.FlatFileName);
                var figureMapPath = Path.Combine(originalDir, FigureMapIO.FlatFileName);
                originalFigureData = await FigureDataIO.LoadAsync(figureDataPath);
                originalFigureMap  = await FigureMapIO.LoadAsync(figureMapPath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Missing original clothes input: {ex.Message}");
                return;
            }

            try
            {
                int totalImported = 0;

                var figureDataEntries = CollectFigureDataEntries(importDir);
                var figureMapEntries  = CollectFigureMapEntries(importDir);

                if (figureDataEntries.Count == 0 && figureMapEntries.Count == 0)
                {
                    Console.WriteLine("No FigureData* or FigureMap* import entries found in Import_ClothesData/.");
                    Console.WriteLine("The original JSON files will be written unchanged.");
                }

                foreach (var entry in figureDataEntries)
                {
                    Console.WriteLine($"Processing FigureData entry: {Path.GetFileName(entry)}");
                    var importJson = await FigureDataIO.LoadAsync(entry);
                    int n = MergeFigureData(originalFigureData, importJson);
                    totalImported += n;
                    Console.WriteLine($"  + {n} merged into FigureData");
                }
                foreach (var entry in figureMapEntries)
                {
                    Console.WriteLine($"Processing FigureMap entry: {Path.GetFileName(entry)}");
                    var importJson = await FigureMapIO.LoadAsync(entry);
                    int n = MergeFigureMap(originalFigureMap, importJson);
                    totalImported += n;
                    Console.WriteLine($"  + {n} merged into FigureMap");
                }

                var fdPath = Path.Combine(mergedDir, FigureDataIO.FlatFileName);
                var fmPath = Path.Combine(mergedDir, FigureMapIO.FlatFileName);
                await FigureDataIO.SaveAsync(originalFigureData, fdPath);
                await FigureMapIO.SaveAsync(originalFigureMap, fmPath);
                Console.WriteLine($"Clothes merged -> {mergedDir}");

                Console.WriteLine($"Total items imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging clothes data: " + ex.Message);
            }
        }

        private static List<string> CollectFigureDataEntries(string importDir)
        {
            return Directory.GetFiles(importDir, "FigureData*.json").ToList();
        }

        private static List<string> CollectFigureMapEntries(string importDir)
        {
            return Directory.GetFiles(importDir, "FigureMap*.json").ToList();
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
