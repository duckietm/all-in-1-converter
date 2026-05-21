using ConsoleApplication;
using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Runners;

namespace Habbo_Downloader.App
{
    /// <summary>
    /// Centralised main-menu definition reused by CliRunner / TuiRunner / Program.RunGui,
    /// so every shell sees exactly the same entries (including the HowToUse pages).
    /// </summary>
    public static class MainMenuFactory
    {
        public static MenuItem[] Build() => new MenuItem[]
        {
            new("1", "Habbo Original Downloads", HabboOriginalMenu.DisplayMenu, IsSubMenu: true, HowToUse:
                "Open the Habbo Original Downloads sub-menu.\n" +
                "Pulls official Habbo CDN assets (badges, clothes, effects, furnidata,\n" +
                "furniture SWF, icons, MP3, productdata, quests, reception, texts,\n" +
                "variables). Use this to bootstrap a fresh hotel with the latest\n" +
                "official assets before customising. Target hotel (.nl / .com / etc.)\n" +
                "is controlled by the URLs in config.ini."),

            new("2", "Nitro Custom Downloads", NitroCustomMenu.DisplayMenu, IsSubMenu: true, HowToUse:
                "Open the Nitro Custom Downloads sub-menu.\n" +
                "Pulls custom furniture / clothes packs from another retro by reading\n" +
                "its renderer-config.json endpoints (set them in config.ini under the\n" +
                "[Nitro Retro Hotels] block). Supports BOTH legacy flat single-file\n" +
                "FurnitureData.json / FigureData.json / FigureMap.json AND the new\n" +
                "JSON5 split layout (URL ending with \"/\"). The latter pulls every\n" +
                "tier (core / custom / seasonal) and merges them locally."),

            new("3", "Hotel Tools", HotelToolsMenu.DisplayMenu, IsSubMenu: true, HowToUse:
                "Open the Hotel Tools sub-menu.\n" +
                "Hosts every transformation step of the asset pipeline: merge\n" +
                "furnidata / productdata / clothesdata (dual flat or JSON5 split,\n" +
                "doubles as a flat<->split converter), generate items_base +\n" +
                "catalog_items SQL from .nitro / .swf, decompile / compile .nitro\n" +
                "bundles, and convert SWF -> Nitro for furniture / clothes / pets\n" +
                "/ effects. Cross-platform thanks to ImageSharp + FFDec via Java."),

            new("4", "Database Tools", DatabaseMenu.DisplayMenu, IsSubMenu: true, HowToUse:
                "Open the Database Tools sub-menu.\n" +
                "Hits the configured MariaDB / MySQL (credentials in config.ini\n" +
                "[Database Settings]) to inspect or repair the hotel DB: show\n" +
                "version + size, OPTIMIZE every table, fix offer_id / sprite_id\n" +
                "/ item_id / sit-lay-walk in items_base from FurnitureData.json.\n" +
                "Use option 5 (Fix Sprite_ID) FIRST when adding new furniture."),

            new("version", "Fetch current Habbo client version", CliRunner.DisplayVersionAsync, HowToUse:
                "Hits https://www.habbo.com/gamedata/external_variables/1 and prints\n" +
                "the current Flash client build directory name (e.g. \"PRODUCTION-...\").\n" +
                "Useful to check whether the official Habbo assets have rotated since\n" +
                "your last download pass."),

            new("credits", "Credits & contributors", Credits.ShowAsync, HowToUse:
                "Prints the authors and contributors of the tool: medievalshell\n" +
                "(.NET 10 modernization, CLI/TUI/GUI shells, JSON5 split-mode),\n" +
                "duckietm (original upstream tool), plus Nitro Team Discord,\n" +
                "AtlasOmega and Leet who contributed assets / effects."),

            UiSwitcher.ForCurrentMode(),
        };
    }
}
