using MySql.Data.MySqlClient;

namespace ConsoleApplication
{
    public static class DatabaseOptimizer
    {
        public static async Task OptimizeDatabaseTablesAsync()
        {
            string connectionString = DatabaseConfig.ConnectionString;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    var tables = new List<string>();
                    string showTablesQuery = "SHOW TABLES;";
                    using (MySqlCommand showTablesCommand = new MySqlCommand(showTablesQuery, connection))
                    {
                        using (var reader = await showTablesCommand.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                tables.Add(reader.GetString(0));
                            }
                        }
                    }

                    if (tables.Count == 0)
                    {
                        Console.WriteLine("No tables found in the database.");
                        return;
                    }

                    Console.WriteLine($"Optimizing {tables.Count} table(s) in database '{DatabaseConfig.DatabaseName}':");

                    foreach (string table in tables)
                    {
                        string optimizeQuery = $"OPTIMIZE TABLE `{table}`;";
                        using (MySqlCommand optimizeCommand = new MySqlCommand(optimizeQuery, connection))
                        {
                            await optimizeCommand.ExecuteNonQueryAsync();
                            Console.WriteLine($"Optimized table: {table}");
                        }
                    }

                    Console.WriteLine("Database optimization complete.");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred during database optimization:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }
}
