using MySql.Data.MySqlClient;
using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class DatabaseGeneralInfo
    {
        public static async Task ShowDatabaseVersionAsync()
        {
            // Use the centralized connection string from DatabaseConfig.
            string connectionString = DatabaseConfig.ConnectionString;

            try
            {
                using (MySqlConnection connection = new MySqlConnection(connectionString))
                {
                    await connection.OpenAsync();

                    // 1. Show Database Version
                    using (MySqlCommand versionCommand = new MySqlCommand("SELECT VERSION() AS Version", connection))
                    {
                        object versionResult = await versionCommand.ExecuteScalarAsync();
                        string version = versionResult?.ToString() ?? "Unknown";
                        Console.WriteLine($"Database Version: {version}");
                    }

                    // 2. Show all databases on the server with their sizes (in MB)
                    Console.WriteLine("\nDatabases on Server (with size in MB):");
                    try
                    {
                        string dbSizeQuery = @"
                            SELECT 
                                table_schema AS `Database`, 
                                ROUND(SUM(data_length + index_length) / 1024 / 1024, 2) AS `SizeMB`
                            FROM information_schema.TABLES
                            GROUP BY table_schema;
                        ";

                        using (MySqlCommand dbSizeCommand = new MySqlCommand(dbSizeQuery, connection))
                        {
                            using (var reader = await dbSizeCommand.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string dbName = reader["Database"].ToString();
                                    string sizeMB = reader["SizeMB"].ToString();
                                    Console.WriteLine($"💾 {dbName}, Size: {sizeMB} MB");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Could not retrieve database sizes: " + ex.Message);
                        Console.ResetColor();
                    }

                    // 3. Show all users on the server (username only)
                    Console.WriteLine("\nUsers on the Server:");
                    try
                    {
                        string usersQuery = "SELECT DISTINCT User FROM mysql.user;";
                        using (MySqlCommand usersCommand = new MySqlCommand(usersQuery, connection))
                        {
                            using (var reader = await usersCommand.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    string user = reader["User"].ToString();
                                    Console.WriteLine($"👤 Username: {user}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Could not retrieve user information: " + ex.Message);
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("An error occurred while connecting to the database:");
                Console.WriteLine(ex.Message);
                Console.ResetColor();
            }
        }
    }
}
