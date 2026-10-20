using System.Globalization;
using System.Text;
using MySql.Data.MySqlClient;

namespace ConsoleApplication.FixSettings
{
    /// <summary>
    /// Database -> Fix width / length / height: for every floor and wall furni (type s or i, no pets: item_name a0...)
    /// in items_base without multiheight, sets width, length and stack_height the way Generate SQL does: a floor furni
    /// takes logic.model.dimensions x / y / z of its Habbo_Default/hof_furni/hab/&lt;name&gt;.hab, a wall furni is
    /// always 1 x 1 x 0.00. The rows that change go to Generate/Update_Item_size_&lt;date&gt;.sql; the .hab files it
    /// could not find or read are listed in Generate/Missing_Hab_files_&lt;date&gt;.txt. The database is only read.
    /// </summary>
    public static class FixItemSize
    {
        private sealed record ItemRow(int Id, string ItemName, string Type, int Width, int Length, double StackHeight);

        private readonly record struct Size(int Width, int Length, double StackHeight);

        private const string CheckedRows = "type s or i, no pets, no multiheight";

        /// <summary>A wall furni has no tiles of its own: Generate SQL writes it as 1 x 1 x 0.00.</summary>
        private static readonly Size WallSize = new(1, 1, 0.00);

        public static async Task RunAsync()
        {
            if (!HabFurniScan.CheckHabFolder()) return;

            List<ItemRow> rows = new List<ItemRow>();
            try
            {
                using var connection = new MySqlConnection(DatabaseConfig.ConnectionString);
                await connection.OpenAsync();

                const string query =
                    "SELECT id, item_name, type, width, length, stack_height FROM items_base " +
                    "WHERE " + HabFurniScan.BaseFilter + " ORDER BY id";
                using var cmd = new MySqlCommand(query, connection);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    rows.Add(new ItemRow(
                        reader.GetInt32(0),
                        reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                        reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                        reader.IsDBNull(3) ? 1 : Convert.ToInt32(reader.GetValue(3), CultureInfo.InvariantCulture),
                        reader.IsDBNull(4) ? 1 : Convert.ToInt32(reader.GetValue(4), CultureInfo.InvariantCulture),
                        reader.IsDBNull(5) ? 0 : Convert.ToDouble(reader.GetValue(5), CultureInfo.InvariantCulture)));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Could not read items_base: {ex.Message}");
                return;
            }

            var floorRows = rows.Where(row => row.Type == "s").ToList();
            Console.WriteLine($"🔍 {rows.Count} furni without multiheight (pets left out): {floorRows.Count} floor, {rows.Count - floorRows.Count} wall.");

            // Only floor furni need their bundle; wall furni always get the wall size.
            var sizes = await HabFurniScan.ReadBundlesAsync(floorRows.Select(row => row.ItemName), root =>
            {
                var dimensions = SQLGenerator.DimensionsFromNitroJson(root);
                if (dimensions == null) return (Size?)null;

                var (width, length, height) = dimensions.Value;
                // items_base keeps whole tiles and a stack height with 2 decimals.
                return new Size(
                    Math.Max(1, (int)Math.Round(width, MidpointRounding.AwayFromZero)),
                    Math.Max(1, (int)Math.Round(length, MidpointRounding.AwayFromZero)),
                    Math.Round(height, 2, MidpointRounding.AwayFromZero));
            });

            var updates = new List<(ItemRow Row, Size NewSize)>();
            var missing = new List<string>();
            foreach (var row in rows)
            {
                Size? size = row.Type == "i" ? WallSize : HabFurniScan.ResultFor(sizes, row.ItemName);
                if (size == null)
                {
                    missing.Add(row.ItemName);
                    continue;
                }

                if (size.Value.Width != row.Width || size.Value.Length != row.Length || Math.Abs(size.Value.StackHeight - row.StackHeight) > 0.001)
                {
                    updates.Add((row, size.Value));
                }
            }

            var (outputPath, missingPath) = HabFurniScan.OutputPaths("Update_Item_size");

            var sql = new StringBuilder();
            sql.AppendLine("-- width / length / stack_height from Habbo_Default/hof_furni/hab, generated " + HabFurniScan.Now);
            sql.AppendLine("-- Only floor/wall furni (type s or i, no pets 'a0*') without multiheight; wall furni are 1 x 1 x 0.00; only rows that change.");
            sql.AppendLine($"-- Checked {rows.Count} rows, {updates.Count} change, {missing.Count} have no readable .hab (see {Path.GetFileName(missingPath)}).");
            sql.AppendLine();
            foreach (var (row, size) in updates)
            {
                string height = size.StackHeight.ToString("0.00", CultureInfo.InvariantCulture);
                string oldHeight = row.StackHeight.ToString("0.00", CultureInfo.InvariantCulture);
                sql.AppendLine(
                    $"UPDATE `items_base` SET `width` = {size.Width}, `length` = {size.Length}, `stack_height` = {height} " +
                    $"WHERE `id` = {row.Id} AND (`multiheight` IS NULL OR `multiheight` IN ('', '0')); " +
                    $"-- {row.ItemName}: {row.Width}x{row.Length}x{oldHeight} -> {size.Width}x{size.Length}x{height}");
            }

            await File.WriteAllTextAsync(outputPath, sql.ToString(), new UTF8Encoding(false));

            if (missing.Count > 0) await HabFurniScan.WriteMissingListAsync(missingPath, CheckedRows, missing);

            Console.WriteLine($"✅ {updates.Count} item(s) get a new width / length / height; {missing.Count} without a .hab.");
            Console.WriteLine($"📄 {outputPath}");
            if (missing.Count > 0) Console.WriteLine($"📄 {missingPath}");
            Console.WriteLine("   Check the file, then run it on your database and restart the emulator (or reload the items).");
        }
    }
}
