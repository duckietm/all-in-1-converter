using System.Xml.Linq;

namespace Habbo_Downloader.SWF_Pets_Compiler.Mapper.Visualizations
{
    public static class VisualizationsMapper
    {
        // Emit every authored size (32 and 64). Pets whose size-32 art is
        // incomplete (fewer animations, or most frames missing) are filtered out
        // later in SWF_Pets_To_Nitro.ShouldKeepSize32, so only pets with a usable
        // size-32 keep it; the rest fall back to size-64 like before.
        private static readonly HashSet<int> ExcludedSizes = new();

        public static List<Visualization> MapVisualizationsXml(XElement root)
        {
            if (root == null) return null;

            var visualizations = new List<Visualization>();

            foreach (var graphicsElement in root.Elements("graphics"))
            {
                foreach (var visualizationElement in graphicsElement.Elements("visualization"))
                {
                    var visualization = new Visualization(visualizationElement);

                    if (!ExcludedSizes.Contains(visualization.Size))
                    {
                        visualizations.Add(visualization);
                    }
                }
            }

            return visualizations;
        }
    }
}
