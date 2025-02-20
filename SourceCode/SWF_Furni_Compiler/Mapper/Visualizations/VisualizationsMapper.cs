using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public static class VisualizationsMapper
    {
        private static readonly HashSet<int> ExcludedSizes = new() { 32 };

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
