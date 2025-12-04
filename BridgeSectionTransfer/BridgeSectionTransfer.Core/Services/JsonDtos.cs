namespace BridgeSectionTransfer.Core.Services;

public class BridgeDeckSectionsData
{
    public ExportInfo ExportInfo { get; set; } = new();
    public List<DeckSectionDto> Sections { get; set; } = new();
}

public class ExportInfo
{
    public DateTime Date { get; set; }
    public string Tool { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Units { get; set; } = string.Empty;
    public string CoordinateSystem { get; set; } = string.Empty;
}

public class DeckSectionDto
{
    public string Name { get; set; } = string.Empty;
    public double Station { get; set; }
    public double Area { get; set; }
    public Point2DDto Centroid { get; set; } = new();
    public ReferencePointDto ReferencePoint { get; set; } = new();
    public MaterialPropertiesDto Material { get; set; } = new();
    public List<Point2DDto> ExteriorBoundary { get; set; } = new();
    public List<VoidDto> InteriorVoids { get; set; } = new();
}

public class Point2DDto
{
    public double X { get; set; }
    public double Y { get; set; }
}

public class ReferencePointDto
{
    public double X { get; set; }
    public double Y { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class MaterialPropertiesDto
{
    public double ConcreteStrength { get; set; }
    public double Density { get; set; }
    public double ElasticModulus { get; set; }
}

public class VoidDto
{
    public string Name { get; set; } = string.Empty;
    public List<Point2DDto> Points { get; set; } = new();
}
