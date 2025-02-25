using System.Text;

namespace ConsoleApplication
{
    internal class Program
    {
        private static readonly HttpClient httpClient;

        static Program()
        {
            httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 100 });
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent.Replace("User-Agent: ", ""));
        }
        private static bool IsJavaAvailable()
        {
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "java",
                        Arguments = "-version",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();

                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }


        private static async Task Main(string[] args)
        {
            if (!IsJavaAvailable())
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Java is not installed or not accessible from the command line.");
                Console.WriteLine("Java is required for the SQL Generator to function properly.");
                Console.WriteLine("Please download and install the latest version of Java from:");
                Console.WriteLine("https://www.java.com/en/download/");
                Console.ResetColor();
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
                return;
            }

            ShowStartupAnimation();
            CreateDirectories();

            while (true)
            {
                DisplayMainMenu();

                string input = Console.ReadLine()?.ToLower() ?? string.Empty;

                switch (input)
                {
                    case "1":
                        await HabboOriginalMenu.DisplayMenu();
                        break;
                    case "2":
                        await NitroCustomMenu.DisplayMenu();
                        break;
                    case "3":
                        await HotelToolsMenu.DisplayMenu();
                        break;
                    case "4":
                        await DatabaseMenu.DisplayMenu();
                        break;
                    case "help":
                        DisplayHelp();
                        break;
                    case "version":
                        await DisplayVersionAsync();
                        break;
                    case "exit":
                        Console.WriteLine("Exiting the program...");
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Please try again.");
                        break;
                }
            }
        }

        private static void CreateDirectories()
        {
            var directoryStructure = new Dictionary<string, string[]>
            {
                { "./NitroCompiler/compile", new[] { "furni", "clothing", "effects", "pets" } },
                { "./NitroCompiler/compiled", new[] { "furni", "clothing", "effects", "pets" } },
                { "./NitroCompiler/extract", new[] { "furni", "clothing", "effects", "pets" } },
                { "./NitroCompiler/extracted", new[] { "furni", "clothing", "effects", "pets" } },
                { "./Habbo_Default", new[] { "badges", "clothes", "files", "files/xml", "files/json", "hof_furni", "hof_furni/icons", "icons", "mp3", "quests", "reception", "reception/web_promo_small" } },
                { "./Merge", new[] { "Original_Furnidata", "Import_Furnidata", "Merged_Furnidata", "Original_ClothesData", "Import_ClothesData", "Merged_ClothesData", "Original_Productdata", "Import_Productdata", "Merged_Productdata" } },
                { "./Generate", new[] { "Furnidata", "Furniture", "Output_SQL" } },
                { "./SWFCompiler", new[] { "clothes", "furniture", "import", "import/clothes", "import/furniture", "import/pets", "import/effects" } },
                { "./Database", new[] { "Variables" } },
                { "./custom_downloads", new[] { "clothes", "nitro_furniture", "nitro_furniture/icons" } }
            };

            try
            {
                foreach (var (baseDir, subDirs) in directoryStructure)
                {
                    CreateDirectoryAndSubdirectories(baseDir, subDirs);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error creating directories: {ex.Message}");
                Console.ResetColor();
            }
        }

        private static void CreateDirectoryAndSubdirectories(string baseDir, string[] subDirs)
        {
            Directory.CreateDirectory(baseDir);

            foreach (var subDir in subDirs)
            {
                string fullPath = Path.Combine(baseDir, subDir);
                Directory.CreateDirectory(fullPath);
            }
        }

        private static void ShowStartupAnimation()
        {
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("          ***** DuckieTM 64BIT BASIC V2.0 The New Generation *****         ");
            Console.WriteLine("                   64K RAM SYSTEM 38911 BASIC BYTES FREE                   ");
            Console.WriteLine("READY.                                                                     ");
            string myText = "LOAD " + (char)34 + "Habboloader" + (char)34 + ",8,1";
            for (int i = 0; i < myText.Length; i++)
            {
                Console.Write(myText[i]);
                Thread.Sleep(10);
            }
            Console.WriteLine("                                                     ");
            Console.WriteLine("LOADING                                                                    ");
            Thread.Sleep(150);
            Console.WriteLine("READY.                                                                     ");
            string myText1 = "SYS 2048";
            for (int i = 0; i < myText1.Length; i++)
            {
                Console.Write(myText1[i]);
                Thread.Sleep(10);
            }
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("                                                                           ");
            Console.WriteLine("Lets get the Show started : Booter loading....                             ");
            Console.WriteLine("                                                                           ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Title = "Loading The core ....";
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            Thread.Sleep(2000);
            Console.Clear();
        }

        private static void DisplayMainMenu()
        {
            Console.ResetColor();
            Console.Clear();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("                          Main Menu: Select a Topic                        ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.WriteLine("1. Habbo Original Downloads                                                ");
            Console.WriteLine("2. Nitro Custom Downloads                                                  ");
            Console.WriteLine("3. Hotel Tools                                                             ");
            Console.WriteLine("4. Database Tools                                                          ");
            Console.WriteLine("                                                                           ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" Type 1, 2 or 3 in the command to go to the required section               ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("                                                                           ");
            Console.WriteLine("-> Help                                                                    ");
            Console.WriteLine("-> Version (Shows habbo version used for download assets)                  ");
            Console.WriteLine("-> Exit                                                                    ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Write("Command:> ");
            Console.OutputEncoding = Encoding.UTF8;
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Help Menu:");
            Console.WriteLine("1 - Habbo Original Downloads: Download assets from the original Habbo.");
            Console.WriteLine("2 - Nitro Custom Downloads: Download custom Nitro assets.");
            Console.WriteLine("3 - Hotel Tools: Manage and compile hotel tools.");
            Console.WriteLine("4 - Database Tools: Manage tools for your database.");
            Console.WriteLine("Type 'version' to display the current Habbo release version.");
            Console.WriteLine("Type 'exit' to quit the application.");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey();
        }

        private static async Task DisplayVersionAsync()
        {
            Console.WriteLine("Fetching the current Habbo version...");
            string version = await GetHabboVersionAsync();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Current Habbo Version: {version}");
            Console.ResetColor();
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey();
        }

        private static async Task<string> GetHabboVersionAsync()
        {
            try
            {
                string externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/1";
                string source = await httpClient.GetStringAsync(externalVariablesUrl);

                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (line.Contains("flash.client.url="))
                    {
                        return line.Substring(0, line.Length - 1).Split('/')[4];
                    }
                }

                return "Unknown";
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error fetching Habbo version: {ex.Message}");
                Console.ResetColor();
                return "Unknown";
            }
        }
    }

    public static class UserAgentClass
    {
        public static string UserAgent { get; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
    }
}
