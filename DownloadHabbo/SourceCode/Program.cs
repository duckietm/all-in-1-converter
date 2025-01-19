namespace ConsoleApplication
{
    internal class Program
    {
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

            string configFilePath = "config.ini";
            Dictionary<string, string> config = IniFileParser.Parse(configFilePath);

            string soundMachineUrl = config.GetValueOrDefault("AppSettings:soundmachineurl", "default_soundmachineurl");
            string furnitureUrl = config.GetValueOrDefault("AppSettings:furnitureurl", "default_furnitureurl");

            ShowStartupAnimation();
            CheckAndCreateFolders();

            while (true)
            {
                DisplayMainMenu();

                string input = Console.ReadLine() ?? string.Empty;

                if (input.ToLower() == "exit")
                {
                    break;
                }

                await ConsoleCommandHandeling.InvokeCommand(input);
            }

            Console.WriteLine("Exiting the application...");
            Console.WriteLine("Press any key to close the program.");
            Console.ReadKey();
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

        private static void CheckAndCreateFolders()
        {
            Console.WriteLine("Checking folders");

            string[] baseDirectories = { "./Compiler/compile", "./Compiler/compiled", "./Compiler/extract", "./Compiler/extracted" };
            string[] subDirectories = { "furni", "clothing", "effects", "pets" };

            string[] additionalFolders = {
                "./effect", "./hof_furni", "./Nitro_hof_furni", "./quests", "./reception", "./reception/web_promo_small",
                "./badges", "./icons", "./files", "./mp3", "./clothes", "./Custom_clothes", "./merge-json", "./Generate",
                "./merge-json/Original_Furnidata", "./merge-json/Import_Furnidata", "./merge-json/Merged_Furnidata",
                "./merge-json/Original_Productdata", "./merge-json/Import_Productdata", "./merge-json/Merged_Productdata",
                "./merge-json/Original_ClothesData", "./merge-json/Import_ClothesData", "./merge-json/Merged_ClothesData",
                "./Generate/Furnidata", "./Generate/Furniture", "./Generate/Output_SQL"
            };

            foreach (string baseDir in baseDirectories)
            {
                foreach (string subDir in subDirectories)
                {
                    string fullPath = Path.Combine(baseDir, subDir);
                    CreateDirectoryIfNotExists(fullPath);
                }
            }

            foreach (string folder in additionalFolders)
            {
                CreateDirectoryIfNotExists(folder);
            }
        }

        private static void CreateDirectoryIfNotExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine($"Creating folder: {path}");
                Directory.CreateDirectory(path);
                Thread.Sleep(100); // Simulate processing delay
                Console.WriteLine($"Created folder: {path}");
            }
        }

        private static void DisplayMainMenu()
        {
            Console.ResetColor();
            Console.Clear();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("                          Available commands:                              ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.WriteLine("-> ############### Habbo Original Downloads ###############                ");
            Console.WriteLine("-> Download Badges               -> Download clothes                       ");
            Console.WriteLine("-> Download Effects              -> Download Furnidata                     ");
            Console.WriteLine("-> Download Furnidata            -> Download Furniture                     ");
            Console.WriteLine("-> Download Icons                -> Download MP3                           ");
            Console.WriteLine("-> Download Productdata          -> Download Quests                        ");
            Console.WriteLine("-> Download Reception            -> Download Texts                         ");
            Console.WriteLine("-> Download Variables            -> Download MP3                           ");
            Console.WriteLine("-> Version                                                                 ");
            Console.WriteLine("-> ############### Nitro Custom Downloads #################                ");
            Console.WriteLine("-> Download nitroclothes         -> Download nitrofurniture                ");
            Console.WriteLine("-> ############### Hotel Tools ############################                ");
            Console.WriteLine("-> Merge Clothes                 -> Merge Furnidata                        ");
            Console.WriteLine("-> Merge Productdata             -> Generate SQL                           ");
            Console.WriteLine("-> NitroFurnicompile             -> NitroFurniextract                      ");
            Console.WriteLine("-> ############### General commands #######################                ");
            Console.WriteLine("-> Help                                                                    ");
            Console.WriteLine("-> Exit                                                                    ");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine("System initialized. Ready to download!");
            Console.WriteLine("Type \"help\" for an overview of commands!");
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}
