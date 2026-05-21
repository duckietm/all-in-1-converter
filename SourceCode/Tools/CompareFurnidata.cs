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
            Console.WriteLine("  (I) From Original_Furnidata/ in Merge (flat file or split directory)");
            Console.Write("Select (I) or (D) [default D]: ");
            var userSelection = Console.ReadLine();

            string originalPath;
            if (string.Equals(userSelection, "I", StringComparison.OrdinalIgnoreCase))
                originalPath = originalDir; // auto-detects flat FurnitureData.json or split layout
            else
                originalPath = Path.Combine(Directory.GetCurrentDirectory(), "Habbo_Default", "files", "json", "FurnitureData.json");

            JObject originalJson;
            try
            {
                originalJson = await FurnidataIO.LoadAsync(originalPath);
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
                    Console.WriteLine("No import entries found in Import_Furnidata/ (expected: *.json files or sub-directories with manifest.json5).");
                    return;
                }

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

                Console.Write("Output format: (F)lat single FurnitureData.json or (S)plit manifest.json5+tier [default F]: ");
                var fmtChoice = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (fmtChoice == "S")
                {
                    var splitOut = Path.Combine(mergedDir, "FurnitureData_split");
                    if (Directory.Exists(splitOut)) Directory.Delete(splitOut, true);
                    await FurnidataIO.SaveAsync(originalJson, splitOut, GamedataFormat.Split);
                    Console.WriteLine($"Furnidata merged and saved (split mode) to {splitOut}");
                }
                else
                {
                    var mergedFilePath = Path.Combine(mergedDir, FurnidataIO.FlatFileName);
                    await FurnidataIO.SaveAsync(originalJson, mergedFilePath, GamedataFormat.Flat);
                    Console.WriteLine($"Furnidata merged and saved (flat) to {mergedFilePath}");
                }

                Console.WriteLine($"Total Furniture imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging furnidata: " + ex.Message);
            }
        }

        private static List<string> CollectImportEntries(string importDir)
        {
            var entries = new List<string>();
            // Directory entries (split-mode imports)
            foreach (var sub in Directory.GetDirectories(importDir))
            {
                if (FurnidataIO.IsSplitDirectory(sub))
                    entries.Add(sub);
            }
            // Flat JSON / JSON5 files
            entries.AddRange(Directory.GetFiles(importDir, "*.json"));
            entries.AddRange(Directory.GetFiles(importDir, "*.json5"));
            return entries;
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
