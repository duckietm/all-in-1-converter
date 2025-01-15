using System;

namespace ConsoleApplication
{
    internal static class HelpCommand
    {
        internal static void DisplayHelp()
        {
            Console.Clear();
            Console.BackgroundColor = ConsoleColor.Red;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("                       Tool Commands:                             ");
            Console.WriteLine("                                                                  ");
            Console.BackgroundColor = ConsoleColor.Gray;
            Console.ForegroundColor = ConsoleColor.Blue;
            Console.WriteLine("-> Help     - This Command List                                   ");
            Console.WriteLine("-> Version  - Show current SWF version on Habbo.com               ");
            Console.WriteLine("-> About    - Show info about this tool                           ");
            Console.WriteLine("-> clothes  - Download all clothes and XML                        ");
            Console.WriteLine("-> customclothes  - Download all Custom clothes and XML           ");
            Console.WriteLine("-> custfurni - Download all Custom furni                         ");
            Console.WriteLine("-> Exit     - Exit the application                                ");
            Console.WriteLine("                                                                  ");
            Console.WriteLine("-> Download - all download commands:                              ");
            Console.ForegroundColor = ConsoleColor.Black;
            Console.WriteLine("- effects - Download the XML off all effects.                     ");
            Console.WriteLine("- furniture - Downloads All Habbo Furniture.                      ");
            Console.WriteLine("- furnidata - Saves a local copy of the furnidata.                ");
            Console.WriteLine("- productdata - Saves a local copy of the productdata             ");
            Console.WriteLine("- texts - Saves all external_flash_texts.                         ");
            Console.WriteLine("- variables - Saves all external_variables.                       ");
            Console.WriteLine("- icons - Saves all catalogue icons.                              ");
            Console.WriteLine("- mp3 - Saves all mp3 sounds.                                     ");
            Console.WriteLine("- quests - Saves all quest images.                                ");
            Console.WriteLine("- badges - Saves all badges.                                      ");
            Console.WriteLine("- reception - Saves client background images                      ");
            Console.WriteLine("                                                                  ");
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Gray;
        }
    }
}