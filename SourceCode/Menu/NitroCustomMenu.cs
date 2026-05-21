using Habbo_Downloader.App.Menus;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class NitroCustomMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Nitro Custom Downloads", new MenuItem[]
        {
            new("1", "Download NitroFurniture", NitroFurnitureDownloader.DownloadFurnitureAsync),
            new("2", "Download NitroClothes",   NitroClothesDownloader.DownloadCustomClothesAsync),
        });
    }
}
