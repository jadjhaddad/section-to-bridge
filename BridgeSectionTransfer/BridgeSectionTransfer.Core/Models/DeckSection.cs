namespace BridgeSectionTransfer.Core.Models;

public class DeckSection
{
    public string Name { get; set; } = string.Empty;
    public double Station { get; set; }
    public double Area { get; set; }
    public Point2D Centroid { get; set; } = new();
    public ReferencePoint ReferencePoint { get; set; } = new();
    public MaterialProperties Material { get; set; } = new();
    public Polygon ExteriorBoundary { get; set; } = new();
    public List<Polygon> InteriorVoids { get; set; } = new();
}
