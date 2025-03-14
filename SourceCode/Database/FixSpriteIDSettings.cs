using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using System.Data;

namespace ConsoleApplication
{
    public static class FixSpriteIDSettings
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
                    Console.WriteLine("❌ Error reading JSON file: " + ex.Message);
                }
                return;
            }

            FixFurnitureData data;
            try
            {
                data = JsonSerializer.Deserialize<FixFurnitureData>(jsonContent);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("❌ Error deserializing JSON: " + ex.Message);
                }
                return;
            }

            if (data == null || (data.roomitemtypes?.furnitype == null && data.wallitemtypes?.furnitype == null))
            {
                lock (consoleLock)
                {
                    Console.WriteLine("❌ No valid furniture items found in JSON.");
                }
                return;
            }

            Dictionary<string, int> furnitureData = new Dictionary<string, int>();

            foreach (var item in data.roomitemtypes?.furnitype ?? Enumerable.Empty<FixItemID>())
            {
                furnitureData[item.classname] = item.id;
            }

            foreach (var item in data.wallitemtypes?.furnitype ?? Enumerable.Empty<FixItemID>())
            {
                furnitureData[item.classname] = item.id;
            }

            Console.WriteLine($"✅ Loaded {furnitureData.Count} items from JSON.");

            await UpdateDatabaseSpriteIDs(furnitureData);
        }

        private static async Task UpdateDatabaseSpriteIDs(Dictionary<string, int> furnitureData)
        {
            List<(int id, string itemName, int currentSpriteId, string publicName)> databaseItems = new List<(int, string, int, string)>();
            List<(int catalogId, string catalogName, string itemIds)> catalogItems = new List<(int, string, string)>();

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    string query = "SELECT id, item_name, sprite_id, public_name FROM items_base";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            databaseItems.Add((
                                reader.GetInt32("id"),
                                reader.GetString("item_name"),
                                reader.GetInt32("sprite_id"),
                                reader.GetString("public_name")
                            ));
                        }
                    }

                    query = "SELECT id, catalog_name, item_ids FROM catalog_items";
                    using (var cmd = new MySqlCommand(query, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            catalogItems.Add((
                                reader.GetInt32("id"),
                                reader.GetString("catalog_name"),
                                reader.GetString("item_ids")
                            ));
                        }
                    }
                }
                catch (Exception ex)
                {
                    lock (consoleLock)
                    {
                        Console.WriteLine("❌ Error fetching items from database: " + ex.Message);
                    }
                    return;
                }
            }

            if (databaseItems.Count == 0 || catalogItems.Count == 0)
            {
                lock (consoleLock)
                {
                    Console.WriteLine(databaseItems.Count == 0 ? "❌ No items found in items_base." : "❌ No items found in catalog_items.");
                }
                return;
            }

            Console.WriteLine($"✅ Loaded {databaseItems.Count} items from items_base.");
            Console.WriteLine($"✅ Loaded {catalogItems.Count} items from catalog_items.");

            // Update sprite_ids in items_base
            List<(int id, int newSpriteId)> itemsToUpdate = databaseItems
                .Where(item => furnitureData.TryGetValue(item.itemName, out var newSpriteId) && newSpriteId != item.currentSpriteId)
                .Select(item => (item.id, furnitureData[item.itemName]))
                .ToList();

            if (itemsToUpdate.Count > 0)
            {
                Console.WriteLine($"🔄 {itemsToUpdate.Count} items need sprite_id updates.");
                await UpdateSpriteIdsAsync(itemsToUpdate);
            }
            else
            {
                Console.WriteLine("✅ No sprite_id updates needed.");
            }

            // Find special items from items_base
            var wallpaperItem = databaseItems.FirstOrDefault(item => item.itemName == "wallpaper");
            var floorItem = databaseItems.FirstOrDefault(item => item.itemName == "floor");
            var landscapeItem = databaseItems.FirstOrDefault(item => item.itemName == "landscape");

            string wallpaperItemId = wallpaperItem.id > 0 ? wallpaperItem.id.ToString() : null;
            string floorItemId = floorItem.id > 0 ? floorItem.id.ToString() : null;
            string landscapeItemId = landscapeItem.id > 0 ? landscapeItem.id.ToString() : null;

            // If not found in items_base, try catalog_items
            if (wallpaperItemId == null)
            {
                var wallpaperCatalog = catalogItems.FirstOrDefault(item => item.catalogName == "wallpaper");
                wallpaperItemId = wallpaperCatalog.catalogId > 0 ? wallpaperCatalog.itemIds : null;
                if (wallpaperItemId == null)
                    Console.WriteLine("⚠️ No 'wallpaper' item found in items_base or catalog_items. Skipping wallpaper_single updates.");
            }

            if (floorItemId == null)
            {
                var floorCatalog = catalogItems.FirstOrDefault(item => item.catalogName == "floor");
                floorItemId = floorCatalog.catalogId > 0 ? floorCatalog.itemIds : null;
                if (floorItemId == null)
                    Console.WriteLine("⚠️ No 'floor' item found in items_base or catalog_items. Skipping floor_single updates.");
            }

            if (landscapeItemId == null)
            {
                var landscapeCatalog = catalogItems.FirstOrDefault(item => item.catalogName == "landscape");
                landscapeItemId = landscapeCatalog.catalogId > 0 ? landscapeCatalog.itemIds : null;
                if (landscapeItemId == null)
                    Console.WriteLine("⚠️ No 'landscape' item found in items_base or catalog_items. Skipping landscape_single updates.");
            }

            // Debug: Log items before update
            Console.WriteLine("🔍 Checking catalog_items to update:");
            var catalogItemsToUpdate = catalogItems
                .Select(catalogItem =>
                {
                    string newItemIds = null;
                    if (catalogItem.catalogName.StartsWith("wallpaper_single") && wallpaperItemId != null)
                    {
                        newItemIds = wallpaperItemId;
                    }
                    else if (catalogItem.catalogName.StartsWith("floor_single") && floorItemId != null)
                    {
                        newItemIds = floorItemId;
                    }
                    else if (catalogItem.catalogName.StartsWith("landscape_single") && landscapeItemId != null)
                    {
                        newItemIds = landscapeItemId;
                    }
                    else
                    {
                        var matchingBaseItem = databaseItems.FirstOrDefault(dbItem => dbItem.publicName == catalogItem.catalogName);
                        newItemIds = matchingBaseItem.id > 0 ? matchingBaseItem.id.ToString() : null;
                    }

                    return new
                    {
                        catalogId = catalogItem.catalogId,
                        catalogName = catalogItem.catalogName,
                        currentItemIds = catalogItem.itemIds,
                        newItemIds
                    };
                })
                .Where(x => x.newItemIds != null && x.currentItemIds != x.newItemIds)
                .Select(x => (x.catalogId, x.newItemIds, x.currentItemIds));

            foreach (var item in catalogItemsToUpdate)
            {
                Console.WriteLine($"🔍 Catalog ID: {item.catalogId}, Current item_ids: {item.currentItemIds}, New item_ids: {item.newItemIds}");
            }

            if (catalogItemsToUpdate.Any())
            {
                Console.WriteLine($"🔄 {catalogItemsToUpdate.Count()} catalog items need item_ids updates.");
                await UpdateCatalogItemIdsAsync(catalogItemsToUpdate.Select(x => (x.catalogId, x.newItemIds)).ToList());
            }
            else
            {
                Console.WriteLine("✅ No item_ids updates needed in catalog_items.");
            }
        }

        private static async Task UpdateSpriteIdsAsync(List<(int id, int newSpriteId)> itemsToUpdate)
        {
            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    int batchSize = 100;
                    int totalBatches = (int)Math.Ceiling(itemsToUpdate.Count / (double)batchSize);
                    int processedBatches = 0;

                    Console.WriteLine($"🔄 Updating {itemsToUpdate.Count} sprite_ids in {totalBatches} batches...");

                    foreach (var batch in Partition(itemsToUpdate, batchSize))
                    {
                        StringBuilder caseStatement = new StringBuilder("CASE id ");
                        List<int> ids = new List<int>();

                        foreach (var (id, newSpriteId) in batch)
                        {
                            ids.Add(id);
                            caseStatement.AppendFormat("WHEN {0} THEN {1} ", id, newSpriteId);
                        }
                        caseStatement.Append("ELSE sprite_id END");

                        string updateQuery = $"UPDATE items_base SET sprite_id = {caseStatement} WHERE id IN ({string.Join(",", ids)})";

                        try
                        {
                            using (var cmd = new MySqlCommand(updateQuery, connection))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error updating batch: {ex.Message}");
                        }

                        processedBatches++;
                        Console.WriteLine($"✅ {processedBatches}/{totalBatches} batches processed.");
                    }

                    Console.WriteLine("✅ sprite_id updates completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error updating sprite_ids: {ex.Message}");
                }
            }
        }

        private static async Task UpdateCatalogItemIdsAsync(List<(int catalogId, string newItemIds)> catalogItemsToUpdate)
        {
            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                    int batchSize = 100;
                    int totalBatches = (int)Math.Ceiling(catalogItemsToUpdate.Count / (double)batchSize);
                    int processedBatches = 0;

                    Console.WriteLine($"🔄 Updating {catalogItemsToUpdate.Count} item_ids in {totalBatches} batches...");

                    foreach (var batch in Partition(catalogItemsToUpdate, batchSize))
                    {
                        StringBuilder caseStatement = new StringBuilder("CASE id ");
                        List<int> ids = new List<int>();

                        foreach (var (catalogId, newItemIds) in batch)
                        {
                            ids.Add(catalogId);
                            caseStatement.AppendFormat("WHEN {0} THEN '{1}' ", catalogId, newItemIds);
                        }
                        caseStatement.Append("ELSE item_ids END");

                        string updateQuery = $"UPDATE catalog_items SET item_ids = {caseStatement} WHERE id IN ({string.Join(",", ids)})";

                        try
                        {
                            using (var cmd = new MySqlCommand(updateQuery, connection))
                            {
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"❌ Error updating batch: {ex.Message}");
                        }

                        processedBatches++;
                        Console.WriteLine($"✅ {processedBatches}/{totalBatches} batches processed.");
                    }

                    Console.WriteLine("✅ item_ids updates in catalog_items completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error updating item_ids in catalog_items: {ex.Message}");
                }
            }
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

    public class FixFurnitureData
    {
        public FixItemType roomitemtypes { get; set; }
        public FixItemType wallitemtypes { get; set; }
    }

    public class FixItemType
    {
        public List<FixItemID> furnitype { get; set; }
    }

    public class FixItemID
    {
        public int id { get; set; }
        public string classname { get; set; }
    }
}
