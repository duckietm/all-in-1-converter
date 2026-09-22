using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HotelToolsWebpMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Tools (WebP Lossless)", new MenuItem[]
        {
            new("1", "SWF Furniture to Nitro (WebP)", OperationCatalog.Get("tools.webp-furniture").Action, HowToUse:
                "Convert Flash furniture SWFs into lightweight .nitro bundles with WebP Lossless textures.\n" +
                "100% bit-for-bit exact graphics + 100% alpha transparency, saving 25-40% file size.\n" +
                "Source: (H) Habbo_Default/hof_furni or (I) SWFCompiler/import/furniture.\n" +
                "Output: SWFCompiler/furniture/."),

            new("2", "SWF Clothes to Nitro (WebP)", OperationCatalog.Get("tools.webp-clothes").Action, HowToUse:
                "Convert clothing SWF files to .nitro bundles with WebP Lossless textures.\n" +
                "Source: (H) Habbo_Default/clothes or (I) SWFCompiler/import/clothes.\n" +
                "Output: SWFCompiler/clothes/."),

            new("3", "SWF Pets to Nitro (WebP)", OperationCatalog.Get("tools.webp-pets").Action, HowToUse:
                "Convert pet SWF files to .nitro bundles with WebP Lossless textures.\n" +
                "Reads SWFCompiler/import/pets/.\n" +
                "Output: SWFCompiler/pets/."),

            new("4", "SWF Effects to Nitro (WebP)", OperationCatalog.Get("tools.webp-effects").Action, HowToUse:
                "Convert effect SWF files to .nitro bundles with WebP Lossless textures.\n" +
                "Reads SWFCompiler/import/effects/.\n" +
                "Output: SWFCompiler/effects/."),

            new("5", "Convert Existing Nitro (PNG to WebP)", OperationCatalog.Get("tools.convert-nitro-webp").Action, HowToUse:
                "Batch converts existing .nitro bundles containing legacy PNG textures to WebP Lossless.\n" +
                "Preserves 100% alpha transparency and pixel fidelity while cutting size by 25-40%.\n" +
                "Reads from NitroCompiler/convert_webp/ (or custom folders).\n" +
                "Output: NitroCompiler/converted_webp/."),

            new("6", "Convert Generic Nitro (room, badges, cursor to WebP)", OperationCatalog.Get("tools.webp-generic").Action, HowToUse:
                "Converts generic client UI bundles (room.nitro, group_badge.nitro, cursors, placeholders)\n" +
                "to WebP Lossless, cutting size by ~68% while keeping 100% pixel fidelity.\n" +
                "Reads from NitroCompiler/generic/ or Desktop/generic/.\n" +
                "Output: NitroCompiler/converted_webp/generic/."),

            new("7", "WebP Lossless Benchmark & Pixel Test", OperationCatalog.Get("tools.benchmark-webp").Action, HowToUse:
                "Runs complete verification and benchmark suite testing PNG vs WebP size\n" +
                "and verifying 100% bit-for-bit RGBA pixel match across sample assets.")
        });
    }
}
