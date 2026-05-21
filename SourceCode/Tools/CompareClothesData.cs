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

            // Original side: each of FigureData/FigureMap can be either flat file
            // (Original_ClothesData/FigureData.json) or split directory
            // (Original_ClothesData/FigureData/manifest.json5 + tier/).
            JObject originalFigureData;
            JObject originalFigureMap;
            try
            {
                var figureDataPath = ResolveOriginalPath(originalDir, FigureDataIO.FlatFileName, "FigureData");
                var figureMapPath  = ResolveOriginalPath(originalDir, FigureMapIO.FlatFileName,  "FigureMap");
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
                    return;
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

                Console.Write("Output format: (F)lat single FigureData.json+FigureMap.json or (S)plit manifest.json5+tier [default F]: ");
                var fmtChoice = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (fmtChoice == "S")
                {
                    var fdOut = Path.Combine(mergedDir, "FigureData_split");
                    var fmOut = Path.Combine(mergedDir, "FigureMap_split");
                    if (Directory.Exists(fdOut)) Directory.Delete(fdOut, true);
                    if (Directory.Exists(fmOut)) Directory.Delete(fmOut, true);
                    await FigureDataIO.SaveAsync(originalFigureData, fdOut, GamedataFormat.Split);
                    await FigureMapIO.SaveAsync(originalFigureMap,  fmOut, GamedataFormat.Split);
                    Console.WriteLine($"Clothes merged (split) -> {fdOut}, {fmOut}");
                }
                else
                {
                    var fdPath = Path.Combine(mergedDir, FigureDataIO.FlatFileName);
                    var fmPath = Path.Combine(mergedDir, FigureMapIO.FlatFileName);
                    await FigureDataIO.SaveAsync(originalFigureData, fdPath, GamedataFormat.Flat);
                    await FigureMapIO.SaveAsync(originalFigureMap,  fmPath, GamedataFormat.Flat);
                    Console.WriteLine($"Clothes merged (flat) -> {mergedDir}");
                }

                Console.WriteLine($"Total items imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging clothes data: " + ex.Message);
            }
        }

        private static string ResolveOriginalPath(string originalDir, string flatFileName, string splitFolderName)
        {
            var flat = Path.Combine(originalDir, flatFileName);
            if (File.Exists(flat)) return flat;
            var splitDir = Path.Combine(originalDir, splitFolderName);
            if (Directory.Exists(splitDir)) return splitDir;
            // Fall back to default path (will throw FileNotFoundException downstream).
            return flat;
        }

        private static List<string> CollectFigureDataEntries(string importDir)
        {
            var entries = new List<string>();
            foreach (var sub in Directory.GetDirectories(importDir))
                if (Path.GetFileName(sub).StartsWith("FigureData", StringComparison.OrdinalIgnoreCase) && FigureDataIO.IsSplitDirectory(sub))
                    entries.Add(sub);
            entries.AddRange(Directory.GetFiles(importDir, "FigureData*.json"));
            entries.AddRange(Directory.GetFiles(importDir, "FigureData*.json5"));
            return entries;
        }

        private static List<string> CollectFigureMapEntries(string importDir)
        {
            var entries = new List<string>();
            foreach (var sub in Directory.GetDirectories(importDir))
                if (Path.GetFileName(sub).StartsWith("FigureMap", StringComparison.OrdinalIgnoreCase) && FigureMapIO.IsSplitDirectory(sub))
                    entries.Add(sub);
            entries.AddRange(Directory.GetFiles(importDir, "FigureMap*.json"));
            entries.AddRange(Directory.GetFiles(importDir, "FigureMap*.json5"));
            return entries;
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
