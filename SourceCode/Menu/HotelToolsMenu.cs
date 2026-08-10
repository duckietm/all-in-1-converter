using Habbo_Downloader.App.Menus;
using Habbo_Downloader.App.Operations;
using Habbo_Downloader.Compiler;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HotelToolsMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Tools Menu", new MenuItem[]
        {
            new("1",  "Merge Furnidata", OperationCatalog.Get("tools.merge-furnidata").Action, HowToUse:
                "Combines Original_Furnidata + Import_Furnidata into Merged_Furnidata.\n" +
                "Skips duplicates by classname OR by id (additive only, no override).\n" +
                "Reads and writes one strict FurnitureData.json file."),

            new("2",  "Merge Productdata", OperationCatalog.Get("tools.merge-productdata").Action, HowToUse:
                "Combines Original_ProductData + Import_ProductData into Merged_ProductData.\n" +
                "For each conflict on `code` you can answer (Y) replace, (A) yes-to-all,\n" +
                "(N) skip, (Z) no-to-all. Reads and writes strict JSON files."),

            new("3",  "Merge Clothes", OperationCatalog.Get("tools.merge-clothes").Action, HowToUse:
                "Merges FigureData (palettes + setTypes) AND FigureMap (libraries)\n" +
                "from Original_ClothesData + Import_ClothesData.\n" +
                "Writes FigureData.json and FigureMap.json into Merged_ClothesData/."),

            new("4",  "Generate SQL", OperationCatalog.Get("tools.generate-sql").Action, HowToUse:
                "Reads FurnitureData.json from Generate/Furnidata/\n" +
                "and every .nitro / .swf inside Generate/Furniture/ (recursive).\n" +
                "Asks: starting ID for items_base + catalog_items, plus Catalog_Page ID.\n" +
                "Produces SQL files in Generate/Output_SQL/ with timestamp, one INSERT per item.\n" +
                "Width / length / height / interactions are read from each .nitro automatically."),

            new("5",  "Decompile NitroFiles", OperationCatalog.Get("tools.decompile-nitro").Action, HowToUse:
                "Drop .nitro bundles into NitroCompiler/extract/{furni,clothing,effects,pets}/\n" +
                "Output: JSON manifest + spritesheet PNG in NitroCompiler/extracted/<tier>/<name>/"),

            new("6",  "Compile NitroFiles", OperationCatalog.Get("tools.compile-nitro").Action, HowToUse:
                "Inverse of (5). Pack a folder containing <name>.json + <name>.png into a\n" +
                ".nitro bundle inside NitroCompiler/compiled/. Reads from NitroCompiler/compile/."),

            new("7",  "SWF Furniture to Nitro", OperationCatalog.Get("tools.swf-furniture").Action, HowToUse:
                "Convert legacy Flash .swf furniture to modern .nitro format.\n" +
                "Source prompt: (H) Habbo_Default/hof_furni or (I) SWFCompiler/import/furniture.\n" +
                "Uses FFDec (Tools/ffdec/) to extract assets, then ImageSharp to build the\n" +
                "spritesheet (cross-platform: Windows + Linux). Output: SWFCompiler/furniture/."),

            new("8",  "SWF Clothes to Nitro", OperationCatalog.Get("tools.swf-clothes").Action, HowToUse:
                "Convert clothing .swf files to .nitro. Source (H) Habbo_Default/clothes or\n" +
                "(I) SWFCompiler/import/clothes. Skips hh_human_fx.swf (effects file).\n" +
                "Output: SWFCompiler/clothes/."),

            new("9",  "SWF Pets to Nitro", OperationCatalog.Get("tools.swf-pets").Action, HowToUse:
                "Convert pet .swf files to .nitro. Reads SWFCompiler/import/pets/.\n" +
                "Includes palette extraction (PaletteExtractor) and visualization XML parsing.\n" +
                "Output: SWFCompiler/pets/. Skips files already converted."),

            new("10", "SWF Effects to Nitro", OperationCatalog.Get("tools.swf-effects").Action, HowToUse:
                "Convert effect .swf files to .nitro. Reads SWFCompiler/import/effects/.\n" +
                "Custom XML can be dropped in SWFCompiler/import/effects/CustomXML/.\n" +
                "Output: SWFCompiler/effects/."),
        });
    }
}
