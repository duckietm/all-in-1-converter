using Habbo_Downloader.App.Menus;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HabboOriginalMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Habbo Original Downloads", new MenuItem[]
        {
            new("1",   "Download Badges",
                Badges.DownloadBadgesAsync,
                HowToUse:
                    "Pulls every badge .gif/.png from the official Habbo CDN.\n" +
                    "Output: Habbo_Default/badges/. Skips files already on disk."),

            new("2",   "Download Clothes (figuredata + figuremap)",
                ClothesDownloader.DownloadClothesAsync,
                HowToUse:
                    "Downloads FigureData.json (palettes + setTypes) and FigureMap.json\n" +
                    "(libraries) from Habbo. Saves to Habbo_Default/files/json/ for use by\n" +
                    "Merge Clothes (option 3 of Hotel Tools)."),

            new("3",   "Download Effects (effectmap + HabboAvatarActions)",
                EffectsDownloader.DownloadEffectsAsync,
                HowToUse:
                    "Fetches effect map metadata and the HabboAvatarActions.json file.\n" +
                    "Required for SWF Effects to Nitro conversion."),

            new("4",   "Download Furnidata -> FurnitureData.json",
                FurnidataDownloader.DownloadFurnidata,
                HowToUse:
                    "Downloads the XML furniture catalog from Habbo and converts it to\n" +
                    "FurnitureData.json in Habbo_Default/files/json/. Used by Merge Furnidata\n" +
                    "and Generate SQL."),

            new("5",   "Download Furniture (SWF)",
                FurnitureDownloader.DownloadFurnitureAsync,
                HowToUse:
                    "Pulls every .swf furniture asset from Habbo CDN into Habbo_Default/hof_furni/.\n" +
                    "These are the files SWF Furniture to Nitro (Hotel Tools option 7, H mode) reads."),

            new("6",   "Download Catalogue icons",
                IconDownloader.DownloadIcons,
                HowToUse:
                    "Downloads the small icon PNGs shown in the catalogue UI.\n" +
                    "Output: Habbo_Default/hof_furni/icons/ and Habbo_Default/icons/."),

            new("7",   "Download MP3",
                Mp3Downloader.DownloadMp3sAsync,
                HowToUse:
                    "Downloads sound samples (.mp3) used by traxmachine and various furni.\n" +
                    "Output: Habbo_Default/mp3/."),

            new("8",   "Download Productdata",
                ProductDataDownloader.DownloadProductDataAsync,
                HowToUse:
                    "Downloads ProductData.json (catalogue product names + descriptions).\n" +
                    "Used by Merge Productdata in Hotel Tools."),

            new("9",   "Download Quests images",
                QuestsDownloader.DownloadQuestsAsync,
                HowToUse:
                    "Downloads quest banner / icon images. Output: Habbo_Default/quests/."),

            new("10",  "Download Reception images",
                ReceptionDownloader.DownloadReceptionImages,
                HowToUse:
                    "Downloads the rotating reception / promo art shown on the lobby page.\n" +
                    "Output: Habbo_Default/reception/ and reception/web_promo_small/."),

            new("11",  "Download Texts",
                TextsDownloader.DownloadTextsAsync,
                HowToUse:
                    "Downloads external_flash_texts / external_texts (UI strings + chat lines).\n" +
                    "Now saved as JSON. Output: Habbo_Default/files/txt/ and .../json/."),

            new("12",  "Download Variables",
                VariablesDownloader.DownloadVariablesAsync,
                HowToUse:
                    "Downloads external_variables (URLs, feature flags, host config).\n" +
                    "Useful for inspecting which CDN endpoints Habbo currently advertises."),

            new("all", "Download All (clothes + furni + product + ...)",
                DownloadAllAsync,
                HowToUse:
                    "Runs the full bootstrap sequence in order: clothes, furnidata, productdata,\n" +
                    "furniture SWF, variables, texts, icons. Useful when initializing a fresh\n" +
                    "hotel - everything Merge / Generate SQL / SWF->Nitro will need afterwards."),
        });

        private static async Task DownloadAllAsync()
        {
            await ClothesDownloader.DownloadClothesAsync();
            await FurnidataDownloader.DownloadFurnidata();
            await ProductDataDownloader.DownloadProductDataAsync();
            await FurnitureDownloader.DownloadFurnitureAsync();
            await VariablesDownloader.DownloadVariablesAsync();
            await TextsDownloader.DownloadTextsAsync();
            await IconDownloader.DownloadIcons();
        }
    }
}
