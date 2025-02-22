using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class CompareProductData
    {
        public static async Task Compare()
        {
            // Define base directories for Merge
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Merge");
            string importDir = Path.Combine(baseDir, "Import_ProductData");
            string mergedDir = Path.Combine(baseDir, "Merged_ProductData");

            // Ensure necessary directories exist
            Directory.CreateDirectory(mergedDir);
            Directory.CreateDirectory(importDir);

            // Ask user where to load the original ProductData from
            Console.WriteLine("👉 Where do you want to load the Original Productdata from 👈");
            Console.WriteLine("⏩ (D) From the Habbo Default directory");
            Console.WriteLine("⏩ (I) From the Original_ProductData folder in Merge");
            Console.Write("💁 Please select (I) or (D) [default is D]: ");
            string choice = Console.ReadLine()?.Trim().ToUpper();

            // Determine the original file path based on user input
            string originalFilePath;
            if (string.IsNullOrEmpty(choice) || choice == "D")
            {
                // Load from Habbo Default directory
                originalFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Habbo_Default", "files", "json", "ProductData.json");
            }
            else if (choice == "I")
            {
                // Load from the Original_ProductData folder in Merge
                string originalDir = Path.Combine(baseDir, "Original_ProductData");
                Directory.CreateDirectory(originalDir); // Ensure the directory exists
                originalFilePath = Path.Combine(originalDir, "ProductData.json");
            }
            else
            {
                // Invalid input defaults to Habbo Default
                originalFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Habbo_Default", "files", "json", "ProductData.json");
            }

            if (!File.Exists(originalFilePath))
            {
                Console.WriteLine($"Original ProductData.json file is missing at: {originalFilePath}");
                return;
            }

            try
            {
                JObject originalJson = JObject.Parse(await File.ReadAllTextAsync(originalFilePath));
                int totalImported = 0;

                var importFiles = Directory.GetFiles(importDir, "*.json");
                if (importFiles.Length == 0)
                {
                    Console.WriteLine("No JSON files found in the Import_ProductData directory.");
                    return;
                }

                bool replaceAll = false;
                bool skipAll = false;

                foreach (var importFile in importFiles)
                {
                    Console.WriteLine($"Processing file: {Path.GetFileName(importFile)}");
                    JObject importJson = JObject.Parse(await File.ReadAllTextAsync(importFile));
                    int importedCount = MergeJson(originalJson, importJson, "productdata", ref replaceAll, ref skipAll);
                    totalImported += importedCount;
                    Console.WriteLine($"Imported {importedCount} items from {Path.GetFileName(importFile)}");
                }

                SortJsonByCode(originalJson, "productdata");
                string mergedFilePath = Path.Combine(mergedDir, "ProductData.json");
                await File.WriteAllTextAsync(mergedFilePath, originalJson.ToString());

                Console.WriteLine($"ProductData merged successfully and saved to {mergedFilePath}");
                Console.WriteLine($"Total Products imported: {totalImported}");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error merging ProductData: " + ex.Message);
            }
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
