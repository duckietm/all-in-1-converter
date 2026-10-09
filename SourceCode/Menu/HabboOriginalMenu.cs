using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HabboOriginalMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Habbo Original Downloads", new MenuItem[]
        {
            new("1",   "Download Badges",
                OperationCatalog.Get("habbo.badges").Action,
                HowToUse:
                    "Pulls every badge .gif/.png from the official Habbo CDN.\n" +
                    "Output: Habbo_Default/badges/. Skips files already on disk."),

            new("2",   $"Download Clothes (figuredata + figuremap + {Format})",
                OperationCatalog.Get("habbo.clothes").Action,
                HowToUse:
                    "Downloads FigureData.json (palettes + setTypes) and FigureMap.json\n" +
                    "(libraries) from Habbo. Saves to Habbo_Default/files/json/ for use by\n" +
                    "Merge Clothes (option 3 of Hotel Tools).\n" +
                    "The clothes libraries go to Habbo_Default/clothes/swf and /hab (config.ini download_format)."),

            new("3",   $"Download Effects (effectmap + HabboAvatarActions + {Format})",
                OperationCatalog.Get("habbo.effects").Action,
                HowToUse:
                    "Fetches effect map metadata and the HabboAvatarActions.json file.\n" +
                    "Required for SWF Effects to Nitro conversion.\n" +
                    "The effect libraries go to Habbo_Default/effects/swf and /hab (config.ini download_format)."),

            new("4",   "Download Furnidata -> FurnitureData.json",
                OperationCatalog.Get("habbo.furnidata").Action,
                HowToUse:
                    "Downloads the XML furniture catalog from Habbo and converts it to\n" +
                    "FurnitureData.json in Habbo_Default/files/json/. Used by Merge Furnidata\n" +
                    "and Generate SQL."),

            new("5",   $"Download Furniture ({Format})",
                OperationCatalog.Get("habbo.furniture").Action,
                HowToUse:
                    "Pulls Habbo's furniture into Habbo_Default/hof_furni/: .swf into swf/, .hab into hab/,\n" +
                    "icons into icons/. config.ini download_format picks swf, hab or both.\n" +
                    "A .hab gets a lossless WebP sheet (spritesheet_format=webp); its json is not changed.\n" +
                    "The swf/ files are what SWF Furniture to Nitro (Hotel Tools option 9/10, H mode) reads."),

            new("6",   "Download Catalogue icons",
                OperationCatalog.Get("habbo.icons").Action,
                HowToUse:
                    "Downloads the small icon PNGs shown in the catalogue UI.\n" +
                    "Output: Habbo_Default/hof_furni/icons/ and Habbo_Default/icons/."),

            new("7",   "Download MP3",
                OperationCatalog.Get("habbo.mp3").Action,
                HowToUse:
                    "Downloads sound samples (.mp3) used by traxmachine and various furni.\n" +
                    "Output: Habbo_Default/mp3/."),

            new("8",   "Download Productdata",
                OperationCatalog.Get("habbo.productdata").Action,
                HowToUse:
                    "Downloads ProductData.json (catalogue product names + descriptions).\n" +
                    "Used by Merge Productdata in Hotel Tools."),

            new("9",   "Download Quests images",
                OperationCatalog.Get("habbo.quests").Action,
                HowToUse:
                    "Downloads quest banner / icon images. Output: Habbo_Default/quests/."),

            new("10",  "Download Reception images",
                OperationCatalog.Get("habbo.reception").Action,
                HowToUse:
                    "Downloads the rotating reception / promo art shown on the lobby page.\n" +
                    "Output: Habbo_Default/reception/ and reception/web_promo_small/."),

            new("11",  "Download Texts",
                OperationCatalog.Get("habbo.texts").Action,
                HowToUse:
                    "Downloads external_flash_texts / external_texts (UI strings + chat lines).\n" +
                    "Now saved as JSON. Output: Habbo_Default/files/txt/ and .../json/."),

            new("12",  "Download Variables",
                OperationCatalog.Get("habbo.variables").Action,
                HowToUse:
                    "Downloads external_variables (URLs, feature flags, host config).\n" +
                    "Useful for inspecting which CDN endpoints Habbo currently advertises."),

            new("13",  $"Download Pets ({Format})",
                OperationCatalog.Get("habbo.pets").Action,
                HowToUse:
                    "Downloads Habbo's pet libraries into Habbo_Default/pets/swf and /hab\n" +
                    "(config.ini download_format). The list can be changed with config.ini pet_libraries.\n" +
                    "A .hab gets a lossless WebP sheet (spritesheet_format=webp); its json is not changed."),

            new("14",  "Extract Effect icons (fx_icon_<type> for the avatar editor)",
                OperationCatalog.Get("habbo.effect-icons").Action,
                HowToUse:
                    "Habbo has no CDN URL for the effect icons: they ship inside the Habbo Classic app at\n" +
                    "HabboClassicWin.zip > resources/app.asar > client/generated/habbo-inventory-com.hab.\n" +
                    "Put the zip, the app.asar, that .hab or a folder of fx_icon_<type> PNGs in Habbo_Default/import/effect_icons\n" +
                    "(or set config.ini effect_icons_source), or set config.ini effect_icons_url to a download link\n" +
                    "of one of them (for example a mirror). Output: Habbo_Default/fx_icons/fx_icon_<type>.png,\n" +
                    "for the client's avatareditor.effects.icon.url (${image.library.url}fx_icons/fx_icon_%type%.png)."),

            new("all", "Download All (clothes + furni + product + ...)",
                OperationCatalog.Get("habbo.all").Action,
                HowToUse:
                    "Runs the full bootstrap sequence in order: clothes, furnidata, productdata,\n" +
                    "furniture SWF, variables, texts, icons. Useful when initializing a fresh\n" +
                    "hotel - everything Merge / Generate SQL / SWF->Nitro will need afterwards."),
        });

        private static string Format => Habbo_Downloader.Tools.ConverterSettings.DownloadFormat.ToUpperInvariant();
    }
}
