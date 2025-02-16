namespace ConsoleApplication
{
    public static class DatabaseConfig
    {
        // Default settings
        public static string DatabaseServer { get; private set; } = "127.0.0.1";
        public static string Port { get; private set; } = "3306";
        public static string Username { get; private set; } = "root";
        public static string Password { get; private set; } = "";
        public static string DatabaseName { get; private set; } = "HABBO";

        // Exposed connection string
        public static string ConnectionString { get; private set; }

        // Static constructor to load configuration once at startup
        static DatabaseConfig()
        {
            LoadConfiguration();
            BuildConnectionString();
        }

        private static void LoadConfiguration()
        {
            string configFilePath = "config.ini";
            if (File.Exists(configFilePath))
            {
                string[] configLines = File.ReadAllLines(configFilePath);
                foreach (var line in configLines)
                {
                    if (line.StartsWith("DATABASESERVER=", StringComparison.OrdinalIgnoreCase))
                        DatabaseServer = line.Substring("DATABASESERVER=".Length).Trim();
                    else if (line.StartsWith("DATABASEPORT=", StringComparison.OrdinalIgnoreCase))
                        Port = line.Substring("DATABASEPORT=".Length).Trim();
                    else if (line.StartsWith("DATABASEUSER=", StringComparison.OrdinalIgnoreCase))
                        Username = line.Substring("DATABASEUSER=".Length).Trim();
                    else if (line.StartsWith("DATABASEPASSWORD=", StringComparison.OrdinalIgnoreCase))
                        Password = line.Substring("DATABASEPASSWORD=".Length).Trim();
                    else if (line.StartsWith("DATABASENAME=", StringComparison.OrdinalIgnoreCase))
                        DatabaseName = line.Substring("DATABASENAME=".Length).Trim();
                }
            }
            else
            {
                Console.WriteLine("config.ini file not found, using default connection settings.");
            }
        }

        private static void BuildConnectionString()
        {
            ConnectionString = $"server={DatabaseServer};port={Port};user={Username};password={Password};database={DatabaseName};";
        }
    }
}
