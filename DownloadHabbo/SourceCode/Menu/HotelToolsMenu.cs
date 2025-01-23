using Habbo_Downloader.Compiler;

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
                Console.WriteLine("-> Merge Furnidata                                                           ");
                Console.WriteLine("-> Merge Productdata                                                         ");
                Console.WriteLine("-> Merge Clothes                                                             ");
                Console.WriteLine("-> Generate SQL                                                              ");
                Console.WriteLine("-> Decompile NitroFiles                                                      ");
                Console.WriteLine("-> Compile NitroFiles                                                        ");
                Console.WriteLine("-> Convert SWF to Nitro                                                      ");
                Console.WriteLine("                                                                             ");
                Console.WriteLine("Type \"back\" to return to the main menu.                                      ");
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
                    case "merge":
                        if (starupconsole.Length > 1)
                        {
                            Console.WriteLine($"DEBUG: Merging {starupconsole[1]}...");
                            await HandleMerge(starupconsole[1].ToLower());
                        }
                        else
                        {
                            Console.WriteLine("Missing argument for 'merge' command.");
                        }
                        break;

                    case "generate":
                        if (starupconsole.Length > 1)
                        {
                            Console.WriteLine($"DEBUG: Generating {starupconsole[1]}...");
                            HandleGenerate(starupconsole[1].ToLower());
                        }
                        else
                        {
                            Console.WriteLine("Missing argument for 'generate' command.");
                        }
                        break;

                    case "decompile":
                        Console.WriteLine("DEBUG: Decompiling NitroFiles...");
                        await NitroExtractor.Extract();
                        break;

                    case "compile":
                        Console.WriteLine("DEBUG: Compiling NitroFiles...");
                        await NitroFurniCompile.Compile();
                        break;

                    case "swftonitro":
                        Console.WriteLine("DEBUG: Converting SWF to Nitro...");
                        await SwfToNitroConverter.ConvertSwfFilesAsync();
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

        private static async Task HandleMerge(string mergeType)
        {
            switch (mergeType)
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
                    Console.WriteLine($"Unknown merge type: {mergeType}");
                    break;
            }
        }

        private static void HandleGenerate(string generateType)
        {
            switch (generateType)
            {
                case "sql":
                    SQLGenerator.GenerateSQL();
                    break;

                default:
                    Console.WriteLine($"Unknown generate type: {generateType}");
                    break;
            }
        }
    }
}
