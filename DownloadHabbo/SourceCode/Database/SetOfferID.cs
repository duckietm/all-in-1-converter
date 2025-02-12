using System.Text.Json;
using MySql.Data.MySqlClient;
using System.Threading;

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
        public static async Task RunAsync()
        {
            Console.WriteLine("Loading Database Fix Order_ID!");

            const string jsonFilePath = "./Database/VAriables/FurnitureData.json";
            if (!File.Exists(jsonFilePath))
            {
                Console.WriteLine($"Error: {jsonFilePath} file not found.");
                return;
            }

            string jsonContent;
            try
            {
                jsonContent = await File.ReadAllTextAsync(jsonFilePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading JSON file: " + ex.Message);
                return;
            }

            FurnitureData furnitureData;
            try
            {
                furnitureData = JsonSerializer.Deserialize<FurnitureData>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing JSON: " + ex.Message);
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

            Console.WriteLine($"{allItems.Count} furniture items loaded from JSON.");

            int totalItems = allItems.Count;
            int processedCount = 0;

            // Limit concurrency to 20
            var semaphore = new SemaphoreSlim(20);
            var tasks = allItems.Select(async item =>
            {
                await semaphore.WaitAsync();
                try
                {
                    await ProcessItemAsync(item);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error processing item {item.id}: {ex.Message}");
                }
                finally
                {
                    int count = Interlocked.Increment(ref processedCount);
                    if (count % 100 == 0 || count == totalItems)
                    {
                        Console.WriteLine($"{count} of {totalItems} items processed.");
                    }
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            Console.WriteLine("SetOfferID process completed.");
        }

        private static async Task ProcessItemAsync(FurnitureItem item)
        {
            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                await connection.OpenAsync();

                using (var transaction = await connection.BeginTransactionAsync())
                {
                    // 1. Select the matching items_base record.
                    using (var selectCmd = new MySqlCommand("SELECT id FROM items_base WHERE sprite_id = @spriteId LIMIT 1", connection, transaction))
                    {
                        selectCmd.Parameters.AddWithValue("@spriteId", item.id);
                        object result = await selectCmd.ExecuteScalarAsync();
                        if (result == null)
                        {
                            Console.WriteLine($"No matching items_base record found for sprite_id {item.id}.");
                            return;
                        }
                        int itemBaseId = Convert.ToInt32(result);

                        // 2. Update items_base.
                        using (var updateItemsBaseCmd = new MySqlCommand("UPDATE items_base SET item_name = @classname, public_name = @classname WHERE id = @itemBaseId", connection, transaction))
                        {
                            updateItemsBaseCmd.Parameters.AddWithValue("@classname", item.classname);
                            updateItemsBaseCmd.Parameters.AddWithValue("@itemBaseId", itemBaseId);
                            int rowsAffected = await updateItemsBaseCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Updated items_base for item {item.classname} (rows affected: {rowsAffected}).");
                        }

                        // 3. Update catalog_items.
                        using (var updateCatalogCmd = new MySqlCommand("UPDATE catalog_items SET catalog_name = @catalogName, offer_id = @offerId WHERE FIND_IN_SET(@itemBaseIdStr, item_ids)", connection, transaction))
                        {
                            updateCatalogCmd.Parameters.AddWithValue("@catalogName", item.classname);
                            updateCatalogCmd.Parameters.AddWithValue("@offerId", item.offerid);
                            updateCatalogCmd.Parameters.AddWithValue("@itemBaseIdStr", itemBaseId.ToString());
                            int catalogRowsAffected = await updateCatalogCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Updated catalog_items for item {item.classname} (rows affected: {catalogRowsAffected}).");
                        }

                        await transaction.CommitAsync();
                    }
                }
                await connection.CloseAsync();
            }
        }
    }
}
