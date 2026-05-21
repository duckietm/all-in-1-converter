using Habbo_Downloader.App.Menus;
using Habbo_Downloader.Compiler;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HotelToolsMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Tools Menu", new MenuItem[]
        {
            new("1",  "Merge Furnidata",          CompareFurnidata.Compare),
            new("2",  "Merge Productdata",        CompareProductData.Compare),
            new("3",  "Merge Clothes",            CompareClothesData.Compare),
            new("4",  "Generate SQL",             () => { SQLGenerator.GenerateSQL(); return Task.CompletedTask; }),
            new("5",  "Decompile NitroFiles",     NitroExtractor.Extract),
            new("6",  "Compile NitroFiles",       NitroFurniCompile.Compile),
            new("7",  "SWF Furniture to Nitro",   SWF_Furni_To_Nitro.ConvertSwfFilesAsync),
            new("8",  "SWF Clothes to Nitro",     SWF_clothes_To_Nitro.ConvertSwfFilesAsync),
            new("9",  "SWF Pets to Nitro",        SWF_Pets_To_Nitro.ConvertSwfFilesAsync),
            new("10", "SWF Effects to Nitro",     SWF_Effects_To_Nitro.ConvertSwfFilesAsync),
        });
    }
}
