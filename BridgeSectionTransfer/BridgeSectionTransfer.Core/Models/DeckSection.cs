using System.Collections.Generic;

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

    /// <summary>
    /// Centerlines calculated from section geometry
    /// </summary>
    public List<Centerline> Centerlines { get; set; } = new();

    /// <summary>
    /// Cutlines calculated from section geometry (positioned between centerlines)
    /// </summary>
    public List<Cutline> Cutlines { get; set; } = new();
}
