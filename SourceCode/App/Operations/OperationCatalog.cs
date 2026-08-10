using ConsoleApplication;
using ConsoleApplication.FixSettings;
using Habbo_Downloader.App.Runners;
using Habbo_Downloader.Compiler;

namespace Habbo_Downloader.App.Operations;

public static class OperationCatalog
{
    public static IReadOnlyList<OperationDefinition> All { get; } = Build();

    public static IReadOnlyList<OperationDefinition> ForCategory(OperationCategory category) =>
        All.Where(operation => operation.Category == category).ToArray();

    public static OperationDefinition Get(string id) =>
        All.Single(operation => string.Equals(operation.Id, id, StringComparison.Ordinal));

    private static IReadOnlyList<OperationDefinition> Build() =>
    [
        Op("habbo.badges", OperationCategory.HabboOriginal, "Download badges", "Download official Habbo badge images.", Badges.DownloadBadgesAsync),
        Op("habbo.clothes", OperationCategory.HabboOriginal, "Download clothes", "Download official FigureData.json and FigureMap.json.", ClothesDownloader.DownloadClothesAsync, true),
        Op("habbo.effects", OperationCategory.HabboOriginal, "Download effects", "Download effect map metadata and avatar actions.", EffectsDownloader.DownloadEffectsAsync, true),
        Op("habbo.furnidata", OperationCategory.HabboOriginal, "Download furnidata", "Download and convert official furnidata to FurnitureData.json.", FurnidataDownloader.DownloadFurnidata),
        Op("habbo.furniture", OperationCategory.HabboOriginal, "Download furniture", "Download official furniture SWF assets.", FurnitureDownloader.DownloadFurnitureAsync),
        Op("habbo.icons", OperationCategory.HabboOriginal, "Download catalogue icons", "Download official catalogue icon images.", IconDownloader.DownloadIcons),
        Op("habbo.mp3", OperationCategory.HabboOriginal, "Download MP3", "Download official sound assets.", Mp3Downloader.DownloadMp3sAsync),
        Op("habbo.productdata", OperationCategory.HabboOriginal, "Download productdata", "Download official ProductData.json.", ProductDataDownloader.DownloadProductDataAsync),
        Op("habbo.quests", OperationCategory.HabboOriginal, "Download quest images", "Download official quest banners and icons.", QuestsDownloader.DownloadQuestsAsync),
        Op("habbo.reception", OperationCategory.HabboOriginal, "Download reception images", "Download official reception and promotional artwork.", ReceptionDownloader.DownloadReceptionImages),
        Op("habbo.texts", OperationCategory.HabboOriginal, "Download texts", "Download official external text resources.", TextsDownloader.DownloadTextsAsync),
        Op("habbo.variables", OperationCategory.HabboOriginal, "Download variables", "Download official external variable resources.", VariablesDownloader.DownloadVariablesAsync),
        Op("habbo.all", OperationCategory.HabboOriginal, "Download all", "Run the complete official asset download sequence.", DownloadAllAsync, true),

        Op("nitro.furniture", OperationCategory.NitroCustom, "Download Nitro furniture", "Download custom Nitro furniture, icons and FurnitureData.json.", NitroFurnitureDownloader.DownloadFurnitureAsync, true),
        Op("nitro.clothes", OperationCategory.NitroCustom, "Download Nitro clothes", "Download custom Nitro clothes, FigureData.json and FigureMap.json.", NitroClothesDownloader.DownloadCustomClothesAsync),

        Op("tools.merge-furnidata", OperationCategory.HotelTools, "Merge furnidata", "Merge strict FurnitureData.json files without duplicate IDs or classnames.", CompareFurnidata.Compare, true),
        Op("tools.merge-productdata", OperationCategory.HotelTools, "Merge productdata", "Merge strict ProductData.json files with conflict control.", CompareProductData.Compare, true),
        Op("tools.merge-clothes", OperationCategory.HotelTools, "Merge clothes", "Merge strict FigureData.json and FigureMap.json files.", CompareClothesData.Compare, true),
        Op("tools.generate-sql", OperationCategory.HotelTools, "Generate SQL", "Generate items_base and catalog_items SQL from furniture assets.", GenerateSqlAsync, true),
        Op("tools.decompile-nitro", OperationCategory.HotelTools, "Decompile Nitro files", "Extract Nitro manifests and spritesheets.", NitroExtractor.Extract),
        Op("tools.compile-nitro", OperationCategory.HotelTools, "Compile Nitro files", "Compile manifests and spritesheets into Nitro bundles.", NitroFurniCompile.Compile),
        Op("tools.swf-furniture", OperationCategory.HotelTools, "SWF furniture to Nitro", "Convert legacy furniture SWF assets to Nitro.", SWF_Furni_To_Nitro.ConvertSwfFilesAsync, true),
        Op("tools.swf-clothes", OperationCategory.HotelTools, "SWF clothes to Nitro", "Convert legacy clothing SWF assets to Nitro.", SWF_clothes_To_Nitro.ConvertSwfFilesAsync, true),
        Op("tools.swf-pets", OperationCategory.HotelTools, "SWF pets to Nitro", "Convert legacy pet SWF assets to Nitro.", SWF_Pets_To_Nitro.ConvertSwfFilesAsync),
        Op("tools.swf-effects", OperationCategory.HotelTools, "SWF effects to Nitro", "Convert legacy effect SWF assets to Nitro.", SWF_Effects_To_Nitro.ConvertSwfFilesAsync),

        Op("database.info", OperationCategory.Database, "Database information", "Show server version, databases and sizes.", DatabaseGeneralInfo.ShowDatabaseVersionAsync),
        Op("database.optimize", OperationCategory.Database, "Optimize database", "Optimize every table in the configured database.", DatabaseOptimizer.OptimizeDatabaseTablesAsync, false, true),
        Op("database.offer-id", OperationCategory.Database, "Fix offer IDs", "Align database offer IDs with FurnitureData.json.", SetOfferID.RunAsync, false, true),
        Op("database.item-settings", OperationCategory.Database, "Fix sit, lay and walk", "Align furniture interaction settings with FurnitureData.json.", FixItemSettings.RunAsync, false, true),
        Op("database.sprite-id", OperationCategory.Database, "Fix sprite and item IDs", "Align sprite and item IDs with FurnitureData.json.", FixSpriteIDSettings.RunAsync, false, true),

        Op("general.version", OperationCategory.General, "Current Habbo client version", "Fetch the current official Habbo client build.", CliRunner.DisplayVersionAsync),
        Op("general.credits", OperationCategory.General, "Credits and contributors", "Show project authors and contributors.", Credits.ShowAsync)
    ];

    private static OperationDefinition Op(
        string id,
        OperationCategory category,
        string title,
        string description,
        Func<Task> action,
        bool requiresInput = false,
        bool isDestructive = false) =>
        new(id, category, title, description, action, requiresInput, isDestructive);

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

    private static Task GenerateSqlAsync()
    {
        SQLGenerator.GenerateSQL();
        return Task.CompletedTask;
    }
}
