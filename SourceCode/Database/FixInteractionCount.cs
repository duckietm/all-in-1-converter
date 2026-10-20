using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;

namespace ConsoleApplication.FixSettings
{
    /// <summary>
    /// Database -> Fix interaction count: reads every floor and wall furni (type s or i, no pets: item_name a0...) in
    /// items_base whose interaction_type is 'default' and that has no multiheight, counts the interaction modes of
    /// its Habbo_Default/hof_furni/hab/&lt;name&gt;.hab the same way Generate SQL does, and writes the rows whose
    /// count differs to Generate/Update_Interaction_count_&lt;date&gt;.sql.
    /// The .hab files it could not find or read are listed in Generate/Missing_Hab_files_&lt;date&gt;.txt.
    /// The database itself is only read.
    /// </summary>
    public static class FixInteractionCount
    {
        private sealed record ItemRow(int Id, string ItemName, int CurrentCount);

        private const string CheckedRows = "type s or i, no pets, interaction_type 'default', no multiheight";

        public static async Task RunAsync()
        {
            if (!HabFurniScan.CheckHabFolder()) return;

            List<ItemRow> rows = new List<ItemRow>();
            try
            {
                using var connection = new MySqlConnection(DatabaseConfig.ConnectionString);
                await connection.OpenAsync();

                const string query =
                    "SELECT id, item_name, interaction_modes_count FROM items_base " +
                    "WHERE interaction_type = 'default' AND " + HabFurniScan.BaseFilter + " ORDER BY id";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new ItemRow(
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2), CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Could not read items_base: {ex.Message}");
                return;
            }

            Console.WriteLine($"🔍 {rows.Count} floor and wall furni with interaction_type 'default' and no multiheight (pets left out).");

            var counts = await HabFurniScan.ReadBundlesAsync(rows.Select(row => row.ItemName),
                root => (int?)SQLGenerator.InteractionModesCountFromNitroJson(root));

            var updates = new List<(ItemRow Row, int NewCount)>();
            var missing = new List<string>();
            foreach (var row in rows)
            {
                int? count = HabFurniScan.ResultFor(counts, row.ItemName);
                if (count == null)
                {
                    missing.Add(row.ItemName);
                    continue;
                }

                if (count.Value != row.CurrentCount) updates.Add((row, count.Value));
            }

            var (outputPath, missingPath) = HabFurniScan.OutputPaths("Update_Interaction_count");

            var sql = new StringBuilder();
            sql.AppendLine("-- interaction_modes_count from Habbo_Default/hof_furni/hab, generated " + HabFurniScan.Now);
            sql.AppendLine("-- Only floor/wall furni (type s or i, no pets 'a0*') with interaction_type = 'default' and no multiheight; only rows whose count changes.");
            sql.AppendLine($"-- Checked {rows.Count} rows, {updates.Count} change, {missing.Count} have no readable .hab (see {Path.GetFileName(missingPath)}).");
            sql.AppendLine();
            foreach (var (row, newCount) in updates)
            {
                sql.AppendLine($"UPDATE `items_base` SET `interaction_modes_count` = {newCount} WHERE `id` = {row.Id} AND `interaction_type` = 'default'; -- {row.ItemName}: {row.CurrentCount} -> {newCount}");
            }

            await File.WriteAllTextAsync(outputPath, sql.ToString(), new UTF8Encoding(false));

            if (missing.Count > 0) await HabFurniScan.WriteMissingListAsync(missingPath, CheckedRows, missing);

            Console.WriteLine($"✅ {updates.Count} item(s) get a new interaction_modes_count; {missing.Count} without a .hab.");
            Console.WriteLine($"📄 {outputPath}");
            if (missing.Count > 0) Console.WriteLine($"📄 {missingPath}");
            Console.WriteLine("   Check the file, then run it on your database and restart the emulator (or reload the items).");
        }
    }
}
