using System.Text.Json;
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

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Prepare the SELECT command (to get the items_base id using sprite_id).
                        var selectCmd = new MySqlCommand("SELECT id FROM items_base WHERE sprite_id = @spriteId LIMIT 1", connection, transaction);
                        var spriteIdParam = selectCmd.Parameters.Add("@spriteId", MySqlDbType.Int32);

                        // Prepare the UPDATE command for items_base.
                        var updateCmd = new MySqlCommand(
                            "UPDATE items_base SET allow_walk = @allow_walk, allow_sit = @allow_sit, allow_lay = @allow_lay WHERE id = @itemBaseId",
                            connection, transaction);
                        updateCmd.Parameters.Add("@allow_walk", MySqlDbType.VarChar, 1);
                        updateCmd.Parameters.Add("@allow_sit", MySqlDbType.VarChar, 1);
                        updateCmd.Parameters.Add("@allow_lay", MySqlDbType.VarChar, 1);
                        updateCmd.Parameters.Add("@itemBaseId", MySqlDbType.Int32);

                        foreach (var fixItem in allFixItems)
                        {
                            spriteIdParam.Value = fixItem.id;
                            object result = await selectCmd.ExecuteScalarAsync();
                            if (result == null)
                            {
                                Console.WriteLine($"No matching items_base record found for sprite_id {fixItem.id} (classname: {fixItem.classname}).");
                                continue;
                            }
                            int itemBaseId = Convert.ToInt32(result);

                            string allowWalk = fixItem.canstandon ? "1" : "0";
                            string allowSit = fixItem.cansiton ? "1" : "0";
                            string allowLay = fixItem.canlayon ? "1" : "0";

                            updateCmd.Parameters["@allow_walk"].Value = allowWalk;
                            updateCmd.Parameters["@allow_sit"].Value = allowSit;
                            updateCmd.Parameters["@allow_lay"].Value = allowLay;
                            updateCmd.Parameters["@itemBaseId"].Value = itemBaseId;

                            int rowsAffected = await updateCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Updated item : {fixItem.classname}");
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error during database update: " + ex.Message);
                        try
                        {
                            transaction.Rollback();
                        }
                        catch (Exception rbEx)
                        {
                            Console.WriteLine("Error rolling back transaction: " + rbEx.Message);
                        }
                    }
                }

                await connection.CloseAsync();
            }

            Console.WriteLine("FixItemSettings process completed.");
        }
    }
}
