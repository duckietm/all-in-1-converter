using System;
using System.IO;
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
                string[] starupconsole = inputData.Split(new char[1] { ' ' });
                switch (starupconsole[0].ToLower())
                {
                    case "download":
                        Console.WriteLine("Starting Download...");
                        switch (starupconsole[1].ToLower())
                        {
                            case "reception":
                                ReceptionDownloader.DownloadReceptionImages();
                                break;

                            case "furniture":
                                FurnitureDownloader.DownloadFurniture();
                                break;

                            case "nitrofurniture":
                                NitroFurnitureDownloader.DownloadFurniture();
                                break;

                            case "nitroclothes":
                                Task.Run(async () => await NitroClothesDownloader.DownloadCustomClothesAsync()).Wait();
                                break;

                            case "icons":
                                Task task = IconDownloader.DownloadIcons();
                                task.Wait();
                                break;

                            case "clothes":
                                Task.Run(async () => await ClothesDownloader.DownloadClothesAsync()).Wait();
                                break;

                            case "mp3":
                                Mp3Downloader.DownloadMp3s();
                                break;

                            case "furnidata":
                                await FurnidataDownloader.DownloadFurnidata();
                                break;

                            case "effects":
                                Task.Run(async () => await EffectsDownloader.DownloadEffectsAsync()).Wait();
                                break;

                            case "texts":
                                TextsDownloader.DownloadTexts();
                                break;

                            case "custfurni":
                                CustomFurniDownloader.DownloadCustomFurni();
                                break;

                            case "productdata":
                                ProductDataDownloader.DownloadProductData();
                                break;

                            case "variables":
                                VariablesDownloader.DownloadVariables();
                                break;

                            case "quests":
                                QuestsDownloader.DownloadQuests();
                                break;

                            case "badges":
                                Badges.DownloadBadges();
                                break;

                            default:
                                unknownCommand(inputData);
                                break;
                        }
                        break;

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
                        Task.Run(async () => await VersionChecker.CheckVersionAsync()).Wait();
                        break;

                    case "customclothes":
                        Task.Run(async () => await NitroClothesDownloader.DownloadCustomClothesAsync()).Wait();
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