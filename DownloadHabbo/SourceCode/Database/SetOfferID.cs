using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

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
            const string jsonFilePath = "./Database/Variables/FurnitureData.json";
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

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error opening database connection: " + ex.Message);
                    return;
                }

                try
                {
                    // Step 1: Fetch all sprite_id mappings at once
                    var spriteIds = allItems.Select(f => f.id).Distinct().ToList();
                    var spriteIdMap = new Dictionary<int, int>();

                    string selectQuery = $"SELECT id, sprite_id FROM items_base WHERE sprite_id IN ({string.Join(",", spriteIds)})";
                    using (var cmd = new MySqlCommand(selectQuery, connection))
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            spriteIdMap[reader.GetInt32(1)] = reader.GetInt32(0);
                        }
                    }

                    if (spriteIdMap.Count == 0)
                    {
                        Console.WriteLine("No matching sprite IDs found in items_base.");
                        return;
                    }

                    // Step 2: Create a Temporary Table
                    string createTempTableQuery = @"
                        CREATE TEMPORARY TABLE temp_furniture_updates (
                            id INT PRIMARY KEY,
                            classname VARCHAR(100),
                            offer_id INT
                        );";

                    using (var createTableCmd = new MySqlCommand(createTempTableQuery, connection))
                    {
                        await createTableCmd.ExecuteNonQueryAsync();
                    }

                    // Step 3: Insert Data in Batches
                    const int batchSize = 1000;
                    int totalInserted = 0;

                    var batchInsertQuery = new StringBuilder();
                    batchInsertQuery.Append("INSERT IGNORE INTO temp_furniture_updates (id, classname, offer_id) VALUES ");

                    int counter = 0;
                    foreach (var item in allItems)
                    {
                        if (spriteIdMap.TryGetValue(item.id, out int itemBaseId))
                        {
                            batchInsertQuery.Append($"({itemBaseId}, '{item.classname}', {item.offerid}),");

                            counter++;
                            if (counter % batchSize == 0)
                            {
                                batchInsertQuery.Length--; // Remove last comma
                                batchInsertQuery.Append(";");

                                using (var insertCmd = new MySqlCommand(batchInsertQuery.ToString(), connection))
                                {
                                    totalInserted += await insertCmd.ExecuteNonQueryAsync();
                                }

                                batchInsertQuery.Clear();
                                batchInsertQuery.Append("INSERT IGNORE INTO temp_furniture_updates (id, classname, offer_id) VALUES ");
                            }
                        }
                    }

                    // Insert remaining records if any
                    if (counter % batchSize != 0)
                    {
                        batchInsertQuery.Length--; // Remove last comma
                        batchInsertQuery.Append(";");

                        using (var insertCmd = new MySqlCommand(batchInsertQuery.ToString(), connection))
                        {
                            totalInserted += await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    Console.WriteLine($"Inserted {totalInserted} rows into temp_furniture_updates.");

                    // Step 4: Perform Bulk Updates
                    string updateItemsBaseQuery = @"
                        UPDATE items_base AS ib
                        JOIN temp_furniture_updates AS tf ON ib.id = tf.id
                        SET 
                            ib.item_name = tf.classname,
                            ib.public_name = tf.classname;
                    ";

                    using (var updateCmd = new MySqlCommand(updateItemsBaseQuery, connection))
                    {
                        int rowsUpdated = await updateCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"Updated {rowsUpdated} rows in items_base.");
                    }

                    string updateCatalogQuery = @"
                        UPDATE catalog_items AS ci
                        JOIN temp_furniture_updates AS tf ON FIND_IN_SET(tf.id, ci.item_ids)
                        SET 
                            ci.catalog_name = tf.classname,
                            ci.offer_id = tf.offer_id;
                    ";

                    using (var updateCmd = new MySqlCommand(updateCatalogQuery, connection))
                    {
                        int catalogRowsUpdated = await updateCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"Updated {catalogRowsUpdated} rows in catalog_items.");
                    }

                    // Step 5: Clean up temporary table
                    using (var cleanupCmd = new MySqlCommand("DROP TEMPORARY TABLE IF EXISTS temp_furniture_updates", connection))
                    {
                        await cleanupCmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine("SetOfferID process completed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error during database update: " + ex.Message);
                }
                finally
                {
                    await connection.CloseAsync();
                }
            }
        }
    }
}
