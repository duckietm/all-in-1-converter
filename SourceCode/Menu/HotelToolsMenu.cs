using Habbo_Downloader.App.Menus;
using Habbo_Downloader.Compiler;
using System.Threading.Tasks;

namespace ConsoleApplication
{
    public static class HotelToolsMenu
    {
        public static Task DisplayMenu() => MenuHost.ShowAsync("Hotel Tools Menu", new MenuItem[]
        {
            new("1",  "Merge Furnidata", CompareFurnidata.Compare, HowToUse:
                "Combines Original_Furnidata + Import_Furnidata into Merged_Furnidata.\n" +
                "Skips duplicates by classname OR by id (additive only, no override).\n" +
                "Input auto-detects flat FurnitureData.json or split-mode (directory\n" +
                "with manifest.json5 + core/custom/seasonal tiers).\n" +
                "Output prompt: (F)lat single file or (S)plit manifest.json5 + chunks of 300."),

            new("2",  "Merge Productdata", CompareProductData.Compare, HowToUse:
                "Combines Original_ProductData + Import_ProductData into Merged_ProductData.\n" +
                "For each conflict on `code` you can answer (Y) replace, (A) yes-to-all,\n" +
                "(N) skip, (Z) no-to-all. Input flat .json or split-mode dir. Output (F/S)."),

            new("3",  "Merge Clothes", CompareClothesData.Compare, HowToUse:
                "Merges FigureData (palettes + setTypes) AND FigureMap (libraries)\n" +
                "from Original_ClothesData + Import_ClothesData.\n" +
                "Both datasets are written into Merged_ClothesData/. Each can be flat or split."),

            new("4",  "Generate SQL", () => { SQLGenerator.GenerateSQL(); return Task.CompletedTask; }, HowToUse:
                "Reads FurnitureData.json (or split manifest) from Generate/Furnidata/\n" +
                "and every .nitro / .swf inside Generate/Furniture/ (recursive).\n" +
                "Asks: starting ID for items_base + catalog_items, plus Catalog_Page ID.\n" +
                "Produces SQL files in Generate/Output_SQL/ with timestamp, one INSERT per item.\n" +
                "Width / length / height / interactions are read from each .nitro automatically."),

            new("5",  "Decompile NitroFiles", NitroExtractor.Extract, HowToUse:
                "Drop .nitro bundles into NitroCompiler/extract/{furni,clothing,effects,pets}/\n" +
                "Output: JSON manifest + spritesheet PNG in NitroCompiler/extracted/<tier>/<name>/"),

            new("6",  "Compile NitroFiles", NitroFurniCompile.Compile, HowToUse:
                "Inverse of (5). Pack a folder containing <name>.json + <name>.png into a\n" +
                ".nitro bundle inside NitroCompiler/compiled/. Reads from NitroCompiler/compile/."),

            new("7",  "SWF Furniture to Nitro", SWF_Furni_To_Nitro.ConvertSwfFilesAsync, HowToUse:
                "Convert legacy Flash .swf furniture to modern .nitro format.\n" +
                "Source prompt: (H) Habbo_Default/hof_furni or (I) SWFCompiler/import/furniture.\n" +
                "Uses FFDec (Tools/ffdec/) to extract assets, then ImageSharp to build the\n" +
                "spritesheet (cross-platform: Windows + Linux). Output: SWFCompiler/furniture/."),

            new("8",  "SWF Clothes to Nitro", SWF_clothes_To_Nitro.ConvertSwfFilesAsync, HowToUse:
                "Convert clothing .swf files to .nitro. Source (H) Habbo_Default/clothes or\n" +
                "(I) SWFCompiler/import/clothes. Skips hh_human_fx.swf (effects file).\n" +
                "Output: SWFCompiler/clothes/."),

            new("9",  "SWF Pets to Nitro", SWF_Pets_To_Nitro.ConvertSwfFilesAsync, HowToUse:
                "Convert pet .swf files to .nitro. Reads SWFCompiler/import/pets/.\n" +
                "Includes palette extraction (PaletteExtractor) and visualization XML parsing.\n" +
                "Output: SWFCompiler/pets/. Skips files already converted."),

            new("10", "SWF Effects to Nitro", SWF_Effects_To_Nitro.ConvertSwfFilesAsync, HowToUse:
                "Convert effect .swf files to .nitro. Reads SWFCompiler/import/effects/.\n" +
                "Custom XML can be dropped in SWFCompiler/import/effects/CustomXML/.\n" +
                "Output: SWFCompiler/effects/."),
        });
    }
}
