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
                Console.WriteLine("-> Download Badges                                                             ");
                Console.WriteLine("-> Download Clothes                                                            ");
                Console.WriteLine("-> Download Effects                                                            ");
                Console.WriteLine("-> Download Furnidata                                                          ");
                Console.WriteLine("-> Download Furniture                                                          ");
                Console.WriteLine("-> Download Icons                                                              ");
                Console.WriteLine("-> Download MP3                                                                ");
                Console.WriteLine("-> Download Productdata                                                        ");
                Console.WriteLine("-> Download Quests                                                             ");
                Console.WriteLine("-> Download Reception                                                          ");
                Console.WriteLine("-> Download Texts                                                              ");
                Console.WriteLine("-> Download Variables                                                          ");
                Console.WriteLine("                                                                               ");
                Console.WriteLine("-> Download All (this will download all that is required for confurting)       ");
                Console.WriteLine("                                                                               ");
                Console.WriteLine("Type \"back\" to return to the main menu.                                        ");
                Console.ResetColor();

                Console.ForegroundColor = ConsoleColor.White;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write("Command:> ");

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
                    await ReceptionDownloader.DownloadReceptionImages();
                    break;

                case "furniture":
                    await FurnitureDownloader.DownloadFurnitureAsync();
                    break;

                case "nitrofurniture":
                    await NitroFurnitureDownloader.DownloadFurnitureAsync();
                    break;

                case "nitroclothes":
                    await NitroClothesDownloader.DownloadCustomClothesAsync();
                    break;

                case "icons":
                    await IconDownloader.DownloadIcons();
                    break;

                case "clothes":
                    await ClothesDownloader.DownloadClothesAsync();
                    break;

                case "mp3":
                    await Mp3Downloader.DownloadMp3sAsync();
                    break;

                case "furnidata":
                    await FurnidataDownloader.DownloadFurnidata();
                    break;

                case "effects":
                    await EffectsDownloader.DownloadEffectsAsync();
                    break;

                case "texts":
                    await TextsDownloader.DownloadTextsAsync();
                    break;

                case "productdata":
                    await ProductDataDownloader.DownloadProductDataAsync();
                    break;

                case "variables":
                    await VariablesDownloader.DownloadVariablesAsync();
                    break;

                case "quests":
                    await QuestsDownloader.DownloadQuestsAsync();
                    break;

                case "badges":
                    await Badges.DownloadBadgesAsync();
                    break;

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
