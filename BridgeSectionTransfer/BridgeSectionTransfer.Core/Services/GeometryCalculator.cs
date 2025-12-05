using System;
using System.Collections.Generic;
using System.Linq;
using BridgeSectionTransfer.Core.Models;

namespace BridgeSectionTransfer.Core.Services;

public class GeometryCalculator
{
    /// <summary>
    /// Calculates the area of a polygon using the Shoelace formula.
    /// Returns positive for clockwise, negative for counter-clockwise.
    /// </summary>
    public double CalculateArea(List<Point2D> points)
    {
        if (points == null || points.Count < 3)
            return 0;

        double area = 0;
        int n = points.Count;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            area += points[i].X * points[j].Y;
            area -= points[j].X * points[i].Y;
        }

        return area / 2.0;
    }

    /// <summary>
    /// Calculates the centroid of a polygon.
    /// </summary>
    public Point2D CalculateCentroid(List<Point2D> points)
    {
        if (points == null || points.Count < 3)
            return new Point2D(0, 0);

        double area = CalculateArea(points);
        if (Math.Abs(area) < 1e-10)
            return new Point2D(0, 0);

        double cx = 0;
        double cy = 0;
        int n = points.Count;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double factor = (points[i].X * points[j].Y - points[j].X * points[i].Y);
            cx += (points[i].X + points[j].X) * factor;
            cy += (points[i].Y + points[j].Y) * factor;
        }

        double areaFactor = 6.0 * area;
        return new Point2D(cx / areaFactor, cy / areaFactor);
    }

    /// <summary>
    /// Ensures the polygon points are in clockwise order.
    /// </summary>
    public List<Point2D> EnsureClockwise(List<Point2D> points)
    {
        if (points == null || points.Count < 3)
            return points ?? new List<Point2D>();

        double area = CalculateArea(points);

        // If area is negative, points are counter-clockwise, so reverse them
        if (area < 0)
        {
            var reversed = new List<Point2D>(points);
            reversed.Reverse();
            return reversed;
        }

        return points;
    }

    /// <summary>
    /// Ensures the polygon points are in counter-clockwise order (for voids).
    /// </summary>
    public List<Point2D> EnsureCounterClockwise(List<Point2D> points)
    {
        if (points == null || points.Count < 3)
            return points ?? new List<Point2D>();

        double area = CalculateArea(points);

        // If area is positive, points are clockwise, so reverse them
        if (area > 0)
        {
            var reversed = new List<Point2D>(points);
            reversed.Reverse();
            return reversed;
        }

        return points;
    }

    /// <summary>
    /// Calculates the net area of a section (exterior minus voids).
    /// </summary>
    public double CalculateNetArea(DeckSection section)
    {
        double exteriorArea = Math.Abs(CalculateArea(section.ExteriorBoundary.Points));
        double voidsArea = section.InteriorVoids
            .Sum(v => Math.Abs(CalculateArea(v.Points)));

        return exteriorArea - voidsArea;
    }

    /// <summary>
    /// Calculates the perimeter of a polygon.
    /// </summary>
    public double CalculatePerimeter(List<Point2D> points)
    {
        if (points == null || points.Count < 2)
            return 0;

        double perimeter = 0;
        int n = points.Count;

        for (int i = 0; i < n; i++)
        {
            int j = (i + 1) % n;
            double dx = points[j].X - points[i].X;
            double dy = points[j].Y - points[i].Y;
            perimeter += Math.Sqrt(dx * dx + dy * dy);
        }

        return perimeter;
    }
}
