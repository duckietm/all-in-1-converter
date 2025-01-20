using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public static class CompareFurnidata
    {
        public static async Task Compare()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "merge-json");
            string originalDir = Path.Combine(baseDir, "Original_Furnidata");
            string importDir = Path.Combine(baseDir, "Import_Furnidata");
            string mergedDir = Path.Combine(baseDir, "Merged_Furnidata");

            Directory.CreateDirectory(originalDir);
            Directory.CreateDirectory(importDir);
            Directory.CreateDirectory(mergedDir);

            string originalFilePath = Path.Combine(originalDir, "FurnitureData.json");
            string mergedFilePath = Path.Combine(mergedDir, "FurnitureData.json");

            if (!File.Exists(originalFilePath))
            {
                Console.WriteLine("Original FurnitureData.json file is missing.");
                return;
            }

            try
            {
                JObject originalJson = JObject.Parse(await File.ReadAllTextAsync(originalFilePath));
                int totalImported = 0;

                var importFiles = Directory.GetFiles(importDir, "*.json");

                if (importFiles.Length == 0)
                {
                    Console.WriteLine("No JSON files found in the Import_Furnidata directory.");
                    return;
                }

                foreach (var importFile in importFiles)
                {
                    Console.WriteLine($"Processing file: {Path.GetFileName(importFile)}");

                    JObject importJson = JObject.Parse(await File.ReadAllTextAsync(importFile));
                    int importedCount = MergeJson(originalJson, importJson, "roomitemtypes");
                    importedCount += MergeJson(originalJson, importJson, "wallitemtypes");

                    totalImported += importedCount;
                    Console.WriteLine($"Imported {importedCount} items from {Path.GetFileName(importFile)}");
                }

                SortJsonByID(originalJson, "roomitemtypes");
                SortJsonByID(originalJson, "wallitemtypes");

                await File.WriteAllTextAsync(mergedFilePath, originalJson.ToString());

                Console.WriteLine($"Furnidata merged successfully and saved to {mergedFilePath}");
                Console.WriteLine($"Total Furniture imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging furnidata: " + ex.Message);
            }
        }

        private static int MergeJson(JObject originalJson, JObject importJson, string itemType)
        {
            var originalItems = originalJson[itemType]["furnitype"]
                .ToDictionary(item => item["classname"].ToString());

            var processedImportKeys = new HashSet<string>();

            int importedCount = 0;

            foreach (var importItem in importJson[itemType]["furnitype"])
            {
                var classname = importItem["classname"].ToString();

                if (originalItems.ContainsKey(classname) || processedImportKeys.Contains(classname))
                {
                    continue;
                }

                ((JArray)originalJson[itemType]["furnitype"]).Add(importItem);
                processedImportKeys.Add(classname);
                importedCount++;
            }

            return importedCount;
        }

        private static void SortJsonByID(JObject json, string itemType)
        {
            var furnitypeArray = json[itemType]["furnitype"] as JArray;
            if (furnitypeArray != null)
            {
                var sortedArray = new JArray(
                    furnitypeArray.OrderBy(item => item["id"].Value<int>())
                );
                json[itemType]["furnitype"] = sortedArray;
            }
        }
    }
}