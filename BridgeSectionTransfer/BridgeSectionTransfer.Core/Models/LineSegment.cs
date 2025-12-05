using System;

namespace BridgeSectionTransfer.Core.Models;

/// <summary>
/// Represents a line segment defined by two points
/// </summary>
public class LineSegment
{
    public Point2D StartPoint { get; set; } = new();
    public Point2D EndPoint { get; set; } = new();
    public string Name { get; set; } = string.Empty;

    public LineSegment() { }

    public LineSegment(Point2D start, Point2D end, string name = "")
    {
        StartPoint = start;
        EndPoint = end;
        Name = name;
    }

    /// <summary>
    /// Returns the length of this line segment
    /// </summary>
    public double Length()
    {
        double dx = EndPoint.X - StartPoint.X;
        double dy = EndPoint.Y - StartPoint.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>
    /// Returns true if this line is horizontal (within tolerance)
    /// </summary>
    public bool IsHorizontal(double tolerance = 1e-6)
    {
        return Math.Abs(EndPoint.Y - StartPoint.Y) < tolerance;
    }

    /// <summary>
    /// Returns true if this line is vertical (within tolerance)
    /// </summary>
    public bool IsVertical(double tolerance = 1e-6)
    {
        return Math.Abs(EndPoint.X - StartPoint.X) < tolerance;
    }
}
