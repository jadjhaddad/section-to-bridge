namespace BridgeSectionTransfer.Core.Models;

public class Polygon
{
    public string Name { get; set; } = string.Empty;
    public PolygonType Type { get; set; }
    public List<Point2D> Points { get; set; } = new();
    public string Handle { get; set; } = string.Empty;
}

public enum PolygonType
{
    Solid = 1,
    Opening = 2
}
