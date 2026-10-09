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
                "and every .nitro / .hab / .swf inside Generate/Furniture/ (recursive).\n" +
                "Asks: starting ID for items_base + catalog_items, plus Catalog_Page ID.\n" +
                "Produces SQL files in Generate/Output_SQL/ with timestamp, one INSERT per item.\n" +
                "Width / length / height / interactions are read from each .nitro / .hab automatically."),

            new("5",  "Decompile NitroFiles", OperationCatalog.Get("tools.decompile-nitro").Action, HowToUse:
                "Drop .nitro bundles into NitroCompiler/extract/{furni,clothing,effects,pets,generic}/nitro/\n" +
                "Output: the json + sheet exactly as stored, in NitroCompiler/extracted/<type>/<name>/"),

            new("6",  "Compile NitroFiles", OperationCatalog.Get("tools.compile-nitro").Action, HowToUse:
                "Inverse of (5). Packs each NitroCompiler/compile/<type>/<name>/ folder (json + sheet)\n" +
                "into NitroCompiler/compiled/<type>/nitro/<name>.nitro. A PNG sheet becomes lossless WebP."),

            new("7",  "Decompile HabFiles", OperationCatalog.Get("tools.decompile-hab").Action, HowToUse:
                "Drop .hab bundles into NitroCompiler/extract/{furni,clothing,effects,pets,generic}/hab/\n" +
                "Output: the json + sheet exactly as stored, in NitroCompiler/extracted/<type>/<name>/"),

            new("8",  "Compile HabFiles", OperationCatalog.Get("tools.compile-hab").Action, HowToUse:
                "Inverse of (7). Packs each NitroCompiler/compile/<type>/<name>/ folder (json + sheet)\n" +
                "into NitroCompiler/compiled/<type>/hab/<name>.hab. A PNG sheet becomes lossless WebP."),

            new("9", "SWF Furniture to Nitro", OperationCatalog.Get("tools.swf-furniture-nitro").Action, HowToUse:
                "Convert furniture .swf files to .nitro (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/hof_furni/swf or (I) SWFCompiler/import/furniture.\n" +
                "Output: SWFCompiler/furniture/nitro/. Skips files already converted."),

            new("10", "SWF Furniture to HAB", OperationCatalog.Get("tools.swf-furniture-hab").Action, HowToUse:
                "Convert furniture .swf files to .hab (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/hof_furni/swf or (I) SWFCompiler/import/furniture.\n" +
                "Output: SWFCompiler/furniture/hab/. Skips files already converted."),

            new("11", "Nitro Furniture to HAB", OperationCatalog.Get("tools.nitro-furniture-hab").Action, HowToUse:
                "Repack furniture .nitro bundles as .hab with the same json; a PNG sheet becomes\n" +
                "lossless WebP (every pixel checked). The .nitro files stay as they are.\n" +
                "Reads SWFCompiler/furniture/nitro/ (or a custom folder). Output: SWFCompiler/furniture/hab/."),

            new("12", "SWF Clothes to Nitro", OperationCatalog.Get("tools.swf-clothes-nitro").Action, HowToUse:
                "Convert clothes .swf files to .nitro (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/clothes/swf or (I) SWFCompiler/import/clothes.\n" +
                "Skips hh_human_fx.swf (effects file).\n" +
                "Output: SWFCompiler/clothes/nitro/. Skips files already converted."),

            new("13", "SWF Clothes to HAB", OperationCatalog.Get("tools.swf-clothes-hab").Action, HowToUse:
                "Convert clothes .swf files to .hab (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/clothes/swf or (I) SWFCompiler/import/clothes.\n" +
                "Skips hh_human_fx.swf (effects file).\n" +
                "Output: SWFCompiler/clothes/hab/. Skips files already converted."),

            new("14", "Nitro Clothes to HAB", OperationCatalog.Get("tools.nitro-clothes-hab").Action, HowToUse:
                "Repack clothes .nitro bundles as .hab with the same json; a PNG sheet becomes\n" +
                "lossless WebP (every pixel checked). The .nitro files stay as they are.\n" +
                "Reads SWFCompiler/clothes/nitro/ (or a custom folder). Output: SWFCompiler/clothes/hab/."),

            new("15", "SWF Pets to Nitro", OperationCatalog.Get("tools.swf-pets-nitro").Action, HowToUse:
                "Convert pets .swf files to .nitro (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/pets/swf or (I) SWFCompiler/import/pets.\n" +
                "Output: SWFCompiler/pets/nitro/. Skips files already converted."),

            new("16", "SWF Pets to HAB", OperationCatalog.Get("tools.swf-pets-hab").Action, HowToUse:
                "Convert pets .swf files to .hab (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/pets/swf or (I) SWFCompiler/import/pets.\n" +
                "Output: SWFCompiler/pets/hab/. Skips files already converted."),

            new("17", "Nitro Pets to HAB", OperationCatalog.Get("tools.nitro-pets-hab").Action, HowToUse:
                "Repack pets .nitro bundles as .hab with the same json; a PNG sheet becomes\n" +
                "lossless WebP (every pixel checked). The .nitro files stay as they are.\n" +
                "Reads SWFCompiler/pets/nitro/ (or a custom folder). Output: SWFCompiler/pets/hab/."),

            new("18", "SWF Effects to Nitro", OperationCatalog.Get("tools.swf-effects-nitro").Action, HowToUse:
                "Convert effects .swf files to .nitro (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/effects/swf or (I) SWFCompiler/import/effects.\n" +
                "Custom XML can be dropped in SWFCompiler/import/effects/CustomXML/.\n" +
                "Output: SWFCompiler/effects/nitro/. Skips files already converted."),

            new("19", "SWF Effects to HAB", OperationCatalog.Get("tools.swf-effects-hab").Action, HowToUse:
                "Convert effects .swf files to .hab (json + WebP Lossless sheet).\n" +
                "Source prompt: (H) Habbo_Default/effects/swf or (I) SWFCompiler/import/effects.\n" +
                "Custom XML can be dropped in SWFCompiler/import/effects/CustomXML/.\n" +
                "Output: SWFCompiler/effects/hab/. Skips files already converted."),

            new("20", "Nitro Effects to HAB", OperationCatalog.Get("tools.nitro-effects-hab").Action, HowToUse:
                "Repack effects .nitro bundles as .hab with the same json; a PNG sheet becomes\n" +
                "lossless WebP (every pixel checked). The .nitro files stay as they are.\n" +
                "Reads SWFCompiler/effects/nitro/ (or a custom folder). Output: SWFCompiler/effects/hab/."),

            new("21", "Decompile SWF Files", OperationCatalog.Get("tools.decompile-swf").Action, HowToUse:
                "Decompile SWF assets permanently to raw images, XMLs (binaryData),\n" +
                "symbols (symbolClass), ActionScript (.as scripts), and audio.\n" +
                "Reads from SWFCompiler/decompile/ (or import folders).\n" +
                "Output: SWFCompiler/decompiled/<name>/."),

            new("22", "Add size 32 to HAB Furniture", OperationCatalog.Get("tools.hab-furniture-32").Action, HowToUse:
                "Habbo's .hab furniture has no size 32 (the zoomed-out room). This copies the size 32\n" +
                "sprites, assets and visualization from a .nitro (or .hab) of the same furni into it;\n" +
                "the 64 graphics stay as they are. Only when both have the same 64 graphics.\n" +
                "Reads Habbo_Default/hof_furni/hab/ and SWFCompiler/furniture/nitro/ (or custom folders).\n" +
                "Output: SWFCompiler/furniture/hab/. Skips files already there.")
        });
    }
}
