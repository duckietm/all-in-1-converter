using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using System.Data;

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
                    Console.WriteLine($"⚠️ Please place FurnitureData.json in the /Database/Variables/ directory");
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

            int totalItems = allItems.Count;
            Console.WriteLine($"🔍 {totalItems} furniture items loaded from JSON.");
            Dictionary<int, int> spriteIdToItemBaseId = new Dictionary<int, int>();

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    var spriteIds = allItems.Select(f => f.id).Distinct().ToList();
                    string inClause = string.Join(",", spriteIds);
                    string mappingQuery = $"SELECT id, sprite_id FROM items_base WHERE sprite_id IN ({inClause})";
                    using (var cmd = new MySqlCommand(mappingQuery, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            int itemBaseId = reader.GetInt32("id");
                            int spriteId = reader.GetInt32("sprite_id");
                            if (!spriteIdToItemBaseId.ContainsKey(spriteId))
                            {
                                spriteIdToItemBaseId[spriteId] = itemBaseId;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine("Error mapping sprite_ids: " + ex.Message);
                    }
                    return;
                }
            }

            List<(int itemBaseId, FurnitureItem item)> itemsToUpdate = allItems
                .Where(f => spriteIdToItemBaseId.ContainsKey(f.id))
                .Select(f => (spriteIdToItemBaseId[f.id], f))
                .ToList();

            int totalToUpdate = itemsToUpdate.Count;
            if (totalToUpdate == 0)
            {
                Console.WriteLine("No matching items_base records found for any furniture items.");
                return;
            }

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
                await connection.OpenAsync();

                foreach (var batch in batches)
                {
                    StringBuilder caseItemName = new StringBuilder("CASE id ");
                    StringBuilder casePublicName = new StringBuilder("CASE id ");
                    List<int> ids = new List<int>();

                    foreach (var (itemBaseId, item) in batch)
                    {
                        ids.Add(itemBaseId);
                        string classname = item.classname.Replace("'", "''");
                        caseItemName.AppendFormat("WHEN {0} THEN '{1}' ", itemBaseId, classname);
                        casePublicName.AppendFormat("WHEN {0} THEN '{1}' ", itemBaseId, classname);
                    }

                    caseItemName.Append("ELSE item_name END");
                    casePublicName.Append("ELSE public_name END");

                    string updateItemsBaseQuery = "UPDATE items_base SET " +
                        "item_name = " + caseItemName.ToString() + ", " +
                        "public_name = " + casePublicName.ToString() +
                        " WHERE id IN (" + string.Join(",", ids) + ")";

                    StringBuilder caseCatalogName = new StringBuilder("CASE ");
                    StringBuilder caseOfferId = new StringBuilder("CASE ");
                    List<string> catalogConditions = new List<string>();

                    foreach (var (itemBaseId, item) in batch)
                    {
                        string classname = item.classname.Replace("'", "''");
                        caseCatalogName.AppendFormat("WHEN FIND_IN_SET('{0}', item_ids) > 0 THEN '{1}' ", itemBaseId, classname);
                        caseOfferId.AppendFormat("WHEN FIND_IN_SET('{0}', item_ids) > 0 THEN {1} ", itemBaseId, item.offerid);
                        catalogConditions.Add($"FIND_IN_SET('{itemBaseId}', item_ids) > 0");
                    }

                    caseCatalogName.Append("ELSE catalog_name END");
                    caseOfferId.Append("ELSE offer_id END");

                    string updateCatalogQuery = "UPDATE catalog_items SET " +
                        "catalog_name = " + caseCatalogName.ToString() + ", " +
                        "offer_id = " + caseOfferId.ToString() +
                        " WHERE " + string.Join(" OR ", catalogConditions);

                    using (var transaction = await connection.BeginTransactionAsync())
                    {
                        try
                        {
                            using (var cmd = new MySqlCommand(updateItemsBaseQuery, connection, transaction))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                            using (var cmd = new MySqlCommand(updateCatalogQuery, connection, transaction))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                            await transaction.CommitAsync();
                        }
                        catch (Exception ex)
                        {
                            lock (consoleLock)
                            {
                                Console.WriteLine($"\nError updating batch: {ex.Message}");
                            }
                            try
                            {
                                await transaction.RollbackAsync();
                            }
                            catch { }
                        }
                    }

                    processedBatches++;
                }

                await connection.CloseAsync();
            }

            timer.Stop();
            timer.Dispose();

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
