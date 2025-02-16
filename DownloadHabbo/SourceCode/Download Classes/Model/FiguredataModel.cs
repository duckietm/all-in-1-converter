using System.Text.Json.Serialization;

namespace ConsoleApplication
{
    public class FigureDataHiddenLayer
    {
        public string PartType { get; set; }
    }

    public class FigureDataColor
    {
        public int Id { get; set; }
        public int Index { get; set; }
        public int Club { get; set; }
        public bool Selectable { get; set; }
        public string HexCode { get; set; }
    }

    public class FigureDataPalette
    {
        public int Id { get; set; }
        public List<FigureDataColor> Colors { get; set; }
    }

    public class FigureDataPart
    {
        public int Id { get; set; }
        public string Type { get; set; }
        public bool Colorable { get; set; }
        public int Index { get; set; }
        public int ColorIndex { get; set; }
    }

    public class FigureDataSet
    {
        public int Id { get; set; }
        public string Gender { get; set; }
        public int Club { get; set; }
        public bool Colorable { get; set; }
        public bool Selectable { get; set; }
        public bool Preselectable { get; set; }
        public bool Sellable { get; set; }
        public List<FigureDataPart> Parts { get; set; }
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<FigureDataHiddenLayer> HiddenLayers { get; set; }
    }

    public class FigureDataSetType
    {
        public string Type { get; set; }
        public int PaletteId { get; set; }
        public bool MandatoryF0 { get; set; }
        public bool MandatoryF1 { get; set; }
        public bool MandatoryM0 { get; set; }
        public bool MandatoryM1 { get; set; }
        
        public List<FigureDataSet> Sets { get; set; }
    }

    public class FigureData
    {
        public List<FigureDataPalette> Palettes { get; set; }
        public List<FigureDataSetType> SetTypes { get; set; }
    }

    public class FigureMap
    {
        public List<FigureMapLibrary> Libraries { get; set; }
    }

    public class FigureMapLibrary
    {
        public string Id { get; set; }
        public int Revision { get; set; }
        public List<FigureMapLibraryPart> Parts { get; set; }
    }

    public class FigureMapLibraryPart
    {
        public int? Id { get; set; }
        public string Type { get; set; }
    }
}
