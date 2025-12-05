using System.Collections.Generic;

namespace BridgeSectionTransfer.Core.Models;

/// <summary>
/// Represents a cutline (reference line between centerlines) for section definition.
/// Cutlines can be polylines with multiple points to follow contours.
/// </summary>
public class Cutline
{
    public string Name { get; set; } = string.Empty;
    public CutlineType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Points defining the cutline polyline.
    /// For straight lines, this will have 2 points.
    /// For contour-following lines, this will have multiple points.
    /// </summary>
    public List<Point2D> Points { get; set; } = new();

    public Cutline() { }

    public Cutline(List<Point2D> points, CutlineType type, string name = "", string description = "")
    {
        Points = points ?? new List<Point2D>();
        Type = type;
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Convenience constructor for simple 2-point cutlines
    /// </summary>
    public Cutline(Point2D start, Point2D end, CutlineType type, string name = "", string description = "")
    {
        Points = new List<Point2D> { start, end };
        Type = type;
        Name = name;
        Description = description;
    }
}

public enum CutlineType
{
    HorizontalTop,      // Horizontal cutline through second-highest void points
    HorizontalBottom,   // Horizontal cutline through second-lowest void points
    VerticalWeb         // Vertical cutline between adjacent web centerlines
}
