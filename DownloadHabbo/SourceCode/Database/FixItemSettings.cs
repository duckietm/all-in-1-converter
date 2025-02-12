using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace ConsoleApplication.FixSettings
{
    public class FixFurnitureData
    {
        public FixRoomItemTypes roomitemtypes { get; set; }
        public FixWallItemTypes wallitemtypes { get; set; }
    }

    public class FixRoomItemTypes
    {
        public List<FixItem> furnitype { get; set; }
    }

    public class FixWallItemTypes
    {
        public List<FixItem> furnitype { get; set; }
    }

    public class FixItem
    {
        public int id { get; set; }
        public string classname { get; set; }
        public bool canstandon { get; set; }
        public bool cansiton { get; set; }
        public bool canlayon { get; set; }
    }

    public static class FixItemSettings
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

            FixFurnitureData data;
            try
            {
                data = JsonSerializer.Deserialize<FixFurnitureData>(jsonContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing JSON: " + ex.Message);
                return;
            }

            List<FixItem> allFixItems = new List<FixItem>();
            if (data?.roomitemtypes?.furnitype != null)
                allFixItems.AddRange(data.roomitemtypes.furnitype);
            if (data?.wallitemtypes?.furnitype != null)
                allFixItems.AddRange(data.wallitemtypes.furnitype);

            if (allFixItems.Count == 0)
            {
                Console.WriteLine("No fix items found in the JSON file.");
                return;
            }

            Console.WriteLine($"Loaded {allFixItems.Count} fix items from JSON.");

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
                    string createTempTableQuery = @"
                        CREATE TEMPORARY TABLE temp_fixitems (
                            id INT PRIMARY KEY,
                            allow_walk VARCHAR(1),
                            allow_sit VARCHAR(1),
                            allow_lay VARCHAR(1)
                        );";
                    using (var createTableCmd = new MySqlCommand(createTempTableQuery, connection))
                    {
                        await createTableCmd.ExecuteNonQueryAsync();
                    }

                    var spriteIds = allFixItems.Select(f => f.id).Distinct().ToList();
                    var spriteIdMap = new Dictionary<int, int>();

                    string query = $"SELECT id, sprite_id FROM items_base WHERE sprite_id IN ({string.Join(",", spriteIds)})";
                    using (var cmd = new MySqlCommand(query, connection))
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

                    const int batchSize = 1000;
                    int totalInserted = 0;

                    var batchInsertQuery = new StringBuilder();
                    batchInsertQuery.Append("INSERT IGNORE INTO temp_fixitems (id, allow_walk, allow_sit, allow_lay) VALUES ");

                    int counter = 0;
                    foreach (var fixItem in allFixItems)
                    {
                        if (spriteIdMap.TryGetValue(fixItem.id, out int itemBaseId))
                        {
                            batchInsertQuery.Append($"({itemBaseId}, '{(fixItem.canstandon ? "1" : "0")}', '{(fixItem.cansiton ? "1" : "0")}', '{(fixItem.canlayon ? "1" : "0")}'),");

                            counter++;
                            if (counter % batchSize == 0)
                            {
                                batchInsertQuery.Length--;
                                batchInsertQuery.Append(";");

                                using (var insertCmd = new MySqlCommand(batchInsertQuery.ToString(), connection))
                                {
                                    totalInserted += await insertCmd.ExecuteNonQueryAsync();
                                }

                                // Reset batch insert query
                                batchInsertQuery.Clear();
                                batchInsertQuery.Append("INSERT IGNORE INTO temp_fixitems (id, allow_walk, allow_sit, allow_lay) VALUES ");
                            }
                        }
                    }

                    if (counter % batchSize != 0)
                    {
                        batchInsertQuery.Length--;
                        batchInsertQuery.Append(";");

                        using (var insertCmd = new MySqlCommand(batchInsertQuery.ToString(), connection))
                        {
                            totalInserted += await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    Console.WriteLine($"Inserted {totalInserted} rows into temp_fixitems.");

                    string updateQuery = @"
                        UPDATE items_base AS ib
                        JOIN temp_fixitems AS tf ON ib.id = tf.id
                        SET 
                            ib.allow_walk = tf.allow_walk,
                            ib.allow_sit = tf.allow_sit,
                            ib.allow_lay = tf.allow_lay;
                    ";

                    using (var updateCmd = new MySqlCommand(updateQuery, connection))
                    {
                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                        Console.WriteLine($"Updated {rowsAffected} rows in items_base.");
                    }

                    using (var cleanupCmd = new MySqlCommand("DROP TEMPORARY TABLE IF EXISTS temp_fixitems", connection))
                    {
                        await cleanupCmd.ExecuteNonQueryAsync();
                    }

                    Console.WriteLine("FixItemSettings process completed.");
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
