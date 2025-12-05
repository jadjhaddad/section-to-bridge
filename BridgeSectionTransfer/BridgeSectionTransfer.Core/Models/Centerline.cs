using System.Collections.Generic;

namespace BridgeSectionTransfer.Core.Models;

/// <summary>
/// Represents a centerline (mid-surface line) for FEA shell modeling.
/// Centerlines can be polylines with multiple points to follow contours.
/// </summary>
public class Centerline
{
    public string Name { get; set; } = string.Empty;
    public CenterlineType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Points defining the centerline polyline.
    /// For straight lines, this will have 2 points.
    /// For contour-following lines, this will have multiple points.
    /// </summary>
    public List<Point2D> Points { get; set; } = new();

    public Centerline() { }

    public Centerline(List<Point2D> points, CenterlineType type, string name = "", string description = "")
    {
        Points = points ?? new List<Point2D>();
        Type = type;
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Convenience constructor for simple 2-point centerlines
    /// </summary>
    public Centerline(Point2D start, Point2D end, CenterlineType type, string name = "", string description = "")
    {
        Points = new List<Point2D> { start, end };
        Type = type;
        Name = name;
        Description = description;
    }
}

public enum CenterlineType
{
    TopSlab,      // Horizontal centerline for top slab
    BottomSlab,   // Horizontal centerline for bottom slab
    WebExterior,  // Vertical centerline for exterior webs
    WebInterior   // Vertical centerline for interior webs
}
