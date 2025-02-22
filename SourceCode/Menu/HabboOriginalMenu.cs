using System.Text;

namespace ConsoleApplication
{
    public static class HabboOriginalMenu
    {
        public static async Task DisplayMenu()
        {
            while (true)
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("                          Habbo Original Downloads                             ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine("1 => Download Badges                                                           ");
                Console.WriteLine("2 => Download Clothes + figuredata & figuremap json and xml                    ");
                Console.WriteLine("3 => Download Effects                                                          ");
                Console.WriteLine("4 => Download Furnidata and convurt to FurnitureData.json                      ");
                Console.WriteLine("5 => Download Furniture as SWF                                                 ");
                Console.WriteLine("6 => Download Icons                                                            ");
                Console.WriteLine("7= > Download MP3                                                              ");
                Console.WriteLine("8 => Download Productdata                                                      ");
                Console.WriteLine("9 => Download Quests Images                                                    ");
                Console.WriteLine("10 => Download Reception images                                                ");
                Console.WriteLine("11 => Download Texts                                                           ");
                Console.WriteLine("12 => Download Variables                                                       ");
                Console.WriteLine("                                                                               ");
                Console.WriteLine("=> 'Download All' this will download all that is required for confurting)      ");
                Console.WriteLine("                                                                               ");
                Console.WriteLine("Type \"back\" to return to the main menu.                                        ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("Command:> ");
                Console.OutputEncoding = Encoding.UTF8;

                string input = Console.ReadLine()?.ToLower() ?? string.Empty;

                if (input == "back")
                {
                    Console.WriteLine("Returning to the main menu...");
                    break;
                }

                await HandleCommand(input);
            }
        }

        private static async Task HandleCommand(string inputData)
        {
            try
            {
                string[] starupconsole = inputData.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (starupconsole.Length == 0)
                {
                    Console.WriteLine("No command entered. Type 'help' for a list of commands.");
                    return;
                }

                switch (starupconsole[0].ToLower())
                {
                    case "download":
                        Console.WriteLine("Starting Download...");
                        if (starupconsole.Length > 1)
                        {
                            Console.WriteLine($"DEBUG: Downloading {starupconsole[1]}");
                            await HandleDownload(starupconsole[1].ToLower());
                        }
                        else
                        {
                            Console.WriteLine("Missing argument for 'download' command.");
                        }
                        break;

                    case "1":
                        Console.WriteLine("Starting Download badges!");
                        await Badges.DownloadBadgesAsync();
                        break;

                    case "2":
                        Console.WriteLine("Starting Download clothes!");
                        await ClothesDownloader.DownloadClothesAsync();
                        break;

                    case "3":
                        Console.WriteLine("Starting Download effects!");
                        await EffectsDownloader.DownloadEffectsAsync();
                        break;

                    case "4":
                        await FurnidataDownloader.DownloadFurnidata();
                        break;

                    case "5":
                        Console.WriteLine("Starting Download furniture with icons!");
                        await FurnitureDownloader.DownloadFurnitureAsync();
                        break;
                    
                    case "6":
                        Console.WriteLine("Starting Download Catalogue icons!");
                        await IconDownloader.DownloadIcons();
                        break;

                    case "7":
                        Console.WriteLine("Starting Download MP3!");
                        await Mp3Downloader.DownloadMp3sAsync();
                        break;
                    case "8":
                        Console.WriteLine("Starting Download Productdata!");
                        await ProductDataDownloader.DownloadProductDataAsync();
                        break;

                    case "9":
                        Console.WriteLine("Starting Download Quests images!");
                        await QuestsDownloader.DownloadQuestsAsync();
                        break;
                    
                    case "10":
                        Console.WriteLine("Starting Download Reception images!");
                        await ReceptionDownloader.DownloadReceptionImages();
                        break;

                    case "11":
                        Console.WriteLine("Starting Download Texts!");
                        await TextsDownloader.DownloadTextsAsync();
                        break;

                    case "12":
                        Console.WriteLine("Starting Download Variables!");
                        await VariablesDownloader.DownloadVariablesAsync();
                        break;

                    default:
                        Console.WriteLine($"Unknown command: {inputData}");
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error executing command: {ex.Message}");
            }
            finally
            {
                Console.ResetColor();
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private static async Task HandleDownload(string downloadType)
        {
            switch (downloadType)
            {
                case "reception":
                    

                case "nitrofurniture":
                    await NitroFurnitureDownloader.DownloadFurnitureAsync();
                    break;

                case "nitroclothes":
                    await NitroClothesDownloader.DownloadCustomClothesAsync();
                    break;
 
                case "texts":
                    await TextsDownloader.DownloadTextsAsync();
                    break;

                case "variables":
                    

                case "all":
                    Console.WriteLine("Starting 'Download All'...");
                    await ClothesDownloader.DownloadClothesAsync();
                    await FurnidataDownloader.DownloadFurnidata();
                    await ProductDataDownloader.DownloadProductDataAsync();
                    await FurnitureDownloader.DownloadFurnitureAsync();
                    await VariablesDownloader.DownloadVariablesAsync();
                    await TextsDownloader.DownloadTextsAsync();
                    await IconDownloader.DownloadIcons();
                    Console.WriteLine("'Download All' completed.");
                    break;

                default:
                    Console.WriteLine($"Unknown download type: {downloadType}");
                    break;
            }
        }
    }
}
