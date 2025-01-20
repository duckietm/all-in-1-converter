using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    internal class Program
    {
        private static readonly HttpClient httpClient;

        static Program()
        {
            // Initialize HttpClient with User-Agent and configuration
            httpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 100 });
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent.Replace("User-Agent: ", ""));
        }

        private static async Task Main(string[] args)
        {
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
        }

        private static void DisplayHelp()
        {
            Console.WriteLine("Help Menu:");
            Console.WriteLine("1 - Habbo Original Downloads: Download assets from the original Habbo.");
            Console.WriteLine("2 - Nitro Custom Downloads: Download custom Nitro assets.");
            Console.WriteLine("3 - Hotel Tools: Manage and compile hotel tools.");
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
