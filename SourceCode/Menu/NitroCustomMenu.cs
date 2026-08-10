using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class NitroCustomMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Nitro Custom Downloads", new MenuItem[]
        {
            new("1", "Download NitroFurniture", OperationCatalog.Get("nitro.furniture").Action, HowToUse:
                "Pulls every .nitro furniture file from a Nitro V3 retro into\n" +
                "custom_downloads/nitro_furniture/.\n" +
                "Reads three config.ini keys: nitro_furnidataJSON, nitro_furnitureurl,\n" +
                "nitro_furniture_icon_url. nitro_furnidataJSON must point directly\n" +
                "to one strict FurnitureData.json file.\n" +
                "\n" +
                "Replace ##DOMAIN## in config.ini with the retro's hostname before\n" +
                "running."),

            new("2", "Download NitroClothes", OperationCatalog.Get("nitro.clothes").Action, HowToUse:
                "Pulls FigureData.json + FigureMap.json from a Nitro V3 retro and then\n" +
                "every .nitro clothing library listed in FigureMap, into\n" +
                "custom_downloads/clothes/.\n" +
                "Reads three config.ini keys: nitro_clothes_dir, nitro_figuredata,\n" +
                "nitro_figuremap. FigureData and FigureMap must each be one JSON file.\n" +
                "Skips hh_human_fx and hh_pets libraries (those\n" +
                "belong to Effects / Pets respectively)."),
        });
    }
}
