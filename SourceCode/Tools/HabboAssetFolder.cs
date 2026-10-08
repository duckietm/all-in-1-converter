namespace Habbo_Downloader.Tools
{
    /// <summary>A Habbo_Default asset folder: swf/ for the .swf files and hab/ for the .hab files.</summary>
    public sealed class HabboAssetFolder
    {
        public static readonly HabboAssetFolder Furniture = new("hof_furni");
        public static readonly HabboAssetFolder Clothes = new("clothes");
        public static readonly HabboAssetFolder Effects = new("effects");
        public static readonly HabboAssetFolder Pets = new("pets");

        private HabboAssetFolder(string name)
        {
            Root = Path.Combine("Habbo_Default", name);
            Swf = Path.Combine(Root, "swf");
            Hab = Path.Combine(Root, "hab");
        }

        public string Root { get; }
        public string Swf { get; }
        public string Hab { get; }

        /// <summary>The .swf folder; the root itself for a download made before the swf/ folder existed.</summary>
        public string SwfSource =>
            Directory.Exists(Swf) && Directory.EnumerateFiles(Swf, "*.swf").Any() ? Swf : Root;
    }
}
