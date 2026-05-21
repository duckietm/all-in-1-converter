using Habbo_Downloader.App.Menus;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HabboOriginalMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Habbo Original Downloads", new MenuItem[]
        {
            new("1",   "Download Badges",                                Badges.DownloadBadgesAsync),
            new("2",   "Download Clothes (figuredata + figuremap)",      ClothesDownloader.DownloadClothesAsync),
            new("3",   "Download Effects (effectmap + HabboAvatarActions)", EffectsDownloader.DownloadEffectsAsync),
            new("4",   "Download Furnidata -> FurnitureData.json",       FurnidataDownloader.DownloadFurnidata),
            new("5",   "Download Furniture (SWF)",                       FurnitureDownloader.DownloadFurnitureAsync),
            new("6",   "Download Catalogue icons",                       IconDownloader.DownloadIcons),
            new("7",   "Download MP3",                                   Mp3Downloader.DownloadMp3sAsync),
            new("8",   "Download Productdata",                           ProductDataDownloader.DownloadProductDataAsync),
            new("9",   "Download Quests images",                         QuestsDownloader.DownloadQuestsAsync),
            new("10",  "Download Reception images",                      ReceptionDownloader.DownloadReceptionImages),
            new("11",  "Download Texts",                                 TextsDownloader.DownloadTextsAsync),
            new("12",  "Download Variables",                             VariablesDownloader.DownloadVariablesAsync),
            new("all", "Download All (clothes + furni + product + ...)", DownloadAllAsync),
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
