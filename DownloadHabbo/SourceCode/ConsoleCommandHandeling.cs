using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class CommonConfig
    {
        public static readonly string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.3";
    }

    internal class ConsoleCommandHandeling
    {
        internal static async Task InvokeCommand(string inputData)
        {
            Console.WriteLine();
            try
            {
                // Split the input into parts and remove empty entries
                string[] starupconsole = inputData.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Check if the input is empty
                if (starupconsole.Length == 0)
                {
                    Console.WriteLine("No command entered. Type 'help' for a list of commands.");
                    return;
                }

                // Process the command
                switch (starupconsole[0].ToLower())
                {
                    // Downloads 
                    case "download":
                        Console.WriteLine("Starting Download...");
                        if (starupconsole.Length > 1)
                        {
                            switch (starupconsole[1].ToLower())
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

                                default:
                                    unknownCommand(inputData);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Missing argument for 'download' command.");
                        }
                        break;

                    // TOOLS
                    case "merge":
                        if (starupconsole.Length > 1)
                        {
                            switch (starupconsole[1].ToLower())
                            {
                                case "furnidata":
                                    await CompareFurnidata.Compare();
                                    break;

                                case "productdata":
                                    await CompareProductData.Compare();
                                    break;

                                case "clothes":
                                    await CompareClothesData.Compare();
                                    break;

                                default:
                                    unknownCommand(inputData);
                                    break;
                            }
                        }
                        else
                        {
                            Console.WriteLine("Missing argument for 'merge' command.");
                        }
                        break;

                    // Default
                    case "help":
                        HelpCommand.DisplayHelp();
                        break;

                    case "exit":
                        Environment.Exit(1);
                        break;

                    case "about":
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("All from the Habbo community !");
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;

                    case "version":
                        await VersionChecker.CheckVersionAsync();
                        break;

                    default:
                        unknownCommand(inputData);
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error in command [" + inputData + "]: " + ex.ToString());
            }

            Console.WriteLine();
            Console.WriteLine("Press any key to return to the menu...");
            Console.ReadKey();
        }

        private static void unknownCommand(string command)
        {
            Console.WriteLine(command + " is an unknown or unsupported command.");
        }
    }
}