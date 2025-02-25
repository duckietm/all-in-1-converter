using Habbo_Downloader.Compiler;
using System.Text;

namespace ConsoleApplication
{
    public static class HotelToolsMenu
    {
        public static async Task DisplayMenu()
        {
            while (true)
            {
                Console.ResetColor();
                Console.Clear();
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("                          Hotel Tools Menu                                   ");
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Gray;
                Console.WriteLine("1 => Merge Furnidata                                                         ");
                Console.WriteLine("2 => Merge Productdata                                                       ");
                Console.WriteLine("3 => Merge Clothes                                                           ");
                Console.WriteLine("4 => Generate SQL                                                            ");
                Console.WriteLine("5 => Decompile NitroFil                                                      ");
                Console.WriteLine("6 => Compile NitroFiles                                                      ");
                Console.WriteLine("7 => SWF Furniture to Nitro                                                  ");
                Console.WriteLine("8 => SWF Clothes to Nitro                                                    ");
                Console.WriteLine("9 => SWF Pets to Nitro                                                       ");
                Console.WriteLine("10 => SWF Effects to Nitro                                                   ");
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
                        await CompareFurnidata.Compare();
                        break;

                    case "2":
                        await CompareProductData.Compare();
                        break;

                    case "3":
                        await CompareClothesData.Compare();
                        break;

                    case "4":
                        SQLGenerator.GenerateSQL();
                        break;

                    case "5":
                        Console.WriteLine("DEBUG: Decompiling NitroFiles...");
                        await NitroExtractor.Extract();
                        break;

                    case "6":
                        Console.WriteLine("DEBUG: Compiling NitroFiles...");
                        await NitroFurniCompile.Compile();
                        break;

                    case "7":
                        Console.WriteLine("DEBUG: Converting SWF Furniture to Nitro...");
                        await SWF_Furni_To_Nitro.ConvertSwfFilesAsync();
                        break;

                    case "8":
                        Console.WriteLine("DEBUG: Converting SWF Clothes to Nitro...");
                        await SWF_clothes_To_Nitro.ConvertSwfFilesAsync();
                        break;

                    case "9":
                        Console.WriteLine("DEBUG: Converting SWF Pets to Nitro...");
                        await SWF_Pets_To_Nitro.RunDecoderPipelineAsync();
                        break;

                    case "10":
                        Console.WriteLine("DEBUG: Converting SWF Pets to Nitro...");
                        await SWF_Effects_To_Nitro.ConvertSwfFilesAsync();
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
