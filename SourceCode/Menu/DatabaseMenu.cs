using ConsoleApplication.FixSettings;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.Compiler;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class DatabaseMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Database Menu", new MenuItem[]
        {
            new("1", "Database General Information (version / databases)",  DatabaseGeneralInfo.ShowDatabaseVersionAsync),
            new("2", "Optimize Database (OPTIMIZE TABLE on all tables)",    DatabaseOptimizer.OptimizeDatabaseTablesAsync),
            new("3", "Fix Offer_ID in database and JSON",                   SetOfferID.RunAsync),
            new("4", "Fix Sit / Lay / Walk in items_base from JSON",        FixItemSettings.RunAsync),
            new("5", "Fix Sprite_ID + Item_IDS in items_base from JSON",    FixSpriteIDSettings.RunAsync),
        });
    }
}
