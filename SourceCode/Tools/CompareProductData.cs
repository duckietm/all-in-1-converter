using Habbo_Downloader.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class CompareProductData
    {
        public static async Task Compare()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Merge");
            string importDir = Path.Combine(baseDir, "Import_ProductData");
            string mergedDir = Path.Combine(baseDir, "Merged_ProductData");
            Directory.CreateDirectory(importDir);
            Directory.CreateDirectory(mergedDir);

            Console.WriteLine("Where do you want to load the Original ProductData from?");
            Console.WriteLine("  (D) From the Habbo Default directory (Habbo_Default/files/json/ProductData.json)");
            Console.WriteLine("  (I) From Original_ProductData/ in Merge (flat file or split directory)");
            Console.Write("Select (I) or (D) [default D]: ");
            string choice = Console.ReadLine()?.Trim().ToUpper();

            string originalPath;
            if (choice == "I")
                originalPath = Path.Combine(baseDir, "Original_ProductData");
            else
                originalPath = Path.Combine(Directory.GetCurrentDirectory(), "Habbo_Default", "files", "json", "ProductData.json");

            JObject originalJson;
            try
            {
                originalJson = await ProductDataIO.LoadAsync(originalPath);
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"Original ProductData not found: {ex.Message}");
                return;
            }

            try
            {
                int totalImported = 0;
                var importEntries = CollectImportEntries(importDir);
                if (importEntries.Count == 0)
                {
                    Console.WriteLine("No import entries found in Import_ProductData/ (expected: *.json files or sub-directories with manifest.json5).");
                    return;
                }

                bool replaceAll = false;
                bool skipAll = false;

                foreach (var entry in importEntries)
                {
                    Console.WriteLine($"Processing: {Path.GetFileName(entry)}");
                    JObject importJson = await ProductDataIO.LoadAsync(entry);
                    int importedCount = MergeJson(originalJson, importJson, "productdata", ref replaceAll, ref skipAll);
                    totalImported += importedCount;
                    Console.WriteLine($"  + {importedCount} items merged");
                }

                SortJsonByCode(originalJson, "productdata");

                Console.Write("Output format: (F)lat single ProductData.json or (S)plit manifest.json5+tier [default F]: ");
                var fmtChoice = Console.ReadLine()?.Trim().ToUpperInvariant();
                if (fmtChoice == "S")
                {
                    var splitOut = Path.Combine(mergedDir, "ProductData_split");
                    if (Directory.Exists(splitOut)) Directory.Delete(splitOut, true);
                    await ProductDataIO.SaveAsync(originalJson, splitOut, GamedataFormat.Split);
                    Console.WriteLine($"ProductData merged and saved (split mode) to {splitOut}");
                }
                else
                {
                    var mergedFilePath = Path.Combine(mergedDir, ProductDataIO.FlatFileName);
                    await ProductDataIO.SaveAsync(originalJson, mergedFilePath, GamedataFormat.Flat);
                    Console.WriteLine($"ProductData merged and saved (flat) to {mergedFilePath}");
                }

                Console.WriteLine($"Total Products imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging ProductData: " + ex.Message);
            }
        }

        private static List<string> CollectImportEntries(string importDir)
        {
            var entries = new List<string>();
            foreach (var sub in Directory.GetDirectories(importDir))
                if (ProductDataIO.IsSplitDirectory(sub)) entries.Add(sub);
            entries.AddRange(Directory.GetFiles(importDir, "*.json"));
            entries.AddRange(Directory.GetFiles(importDir, "*.json5"));
            return entries;
        }

        private static int MergeJson(JObject originalJson, JObject importJson, string itemType, ref bool replaceAll, ref bool skipAll)
        {
            // Instead of ToDictionary directly (which fails on duplicate keys),
            // group by the code and take the first occurrence.
            var originalArray = (JArray)originalJson[itemType]["product"];
            var originalItems = originalArray
                .GroupBy(item => item["code"].ToString())
                .ToDictionary(g => g.Key, g => g.First());

            // Use a HashSet to track processed keys in the imported JSON for deduplication
            HashSet<string> processedKeys = new HashSet<string>();
            int importedCount = 0;

            foreach (var importItem in importJson[itemType]["product"])
            {
                string importKey = importItem["code"].ToString();

                // Skip duplicates within the imported JSON
                if (processedKeys.Contains(importKey))
                {
                    continue;
                }
                processedKeys.Add(importKey);

                // Check if the key exists in the original JSON
                if (originalItems.ContainsKey(importKey))
                {
                    if (skipAll)
                    {
                        continue;
                    }
                    else if (!replaceAll)
                    {
                        string description = importItem["description"]?.ToString() ?? "No description";

                        Console.WriteLine("Would you like to replace the following product?");
                        Console.WriteLine($"Code: {importKey}");
                        Console.WriteLine("Enter (Y) for Yes, (A) for Yes to all, (N) for No, or (Z) for No to all:");
                        var response = Console.ReadLine()?.ToUpper();

                        switch (response)
                        {
                            case "Y":
                                break;
                            case "A":
                                replaceAll = true;
                                break;
                            case "N":
                                continue;
                            case "Z":
                                skipAll = true;
                                continue;
                            default:
                                Console.WriteLine("Invalid input. Skipping this entry.");
                                continue;
                        }
                    }

                    // Replace the original item with the imported item
                    var duplicateIndex = originalArray.IndexOf(
                        originalArray.First(item => item["code"].ToString() == importKey));
                    originalArray[duplicateIndex] = importItem;
                    importedCount++;
                }
                else
                {
                    // Add the new item to the original JSON
                    originalArray.Add(importItem);
                    importedCount++;
                }
            }

            return importedCount;
        }

        private static void SortJsonByCode(JObject json, string itemType)
        {
            var productArray = json[itemType]["product"] as JArray;
            if (productArray != null)
            {
                var sortedArray = new JArray(productArray.OrderBy(item => item["code"].Value<string>()));
                json[itemType]["product"] = sortedArray;
            }
        }
    }
}
