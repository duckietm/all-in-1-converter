using Newtonsoft.Json.Linq;

namespace ConsoleApplication
{
    public static class CompareProductData
    {
        public static async Task Compare()
        {
            string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Merge");
            string originalDir = Path.Combine(baseDir, "Original_ProductData");
            string importDir = Path.Combine(baseDir, "Import_ProductData");
            string mergedDir = Path.Combine(baseDir, "Merged_ProductData");

            Directory.CreateDirectory(originalDir);
            Directory.CreateDirectory(importDir);
            Directory.CreateDirectory(mergedDir);

            string originalFilePath = Path.Combine(originalDir, "ProductData.json");
            string mergedFilePath = Path.Combine(mergedDir, "ProductData.json");

            if (!File.Exists(originalFilePath))
            {
                Console.WriteLine("Original ProductData.json file is missing.");
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
            // Extract dictionaries of items from original and import JSON
            var originalItems = originalJson[itemType]["product"].ToDictionary(item => item["code"].ToString());

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
                        string code = importKey;
                        string description = importItem["description"]?.ToString() ?? "No description";

                        Console.WriteLine("Would you like to replace the:");
                        Console.WriteLine($"Furniture code: {code}");
                        Console.WriteLine($"Description: {description}");
                        Console.WriteLine("From the original?");
                        Console.WriteLine("(Y)-Yes, (A)-Yes to all, (N)-No, (Z)-No to all");
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
                    var originalArray = (JArray)originalJson[itemType]["product"];
                    var duplicateIndex = originalArray.IndexOf(originalArray.First(item => item["code"].ToString() == importKey));
                    originalArray[duplicateIndex] = importItem;
                    importedCount++;
                }
                else
                {
                    // Add the new item to the original JSON
                    ((JArray)originalJson[itemType]["product"]).Add(importItem);
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
                var sortedArray = new JArray(
                    productArray.OrderBy(item => item["code"].Value<string>())
                );
                json[itemType]["product"] = sortedArray;
            }
        }
    }
}