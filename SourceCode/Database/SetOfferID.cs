using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;

namespace ConsoleApplication
{
    public class FurnitureData
    {
        public RoomItemTypes roomitemtypes { get; set; }
        public WallItemTypes wallitemtypes { get; set; }
    }

    public class RoomItemTypes
    {
        public List<FurnitureItem> furnitype { get; set; }
    }

    public class WallItemTypes
    {
        public List<FurnitureItem> furnitype { get; set; }
    }

    public class FurnitureItem
    {
        public int id { get; set; }
        public string classname { get; set; }
        public int offerid { get; set; } = -1;
        public int revision { get; set; }
        public string category { get; set; }
        public int defaultdir { get; set; }
        public int xdim { get; set; }
        public int ydim { get; set; }
        public PartColors partcolors { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string adurl { get; set; }
        public bool buyout { get; set; }
        public int rentofferid { get; set; }
        public bool rentbuyout { get; set; }
        public bool bc { get; set; }
        public bool excludeddynamic { get; set; }
        public string customparams { get; set; }
        public int specialtype { get; set; }
        public bool canstandon { get; set; }
        public bool cansiton { get; set; }
        public bool canlayon { get; set; }
        public string furniline { get; set; }
        public string environment { get; set; }
        public bool rare { get; set; }
    }

    public class PartColors
    {
        public List<string> color { get; set; }
    }

    public static class SetOfferID
    {
        private static readonly object consoleLock = new object();

        public static async Task RunAsync()
        {
            const string jsonFilePath = "./Database/Variables/FurnitureData.json";
            if (!File.Exists(jsonFilePath))
            {
                lock (consoleLock)
                {
                    Console.WriteLine("⚠️ Please place FurnitureData.json in the /Database/Variables/ directory");
                }
                return;
            }

            string jsonContent;
            try
            {
                jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("Error reading JSON file: " + ex.Message);
                }
                return;
            }

            FurnitureData furnitureData;
            try
            {
                furnitureData = JsonSerializer.Deserialize<FurnitureData>(jsonContent);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("Error deserializing JSON: " + ex.Message);
                }
                return;
            }

            List<FurnitureItem> allItems = new List<FurnitureItem>();
            if (furnitureData.roomitemtypes?.furnitype != null)
                allItems.AddRange(furnitureData.roomitemtypes.furnitype);
            if (furnitureData.wallitemtypes?.furnitype != null)
                allItems.AddRange(furnitureData.wallitemtypes.furnitype);

            if (allItems.Count == 0)
            {
                Console.WriteLine("No furniture items found in the JSON file.");
                return;
            }

            Console.WriteLine($"🔍 {allItems.Count} furniture items loaded from JSON.");

            // Fetch catalog_items from the database to get existing offer_ids
            List<(int catalogId, string catalogName, int currentOfferId)> catalogItems = new List<(int, string, int)>();
            HashSet<int> usedOfferIds = new HashSet<int>();
            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    string query = "SELECT id, catalog_name, offer_id FROM catalog_items";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int offerId = reader.GetInt32("offer_id");
                            catalogItems.Add((
                                reader.GetInt32("id"),
                                reader.GetString("catalog_name"),
                                offerId
                            ));
                            if (offerId > 0)
                            {
                                usedOfferIds.Add(offerId);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine("Error fetching catalog_items: " + ex.Message);
                    }
                    return;
                }
            }

            if (catalogItems.Count == 0)
            {
                Console.WriteLine("No items found in catalog_items.");
                return;
            }

            var groups = allItems.GroupBy(x => x.classname);
            Dictionary<string, int> offerMapping = new Dictionary<string, int>();
            Random rng = new Random();
            int generatedCount = 0;

            foreach (var g in groups)
            {
                int offerId = g.First().offerid;
                if (offerId == -1)
                {
                    int newId;
                    do
                    {
                        newId = rng.Next(60001, int.MaxValue);
                    } while (usedOfferIds.Contains(newId));

                    usedOfferIds.Add(newId);
                    offerId = newId;
                    generatedCount++;

                    foreach (var item in g)
                    {
                        item.offerid = newId;
                    }
                }
                offerMapping[g.Key] = offerId;
            }

            if (generatedCount > 0)
            {
                Console.WriteLine($"🔢 Generated {generatedCount} new unique offer IDs (>60000).");
            }

            List<(int catalogId, int newOfferId)> itemsToUpdate = catalogItems
                .Where(c => offerMapping.ContainsKey(c.catalogName) && offerMapping[c.catalogName] != c.currentOfferId)
                .Select(c => (c.catalogId, offerMapping[c.catalogName]))
                .ToList();

            if (itemsToUpdate.Count == 0)
            {
                Console.WriteLine("No catalog_items need offer_id updates.");
                try
                {
                    string updatedJson = JsonSerializer.Serialize(furnitureData, new JsonSerializerOptions { WriteIndented = true });
                    await File.WriteAllTextAsync(jsonFilePath, updatedJson);
                    Console.WriteLine("Updated JSON saved with new offer IDs.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error saving updated JSON: " + ex.Message);
                }
                return;
            }

            Console.WriteLine($"🔄 {itemsToUpdate.Count} catalog_items need offer_id updates.");

            var batches = Partition(itemsToUpdate, 100);
            int totalBatches = batches.Count;
            int processedBatches = 0;

            System.Timers.Timer timer = new System.Timers.Timer(500);
            timer.Elapsed += (sender, e) =>
            {
                lock (consoleLock)
                {
                    double percent = (double)processedBatches / totalBatches * 100;
                    string progressBar = BuildProgressBar(processedBatches, totalBatches, 50);
                    Console.Write($"\r{progressBar} {percent:0.00}% ({processedBatches}/{totalBatches} batches processed)");
                }
            };
            timer.AutoReset = true;
            timer.Start();

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    foreach (var batch in batches)
                    {
                        StringBuilder caseOfferId = new StringBuilder("CASE id ");
                        List<int> ids = new List<int>();
                        foreach (var (catalogId, newOfferId) in batch)
                        {
                            ids.Add(catalogId);
                            caseOfferId.AppendFormat("WHEN {0} THEN {1} ", catalogId, newOfferId);
                        }
                        caseOfferId.Append("ELSE offer_id END");

                        string updateQuery = $"UPDATE catalog_items SET offer_id = {caseOfferId} WHERE id IN ({string.Join(",", ids)})";
                        try
                        {
                            using (var cmd = new MySqlCommand(updateQuery, connection))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"\nError updating batch: {ex.Message}");
                        }
                        processedBatches++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error updating catalog_items: " + ex.Message);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }

            timer.Stop();
            timer.Dispose();

            try
            {
                string updatedJson = JsonSerializer.Serialize(furnitureData, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(jsonFilePath, updatedJson);
                Console.WriteLine("Updated JSON saved with new offer IDs.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving updated JSON: " + ex.Message);
            }

            lock (consoleLock)
            {
                string progressBar = BuildProgressBar(totalBatches, totalBatches, 50);
                Console.WriteLine($"\r{progressBar} 100.00% ({totalBatches}/{totalBatches} batches processed)");
                Console.WriteLine("\nSetOfferID process completed.");
            }
        }

        private static string BuildProgressBar(int processed, int total, int barWidth)
        {
            double fraction = (double)processed / total;
            int filledBars = (int)(fraction * barWidth);
            int emptyBars = barWidth - filledBars;
            return "[" + new string('▓', filledBars) + new string('-', emptyBars) + "]";
        }

        private static List<List<T>> Partition<T>(List<T> source, int size)
        {
            List<List<T>> partitions = new List<List<T>>();
            for (int i = 0; i < source.Count; i += size)
            {
                partitions.Add(source.GetRange(i, Math.Min(size, source.Count - i)));
            }
            return partitions;
        }
    }
}