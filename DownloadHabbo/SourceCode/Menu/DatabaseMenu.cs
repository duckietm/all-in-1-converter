using ConsoleApplication.FixSettings;
using Habbo_Downloader.Compiler;
using System.Text;

namespace ConsoleApplication
{
    public static class DatabaseMenu
    {
        public static async Task DisplayMenu()
        {
            while (true)
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("                          Hotel Database Menu                                ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine("1 => Show Database General Information (version / databases etc.)            ");
                Console.WriteLine("2 => Optimize your database (Runs optimize table on all your tables)         ");
                Console.WriteLine("3 => Fix the Offer_ID in the database from the JSON                          ");
                Console.WriteLine("4 => Fix Sit / Lay / Walk in the items_base with the settings from the json  ");
                Console.WriteLine("5 => Fix Sprite_ID in the items_base from the JSON                           ");
                Console.WriteLine("                                                                             ");
                Console.WriteLine("Type \"back\" to return to the main menu.                                      ");
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
                    case "1":
                        Console.WriteLine("✅ Loading Database version!");
                        await DatabaseGeneralInfo.ShowDatabaseVersionAsync();
                        break;

                    case "2":
                        Console.WriteLine("✅ Loading Database Optimize!");
                        await DatabaseOptimizer.OptimizeDatabaseTablesAsync();
                        break;

                    case "3":
                        Console.WriteLine("✅ Loading Database and start proccessing the offer_id");
                        await SetOfferID.RunAsync();
                        break;

                    case "4":
                        Console.WriteLine("✅ Loading Database Fix Settings!");
                        await FixItemSettings.RunAsync();
                        break;

                    case "5":
                        Console.WriteLine("✅ Fixing Sprite_IDs in items_base from JSON!");
                        await FixSpriteIDSettings.RunAsync();
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
    }
}
