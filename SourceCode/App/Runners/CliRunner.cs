using System;
using System.Net.Http;
using System.Threading.Tasks;
using ConsoleApplication;
using Habbo_Downloader.App.Menus;

namespace Habbo_Downloader.App.Runners
{
    /// <summary>
    /// Classic console runner. Uses the shared MenuHost so the main menu picks up
    /// the same look as every sub-menu (red header / gray rows / "Command:&gt;" prompt).
    /// </summary>
    public sealed class CliRunner : IAppRunner
    {
        public async Task RunAsync(Args args)
        {
            if (!string.IsNullOrEmpty(args.Command))
            {
                await DispatchDirect(args.Command);
                return;
            }

            await MenuHost.ShowAsync("Main Menu: Select a Topic", new MenuItem[]
            {
                new("1",       "Habbo Original Downloads",            HabboOriginalMenu.DisplayMenu, IsSubMenu: true),
                new("2",       "Nitro Custom Downloads",              NitroCustomMenu.DisplayMenu,   IsSubMenu: true),
                new("3",       "Hotel Tools",                         HotelToolsMenu.DisplayMenu,    IsSubMenu: true),
                new("4",       "Database Tools",                      DatabaseMenu.DisplayMenu,      IsSubMenu: true),
                new("version", "Fetch current Habbo client version",  DisplayVersionAsync),
                new("help",    "Show help",                           DisplayHelpAsync),
            }, isTopLevel: true);
        }

        private static async Task DispatchDirect(string command)
        {
            switch (command.ToLowerInvariant())
            {
                case "habbo":    await HabboOriginalMenu.DisplayMenu(); break;
                case "nitro":    await NitroCustomMenu.DisplayMenu(); break;
                case "tools":    await HotelToolsMenu.DisplayMenu(); break;
                case "database": await DatabaseMenu.DisplayMenu(); break;
                default:
                    Console.Error.WriteLine($"Unknown command: {command}. Valid: habbo, nitro, tools, database.");
                    Environment.ExitCode = 2;
                    break;
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
                System.Threading.Thread.Sleep(10);
            }
            Console.WriteLine("                                                     ");
            Console.WriteLine("LOADING                                                                    ");
            System.Threading.Thread.Sleep(150);
            Console.WriteLine("READY.                                                                     ");
            string myText1 = "SYS 2048";
            for (int i = 0; i < myText1.Length; i++)
            {
                Console.Write(myText1[i]);
                System.Threading.Thread.Sleep(10);
            }
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("                                                                           ");
            Console.WriteLine("Lets get the Show started : Booter loading....                             ");
            Console.WriteLine("                                                                           ");
            Console.ForegroundColor = ConsoleColor.Gray;
            if (OperatingSystem.IsWindows()) Console.Title = "Loading The core ....";
            Console.WriteLine("");
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            System.Threading.Thread.Sleep(2000);
            Console.Clear();
        }

        private static Task DisplayHelpAsync()
        {
            Console.WriteLine("Help Menu:");
            Console.WriteLine("1 - Habbo Original Downloads: Download assets from the original Habbo.");
            Console.WriteLine("2 - Nitro Custom Downloads: Download custom Nitro assets.");
            Console.WriteLine("3 - Hotel Tools: Manage and compile hotel tools.");
            Console.WriteLine("4 - Database Tools: Manage tools for your database.");
            Console.WriteLine("Type 'version' to display the current Habbo release version.");
            Console.WriteLine("Type 'exit' to quit the application.");
            return Task.CompletedTask;
        }

        private static readonly HttpClient HttpClient = new HttpClient(new HttpClientHandler { MaxConnectionsPerServer = 100 });

        public static async Task DisplayVersionAsync()
        {
            Console.WriteLine("Fetching the current Habbo version...");
            string version = await GetHabboVersionAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Current Habbo Version: {version}");
            Console.ResetColor();
        }

        private static async Task<string> GetHabboVersionAsync()
        {
            try
            {
                if (HttpClient.DefaultRequestHeaders.UserAgent.Count == 0)
                    HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgentClass.UserAgent.Replace("User-Agent: ", ""));
                string externalVariablesUrl = "https://www.habbo.com/gamedata/external_variables/1";
                string source = await HttpClient.GetStringAsync(externalVariablesUrl);
                foreach (string line in source.Split(Environment.NewLine.ToCharArray()))
                {
                    if (line.Contains("flash.client.url="))
                        return line.Substring(0, line.Length - 1).Split('/')[4];
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
}
