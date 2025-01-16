namespace ConsoleApplication
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
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
        }

        private static void CheckAndCreateFolders()
        {
            if (Directory.Exists("./temp"))
            {
                foreach (string path in Directory.GetFiles("./temp"))
                    File.Delete(path);
            }
            Console.WriteLine("Checking folders");

            string[] folders = {
                "./effect", "./hof_furni", "./Nitro_hof_furni", "./quests", "./reception", "./reception/web_promo_small",
                "./badges", "./icons", "./files", "./mp3", "./clothes", "./Custom_clothes", "./merge-json",
                "./merge-json/Original_Furnidata", "./merge-json/Import_Furnidata", "./merge-json/Merged_Furnidata",
                "./merge-json/Original_Productdata", "./merge-json/Import_Productdata", "./merge-json/Merged_Productdata",
            };

            foreach (string folder in folders)
            {
                if (!Directory.Exists(folder))
                {
                    Console.WriteLine($"We need to create the {folder} folder.");
                    Directory.CreateDirectory(folder);
                    Thread.Sleep(100);
                    Console.WriteLine($"Created {folder} folder.");
                }
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
            Console.WriteLine("-> Download Badges                                                         ");
            Console.WriteLine("-> Download clothes                                                        ");
            Console.WriteLine("-> Download Effects (Download the XML off all effects)                     ");
            Console.WriteLine("-> Download Furnidata                                                      ");
            Console.WriteLine("-> Download Furniture                                                      ");
            Console.WriteLine("-> Download Icons                                                          ");
            Console.WriteLine("-> Download MP3                                                            ");
            Console.WriteLine("-> Download Productdata                                                    ");
            Console.WriteLine("-> Download Quests                                                         ");
            Console.WriteLine("-> Download Reception                                                      ");
            Console.WriteLine("-> Download Texts                                                          ");
            Console.WriteLine("-> Download Variables                                                      ");
            Console.WriteLine("-> Download MP3                                                            ");
            Console.WriteLine("-> Version                                                                 ");
            Console.WriteLine("-> ############### Nitro Custom Downloads #################                ");
            Console.WriteLine("-> Download nitroclothes                                                   ");
            Console.WriteLine("-> Download nitrofurniture                                                 ");
            Console.WriteLine("-> ############### Hotel Tools ############################                ");
            Console.WriteLine("-> Merge Furnidata                                                         ");
            Console.WriteLine("-> Merge Productdata                                                       ");
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