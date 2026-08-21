using Habbo_Downloader.IO;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class CompareFurnidata
    {
        public static async Task Compare()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Merge");
            string originalDir = Path.Combine(baseDir, "Original_Furnidata");
            string importDir = Path.Combine(baseDir, "Import_Furnidata");
            string mergedDir = Path.Combine(baseDir, "Merged_Furnidata");

            Directory.CreateDirectory(originalDir);
            Directory.CreateDirectory(importDir);
            Directory.CreateDirectory(mergedDir);

            Console.WriteLine("Where do you want to load the Original Furnidata from?");
            Console.WriteLine("  (D) From the Habbo Default directory (Habbo_Default/files/json/FurnitureData.json)");
            Console.WriteLine("  (I) From Original_Furnidata/FurnitureData.json in Merge");
            Console.Write("Select (I) or (D) [default D]: ");
            var userSelection = Console.ReadLine();

            string originalPath;
            if (string.Equals(userSelection, "I", StringComparison.OrdinalIgnoreCase))
                originalPath = Path.Combine(originalDir, FurnidataIO.FlatFileName);
            else
                originalPath = Path.Combine(Directory.GetCurrentDirectory(), "Habbo_Default", "files", "json", "FurnitureData.json");

            JObject originalJson;
            try
            {
                originalJson = await FurnidataIO.LoadAsync(originalPath);
                int floor = (originalJson["roomitemtypes"]?["furnitype"] as JArray)?.Count ?? 0;
                int wall = (originalJson["wallitemtypes"]?["furnitype"] as JArray)?.Count ?? 0;
                Console.WriteLine($"Loaded FurnitureData.json - floor={floor}, wall={wall}");
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Original FurnitureData not found: {ex.Message}");
                return;
            }

            try
            {
                int totalImported = 0;
                var importEntries = CollectImportEntries(importDir);
                if (importEntries.Count == 0)
                {
                    Console.WriteLine("No import entries found in Import_Furnidata/.");
                    Console.WriteLine("The original JSON will be written unchanged.");
                }
                else
                {
                    foreach (var entry in importEntries)
                    {
                        Console.WriteLine($"Processing: {Path.GetFileName(entry)}");
                        var importJson = await FurnidataIO.LoadAsync(entry);
                        int importedCount = MergeJson(originalJson, importJson, "roomitemtypes");
                        importedCount += MergeJson(originalJson, importJson, "wallitemtypes");
                        totalImported += importedCount;
                        Console.WriteLine($"  + {importedCount} items merged");
                    }

                    SortJsonByID(originalJson, "roomitemtypes");
                    SortJsonByID(originalJson, "wallitemtypes");
                }

                var mergedFilePath = Path.Combine(mergedDir, FurnidataIO.FlatFileName);
                await FurnidataIO.SaveAsync(originalJson, mergedFilePath);
                Console.WriteLine($"Furnidata saved to {mergedFilePath}");

                Console.WriteLine($"Total Furniture imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging furnidata: " + ex.Message);
            }
        }

        private static List<string> CollectImportEntries(string importDir)
        {
            return Directory.GetFiles(importDir, "*.json").ToList();
        }

        // Original additive merge: skip duplicates by classname OR by id.
        private static int MergeJson(JObject originalJson, JObject importJson, string itemType)
        {
            var originalFurniArray = originalJson[itemType]?["furnitype"] as JArray;
            var importFurniArray = importJson[itemType]?["furnitype"] as JArray;
            if (originalFurniArray == null || importFurniArray == null) return 0;

            var originalByClass = originalFurniArray.Cast<JObject>()
                .Where(j => j["classname"] != null)
                .ToDictionary(j => j["classname"].ToString());
            var originalById = originalFurniArray.Cast<JObject>()
                .Where(j => j["id"] != null)
                .ToDictionary(j => j["id"].Value<int>());

            var processedImportClassnames = new HashSet<string>();
            var processedImportIds = new HashSet<int>();
            int importedCount = 0;

            foreach (var importItem in importFurniArray.Cast<JObject>())
            {
                var classname = importItem["classname"]?.ToString();
                var idTok = importItem["id"];
                if (classname == null || idTok == null) continue;
                var id = idTok.Value<int>();

                if (originalByClass.ContainsKey(classname) || processedImportClassnames.Contains(classname) ||
                    originalById.ContainsKey(id) || processedImportIds.Contains(id))
                    continue;

                originalFurniArray.Add(importItem);
                processedImportClassnames.Add(classname);
                processedImportIds.Add(id);
                importedCount++;
            }
            return importedCount;
        }

        private static void SortJsonByID(JObject json, string itemType)
        {
            var furnitypeArray = json[itemType]?["furnitype"] as JArray;
            if (furnitypeArray == null) return;
            var sorted = new JArray(furnitypeArray.OrderBy(item => item["id"]?.Value<int>() ?? int.MaxValue));
            json[itemType]["furnitype"] = sorted;
        }
    }
}
