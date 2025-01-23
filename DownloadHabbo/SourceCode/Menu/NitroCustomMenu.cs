using System;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class NitroCustomMenu
    {
        public static async Task DisplayMenu()
        {
            while (true)
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("                          Nitro Custom Downloads                           ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine("-> Download NitroFurniture                                                 ");
                Console.WriteLine("-> Download NitroClothes                                                   ");
                Console.WriteLine("                                                                           ");
                Console.WriteLine("Type \"back\" to return to the main menu.                                    ");
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
                case "nitrofurniture":
                    await NitroFurnitureDownloader.DownloadFurnitureAsync();
                    break;

                case "nitroclothes":
                    await NitroClothesDownloader.DownloadCustomClothesAsync();
                    break;

                default:
                    Console.WriteLine($"Unknown download type: {downloadType}");
                    break;
            }
        }
    }
}
