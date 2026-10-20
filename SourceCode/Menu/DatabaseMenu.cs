using ConsoleApplication.FixSettings;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using Habbo_Downloader.Compiler;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class DatabaseMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Database Menu", new MenuItem[]
        {
            new("1", "Database General Information (version / databases)", OperationCatalog.Get("database.info").Action, HowToUse:
                "Connects to the MariaDB / MySQL server defined in config.ini under\n" +
                "[Database Settings] and prints: server version, list of databases\n" +
                "with size in MB, current user table count. Read-only.\n" +
                "Use this first to sanity-check that the credentials work before\n" +
                "running any of the destructive options below."),

            new("2", "Optimize Database (OPTIMIZE TABLE on all tables)", OperationCatalog.Get("database.optimize").Action, HowToUse:
                "Runs OPTIMIZE TABLE on every table of the configured database.\n" +
                "This rebuilds indexes and reclaims fragmented space. Safe but slow\n" +
                "on big tables (room_items, users_items, etc.). Best run during\n" +
                "maintenance windows because OPTIMIZE locks each table while it\n" +
                "rewrites it on disk."),

            new("3", "Fix Offer_ID in database and JSON", OperationCatalog.Get("database.offer-id").Action, HowToUse:
                "Walks Generate/Variables/FurnitureData.json and aligns each\n" +
                "items_base row's offer_id with the catalogue offer_id read from\n" +
                "the JSON. Useful when offer_ids have drifted between your DB and\n" +
                "the FurnitureData you actually serve to the client.\n" +
                "Writes back into both the DB and the JSON (copy the JSON to the\n" +
                "Nitro server afterwards)."),

            new("4", "Fix Sit / Lay / Walk in items_base from JSON", OperationCatalog.Get("database.item-settings").Action, HowToUse:
                "Reads canstandon / cansiton / canlayon from FurnitureData.json and\n" +
                "writes them back into items_base.allow_walk / allow_sit / allow_lay\n" +
                "for every matching classname. Run after adding new furniture so\n" +
                "the emulator stops dropping users mid-room."),

            new("5", "Fix Sprite_ID + Item_IDS in items_base from JSON", OperationCatalog.Get("database.sprite-id").Action, HowToUse:
                "Updates items_base.sprite_id and items_base.item_ids from the\n" +
                "FurnitureData.json so the catalogue is in sync with the DB.\n" +
                "RUN THIS BEFORE option 3 / 4 when you have just added new\n" +
                "furniture - otherwise the offer_id / sit-lay-walk fixes have\n" +
                "nothing to match against."),

            new("6", "Fix interaction count (SQL from Habbo_Default/hof_furni/hab)", OperationCatalog.Get("database.interaction-count").Action, HowToUse:
                "Reads every floor and wall furni (type s or i; pets 'a0 ...' left out)\n" +
                "with interaction_type 'default' and no multiheight, opens\n" +
                "Habbo_Default/hof_furni/hab/<item_name>.hab and\n" +
                "counts its interaction modes the same way Generate SQL does.\n" +
                "Rows whose interaction_modes_count differs are written to\n" +
                "Generate/Update_Interaction_count_<date>.sql; the .hab files that\n" +
                "are missing (or unreadable) go to Generate/Missing_Hab_files_<date>.txt.\n" +
                "The database is only read:\n" +
                "check the file, then run it yourself."),

            new("7", "Fix width / length / height (SQL from Habbo_Default/hof_furni/hab)", OperationCatalog.Get("database.item-size").Action, HowToUse:
                "Reads every floor and wall furni (type s or i; pets 'a0 ...' left out)\n" +
                "without multiheight. A floor furni gets width / length / stack_height\n" +
                "from logic.model.dimensions in Habbo_Default/hof_furni/hab/<item_name>.hab,\n" +
                "the same way Generate SQL does; a wall furni is always 1 x 1 x 0.00.\n" +
                "Rows that change are written to Generate/Update_Item_size_<date>.sql;\n" +
                "the .hab files that are missing (or unreadable) go to\n" +
                "Generate/Missing_Hab_files_<date>.txt. The database is only read:\n" +
                "check the file, then run it yourself."),
        });
    }
}
