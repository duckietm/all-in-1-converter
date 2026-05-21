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
            new("1", "Database General Information (version / databases)", DatabaseGeneralInfo.ShowDatabaseVersionAsync, HowToUse:
                "Connects to the MariaDB / MySQL server defined in config.ini under\n" +
                "[Database Settings] and prints: server version, list of databases\n" +
                "with size in MB, current user table count. Read-only.\n" +
                "Use this first to sanity-check that the credentials work before\n" +
                "running any of the destructive options below."),

            new("2", "Optimize Database (OPTIMIZE TABLE on all tables)", DatabaseOptimizer.OptimizeDatabaseTablesAsync, HowToUse:
                "Runs OPTIMIZE TABLE on every table of the configured database.\n" +
                "This rebuilds indexes and reclaims fragmented space. Safe but slow\n" +
                "on big tables (room_items, users_items, etc.). Best run during\n" +
                "maintenance windows because OPTIMIZE locks each table while it\n" +
                "rewrites it on disk."),

            new("3", "Fix Offer_ID in database and JSON", SetOfferID.RunAsync, HowToUse:
                "Walks Generate/Variables/FurnitureData.json and aligns each\n" +
                "items_base row's offer_id with the catalogue offer_id read from\n" +
                "the JSON. Useful when offer_ids have drifted between your DB and\n" +
                "the FurnitureData you actually serve to the client.\n" +
                "Writes back into both the DB and the JSON (copy the JSON to the\n" +
                "Nitro server afterwards)."),

            new("4", "Fix Sit / Lay / Walk in items_base from JSON", FixItemSettings.RunAsync, HowToUse:
                "Reads canstandon / cansiton / canlayon from FurnitureData.json and\n" +
                "writes them back into items_base.allow_walk / allow_sit / allow_lay\n" +
                "for every matching classname. Run after adding new furniture so\n" +
                "the emulator stops dropping users mid-room."),

            new("5", "Fix Sprite_ID + Item_IDS in items_base from JSON", FixSpriteIDSettings.RunAsync, HowToUse:
                "Updates items_base.sprite_id and items_base.item_ids from the\n" +
                "FurnitureData.json so the catalogue is in sync with the DB.\n" +
                "RUN THIS BEFORE option 3 / 4 when you have just added new\n" +
                "furniture - otherwise the offer_id / sit-lay-walk fixes have\n" +
                "nothing to match against."),
        });
    }
}
