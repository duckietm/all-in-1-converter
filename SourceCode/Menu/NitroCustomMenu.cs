using Habbo_Downloader.App.Menus;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class NitroCustomMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Nitro Custom Downloads", new MenuItem[]
        {
            new("1", "Download NitroFurniture", NitroFurnitureDownloader.DownloadFurnitureAsync, HowToUse:
                "Pulls every .nitro furniture file from a Nitro V3 retro into\n" +
                "custom_downloads/nitro_furniture/.\n" +
                "Reads three config.ini keys: nitro_furnidataJSON, nitro_furnitureurl,\n" +
                "nitro_furniture_icon_url.\n" +
                "\n" +
                "Two formats are auto-detected from the URL:\n" +
                "  * nitro_furnidataJSON ending with .json  -> legacy flat layout.\n" +
                "  * nitro_furnidataJSON ending with \"/\"    -> JSON5 split layout\n" +
                "    (manifest.json5 + core/custom/seasonal/ tiers). The downloader\n" +
                "    mirrors every tier locally and merges them in load order, then\n" +
                "    proceeds exactly like the flat case.\n" +
                "\n" +
                "Replace ##DOMAIN## in config.ini with the retro's hostname before\n" +
                "running."),

            new("2", "Download NitroClothes", NitroClothesDownloader.DownloadCustomClothesAsync, HowToUse:
                "Pulls FigureData.json + FigureMap.json from a Nitro V3 retro and then\n" +
                "every .nitro clothing library listed in FigureMap, into\n" +
                "custom_downloads/clothes/.\n" +
                "Reads three config.ini keys: nitro_clothes_dir, nitro_figuredata,\n" +
                "nitro_figuremap.\n" +
                "\n" +
                "Same dual-format support as option 1: trailing \"/\" on the URL means\n" +
                "JSON5 split layout. Skips hh_human_fx and hh_pets libraries (those\n" +
                "belong to Effects / Pets respectively)."),
        });
    }
}
