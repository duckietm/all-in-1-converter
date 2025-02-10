using System.Text.Json;
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
                        // Prepare a command to select the items_base id based on sprite_id.
                        var selectCmd = new MySqlCommand("SELECT id FROM items_base WHERE sprite_id = @spriteId LIMIT 1", connection, transaction);
                        var spriteIdParam = selectCmd.Parameters.Add("@spriteId", MySqlDbType.Int32);

                        // Prepare the update command for items_base.
                        var updateItemsBaseCmd = new MySqlCommand(
                            "UPDATE items_base SET item_name = @classname, public_name = @classname WHERE id = @itemBaseId",
                            connection, transaction);
                        updateItemsBaseCmd.Parameters.Add("@classname", MySqlDbType.VarChar, 70);
                        updateItemsBaseCmd.Parameters.Add("@itemBaseId", MySqlDbType.Int32);

                        // Prepare the update command for catalog_items.
                        var updateCatalogCmd = new MySqlCommand(
                            "UPDATE catalog_items SET catalog_name = @catalogName, offer_id = @offerId WHERE FIND_IN_SET(@itemBaseIdStr, item_ids)",
                            connection, transaction);
                        updateCatalogCmd.Parameters.Add("@catalogName", MySqlDbType.VarChar, 100);
                        updateCatalogCmd.Parameters.Add("@offerId", MySqlDbType.Int32);
                        updateCatalogCmd.Parameters.Add("@itemBaseIdStr", MySqlDbType.VarChar, 10);

                        foreach (var item in allItems)
                        {
                            spriteIdParam.Value = item.id;
                            object result = await selectCmd.ExecuteScalarAsync();
                            if (result == null)
                            {
                                Console.WriteLine($"No matching items_base record found for sprite_id {item.id}.");
                                continue;
                            }
                            int itemBaseId = Convert.ToInt32(result);

                            updateItemsBaseCmd.Parameters["@classname"].Value = item.classname;
                            updateItemsBaseCmd.Parameters["@itemBaseId"].Value = itemBaseId;
                            int rowsAffected = await updateItemsBaseCmd.ExecuteNonQueryAsync();
                            Console.WriteLine($"Updated item {item.classname} ");

                            updateCatalogCmd.Parameters["@catalogName"].Value = item.classname;
                            updateCatalogCmd.Parameters["@offerId"].Value = item.offerid;
                            updateCatalogCmd.Parameters["@itemBaseIdStr"].Value = itemBaseId.ToString();
                            int catalogRowsAffected = await updateCatalogCmd.ExecuteNonQueryAsync();
                        }

                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error during database updates: " + ex.Message);
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

            Console.WriteLine("SetOfferID process completed.");
        }
    }
}
