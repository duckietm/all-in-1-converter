using System.Text;
using System.Text.Json;
using MySql.Data.MySqlClient;
using System.Data;

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

            FixFurnitureData data;
            try
            {
                data = JsonSerializer.Deserialize<FixFurnitureData>(jsonContent);
            }
            catch (Exception ex)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("Error deserializing JSON: " + ex.Message);
                }
                return;
            }

            List<FixItem> allFixItems = new List<FixItem>();
            if (data?.roomitemtypes?.furnitype != null)
                allFixItems.AddRange(data.roomitemtypes.furnitype);
            if (data?.wallitemtypes?.furnitype != null)
                allFixItems.AddRange(data.wallitemtypes.furnitype);

            if (allFixItems.Count == 0)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("No fix items found in the JSON file.");
                }
                return;
            }

            int totalItems = allFixItems.Count;
            lock (consoleLock)
            {
                Console.WriteLine($"🔍 Loaded {totalItems} fix items from JSON.");
            }

            Dictionary<int, int> spriteIdToItemBaseId = new Dictionary<int, int>();

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                try
                {
                    await connection.OpenAsync();

                    var spriteIds = allFixItems.Select(f => f.id).Distinct().ToList();
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

            List<(int itemBaseId, FixItem fixItem)> itemsToUpdate = allFixItems
                .Where(f => spriteIdToItemBaseId.ContainsKey(f.id))
                .Select(f => (spriteIdToItemBaseId[f.id], f))
                .ToList();

            int totalToUpdate = itemsToUpdate.Count;
            if (totalToUpdate == 0)
            {
                lock (consoleLock)
                {
                    Console.WriteLine("No matching items_base records found for any fix items.");
                }
                return;
            }

            int processedBatches = 0;
            int totalBatches = (int)Math.Ceiling(totalToUpdate / 100.0);
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

            var batches = Partition(itemsToUpdate, 100);

            using (var connection = new MySqlConnection(DatabaseConfig.ConnectionString))
            {
                await connection.OpenAsync();
                foreach (var batch in batches)
                {
                    StringBuilder caseAllowWalk = new StringBuilder("CASE id ");
                    StringBuilder caseAllowSit = new StringBuilder("CASE id ");
                    StringBuilder caseAllowLay = new StringBuilder("CASE id ");
                    List<int> ids = new List<int>();

                    foreach (var (itemBaseId, fixItem) in batch)
                    {
                        ids.Add(itemBaseId);
                        string allowWalk = fixItem.canstandon ? "1" : "0";
                        string allowSit = fixItem.cansiton ? "1" : "0";
                        string allowLay = fixItem.canlayon ? "1" : "0";

                        caseAllowWalk.AppendFormat("WHEN {0} THEN '{1}' ", itemBaseId, allowWalk);
                        caseAllowSit.AppendFormat("WHEN {0} THEN '{1}' ", itemBaseId, allowSit);
                        caseAllowLay.AppendFormat("WHEN {0} THEN '{1}' ", itemBaseId, allowLay);
                    }
                    caseAllowWalk.Append("ELSE allow_walk END");
                    caseAllowSit.Append("ELSE allow_sit END");
                    caseAllowLay.Append("ELSE allow_lay END");

                    string updateQuery = "UPDATE items_base SET " +
                        "allow_walk = " + caseAllowWalk.ToString() + ", " +
                        "allow_sit = " + caseAllowSit.ToString() + ", " +
                        "allow_lay = " + caseAllowLay.ToString() +
                        " WHERE id IN (" + string.Join(",", ids) + ")";

                    try
                    {
                        using (var cmd = new MySqlCommand(updateQuery, connection))
                        {
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        lock (consoleLock)
                        {
                            Console.WriteLine($"\nError updating batch: {ex.Message}");
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
                // Write final status line.
                string progressBar = BuildProgressBar(totalBatches, totalBatches, 50);
                Console.WriteLine($"\r{progressBar} 100.00% ({totalBatches}/{totalBatches} batches processed)");
                Console.WriteLine("\nFixItemSettings process completed.");
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
