namespace Habbo_Downloader.Tools
{
    /// <summary>Habbo_Default/hof_furni: swf/ for the .swf files, hab/ for the .hab files, icons/ for the icon .png files.</summary>
    public static class HofFurniPaths
    {
        public static string Root => HabboAssetFolder.Furniture.Root;
        public static string Swf => HabboAssetFolder.Furniture.Swf;
        public static string Hab => HabboAssetFolder.Furniture.Hab;
        public static readonly string Icons = Path.Combine("Habbo_Default", "hof_furni", "icons");

        public static string SwfSource => HabboAssetFolder.Furniture.SwfSource;
    }
}
