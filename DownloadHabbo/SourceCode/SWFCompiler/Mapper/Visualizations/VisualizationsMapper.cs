using System.Collections.Generic;
using System.Xml.Linq;

namespace Habbo_Downloader.SWFCompiler.Mapper.Visualizations
{
    public static class VisualizationsMapper
    {
        private static readonly HashSet<int> ExcludedSizes = new() { 32 }; // Exclude size 32

        public static List<Visualization> MapVisualizationsXml(XElement root)
        {
            if (root == null) return null;

            var visualizations = new List<Visualization>();

            var graphicsElements = root.Elements("graphics");
            foreach (var graphicsElement in graphicsElements)
            {
                var visualizationElements = graphicsElement.Elements("visualization");
                foreach (var visualizationElement in visualizationElements)
                {
                    // Parse visualization
                    var visualization = new Visualization(visualizationElement);

                    // Include only if size is not 32
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