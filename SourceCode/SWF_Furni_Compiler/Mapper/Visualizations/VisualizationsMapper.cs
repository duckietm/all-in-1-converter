using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public static class VisualizationsMapper
    {
        // Include every authored size (1, 32, 64). Size 32 is Sulake's hand-tuned
        // zoomed-out art; keeping it lets furni stay crisp when the room zooms out.
        private static readonly HashSet<int> ExcludedSizes = new();

        public static List<Visualization> MapVisualizationsXml(XElement root)
        {
            if (root == null) return null;

            var visualizations = new List<Visualization>();

            var visualizationElements = root.Elements("graphics")
                .SelectMany(graphicsElement => graphicsElement.Elements("visualization"))
                .Concat(root.Elements("visualization"));

            foreach (var visualizationElement in visualizationElements)
            {
                var visualization = new Visualization(visualizationElement);

                if (!ExcludedSizes.Contains(visualization.Size))
                {
                    visualizations.Add(visualization);
                }
            }

            return visualizations;
        }
    }
}
