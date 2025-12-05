using System;
using System.Collections.Generic;
using System.Linq;
using BridgeSectionTransfer.Core.Models;

namespace BridgeSectionTransfer.Core.Services;

/// <summary>
/// Calculates centerlines and cutlines for bridge deck sections
/// </summary>
public class CenterlineCalculator
{
    private readonly GeometryCalculator _geomCalc;
    private const double Tolerance = 1e-6;

    public CenterlineCalculator(GeometryCalculator geomCalc)
    {
        _geomCalc = geomCalc ?? throw new ArgumentNullException(nameof(geomCalc));
    }

    /// <summary>
    /// Calculates all centerlines and cutlines for a deck section
    /// </summary>
    public void CalculateCenterlinesAndCutlines(DeckSection section)
    {
        if (section == null)
            throw new ArgumentNullException(nameof(section));

        // Step 1: Analyze geometry and compute bounds
        var bounds = AnalyzeSectionGeometry(section);

        // Step 2: Calculate centerlines
        section.Centerlines = CalculateCenterlines(section, bounds);

        // Step 3: Calculate cutlines (positioned between centerlines)
        section.Cutlines = CalculateCutlines(section, bounds, section.Centerlines);

        // Step 4: Validate all lines are within section bounds
        ValidateLines(section, bounds);
    }

    /// <summary>
    /// Analyzes section geometry and returns bounding information
    /// </summary>
    private SectionGeometryBounds AnalyzeSectionGeometry(DeckSection section)
    {
        var bounds = new SectionGeometryBounds();

        // Analyze exterior boundary
        var exteriorPoints = section.ExteriorBoundary.Points;
        if (exteriorPoints.Count == 0)
            throw new InvalidOperationException("Exterior boundary has no points");

        bounds.MinX = exteriorPoints.Min(p => p.X);
        bounds.MaxX = exteriorPoints.Max(p => p.X);
        bounds.MinY = exteriorPoints.Min(p => p.Y);
        bounds.MaxY = exteriorPoints.Max(p => p.Y);

        bounds.TopSurfaceY = bounds.MaxY;
        bounds.BottomSurfaceY = bounds.MinY;

        // Analyze voids if they exist
        if (section.InteriorVoids.Count > 0)
        {
            bounds.TopVoidEdgeY = double.MaxValue;
            bounds.BottomVoidEdgeY = double.MinValue;
            bounds.LeftmostVoidEdgeX = double.MaxValue;
            bounds.RightmostVoidEdgeX = double.MinValue;

            foreach (var voidPolygon in section.InteriorVoids)
            {
                var voidBounds = new VoidBounds
                {
                    VoidName = voidPolygon.Name,
                    MinX = voidPolygon.Points.Min(p => p.X),
                    MaxX = voidPolygon.Points.Max(p => p.X),
                    MinY = voidPolygon.Points.Min(p => p.Y),
                    MaxY = voidPolygon.Points.Max(p => p.Y),
                    Centroid = _geomCalc.CalculateCentroid(voidPolygon.Points)
                };

                bounds.VoidBoundsList.Add(voidBounds);

                // Update global void extrema
                bounds.TopVoidEdgeY = Math.Min(bounds.TopVoidEdgeY.Value, voidBounds.MaxY);
                bounds.BottomVoidEdgeY = Math.Max(bounds.BottomVoidEdgeY.Value, voidBounds.MinY);
                bounds.LeftmostVoidEdgeX = Math.Min(bounds.LeftmostVoidEdgeX.Value, voidBounds.MinX);
                bounds.RightmostVoidEdgeX = Math.Max(bounds.RightmostVoidEdgeX.Value, voidBounds.MaxX);
            }
        }

        return bounds;
    }

    /// <summary>
    /// Calculates all centerlines for the section
    /// </summary>
    private List<Centerline> CalculateCenterlines(DeckSection section, SectionGeometryBounds bounds)
    {
        var centerlines = new List<Centerline>();

        // Calculate top slab centerline
        var topSlabCenterline = CalculateTopSlabCenterline(section, bounds);
        if (topSlabCenterline != null)
            centerlines.Add(topSlabCenterline);

        // Calculate bottom slab centerline
        var bottomSlabCenterline = CalculateBottomSlabCenterline(section, bounds);
        if (bottomSlabCenterline != null)
            centerlines.Add(bottomSlabCenterline);

        // Calculate web centerlines
        centerlines.AddRange(CalculateWebCenterlines(section, bounds));

        return centerlines;
    }

    /// <summary>
    /// Calculates top slab centerline following the contour
    /// Rule: With voids: Y = (top of deck + top edge of voids) / 2
    ///       Without voids: Y = top surface - (thickness / 2) using 10% heuristic
    /// </summary>
    private Centerline CalculateTopSlabCenterline(DeckSection section, SectionGeometryBounds bounds)
    {
        List<Point2D> centerlinePoints = new();

        if (bounds.TopVoidEdgeY.HasValue)
        {
            // With voids: trace contour at mid-thickness
            double midThicknessY = (bounds.TopSurfaceY + bounds.TopVoidEdgeY.Value) / 2.0;

            // For now, create simple horizontal line
            // TODO: Implement full contour-following when top surface is sloped
            centerlinePoints.Add(new Point2D(bounds.MinX, midThicknessY));
            centerlinePoints.Add(new Point2D(bounds.MaxX, midThicknessY));

            return new Centerline(
                centerlinePoints,
                CenterlineType.TopSlab,
                "TopSlabCL",
                $"Top slab centerline at Y={midThicknessY:F4} (midpoint of top slab)"
            );
        }
        else
        {
            // Without voids: use 10% heuristic
            double slabThickness = bounds.Height * 0.1;
            double centerlineY = bounds.TopSurfaceY - (slabThickness / 2.0);

            centerlinePoints.Add(new Point2D(bounds.MinX, centerlineY));
            centerlinePoints.Add(new Point2D(bounds.MaxX, centerlineY));

            return new Centerline(
                centerlinePoints,
                CenterlineType.TopSlab,
                "TopSlabCL",
                $"Top slab centerline at Y={centerlineY:F4} (estimated thickness={slabThickness:F4})"
            );
        }
    }

    /// <summary>
    /// Calculates bottom slab centerline following the contour
    /// Rule: With voids: Y = (bottom of section + bottom edge of voids) / 2
    ///       Without voids: Y = bottom surface + (thickness / 2) using 10% heuristic
    /// </summary>
    private Centerline CalculateBottomSlabCenterline(DeckSection section, SectionGeometryBounds bounds)
    {
        List<Point2D> centerlinePoints = new();

        if (bounds.BottomVoidEdgeY.HasValue)
        {
            // With voids: trace contour at mid-thickness
            double midThicknessY = (bounds.BottomSurfaceY + bounds.BottomVoidEdgeY.Value) / 2.0;

            // For now, create simple horizontal line
            // TODO: Implement full contour-following when bottom surface is sloped
            centerlinePoints.Add(new Point2D(bounds.MinX, midThicknessY));
            centerlinePoints.Add(new Point2D(bounds.MaxX, midThicknessY));

            return new Centerline(
                centerlinePoints,
                CenterlineType.BottomSlab,
                "BottomSlabCL",
                $"Bottom slab centerline at Y={midThicknessY:F4} (midpoint of bottom slab)"
            );
        }
        else
        {
            // Without voids: use 10% heuristic
            double slabThickness = bounds.Height * 0.1;
            double centerlineY = bounds.BottomSurfaceY + (slabThickness / 2.0);

            centerlinePoints.Add(new Point2D(bounds.MinX, centerlineY));
            centerlinePoints.Add(new Point2D(bounds.MaxX, centerlineY));

            return new Centerline(
                centerlinePoints,
                CenterlineType.BottomSlab,
                "BottomSlabCL",
                $"Bottom slab centerline at Y={centerlineY:F4} (estimated thickness={slabThickness:F4})"
            );
        }
    }

    /// <summary>
    /// Calculates all web centerlines (exterior and interior)
    /// </summary>
    private List<Centerline> CalculateWebCenterlines(DeckSection section, SectionGeometryBounds bounds)
    {
        var webCenterlines = new List<Centerline>();

        if (section.InteriorVoids.Count == 0)
        {
            // No voids: no web centerlines needed (solid section)
            return webCenterlines;
        }

        // Sort voids by X position (left to right)
        var sortedVoids = bounds.VoidBoundsList.OrderBy(v => v.Centroid.X).ToList();

        // Calculate exterior web centerlines

        // Left exterior web: midpoint between left section edge and leftmost void
        double leftWebX = (bounds.MinX + sortedVoids[0].MinX) / 2.0;
        var leftWebPoints = new List<Point2D>
        {
            new Point2D(leftWebX, bounds.BottomSurfaceY),
            new Point2D(leftWebX, bounds.TopSurfaceY)
        };
        webCenterlines.Add(new Centerline(
            leftWebPoints,
            CenterlineType.WebExterior,
            "LeftWebCL",
            $"Left exterior web at X={leftWebX:F4}"
        ));

        // Right exterior web: midpoint between right section edge and rightmost void
        double rightWebX = (bounds.MaxX + sortedVoids[^1].MaxX) / 2.0;
        var rightWebPoints = new List<Point2D>
        {
            new Point2D(rightWebX, bounds.BottomSurfaceY),
            new Point2D(rightWebX, bounds.TopSurfaceY)
        };
        webCenterlines.Add(new Centerline(
            rightWebPoints,
            CenterlineType.WebExterior,
            "RightWebCL",
            $"Right exterior web at X={rightWebX:F4}"
        ));

        // Calculate interior web centerlines (between adjacent voids)
        for (int i = 0; i < sortedVoids.Count - 1; i++)
        {
            double interiorWebX = (sortedVoids[i].MaxX + sortedVoids[i + 1].MinX) / 2.0;
            var interiorWebPoints = new List<Point2D>
            {
                new Point2D(interiorWebX, bounds.BottomSurfaceY),
                new Point2D(interiorWebX, bounds.TopSurfaceY)
            };
            webCenterlines.Add(new Centerline(
                interiorWebPoints,
                CenterlineType.WebInterior,
                $"Web{i + 1}CL",
                $"Interior web {i + 1} at X={interiorWebX:F4}"
            ));
        }

        return webCenterlines;
    }

    /// <summary>
    /// Calculates all cutlines positioned between centerlines
    /// </summary>
    private List<Cutline> CalculateCutlines(DeckSection section, SectionGeometryBounds bounds, List<Centerline> centerlines)
    {
        var cutlines = new List<Cutline>();

        // Calculate horizontal cutlines (top and bottom)
        var horizontalCutlines = CalculateHorizontalCutlines(section, bounds, centerlines);
        cutlines.AddRange(horizontalCutlines);

        // Calculate vertical cutlines (between web centerlines)
        var verticalCutlines = CalculateVerticalCutlines(bounds, centerlines);
        cutlines.AddRange(verticalCutlines);

        return cutlines;
    }

    /// <summary>
    /// Calculates horizontal cutlines through second-highest and second-lowest void points
    /// </summary>
    private List<Cutline> CalculateHorizontalCutlines(DeckSection section, SectionGeometryBounds bounds, List<Centerline> centerlines)
    {
        var cutlines = new List<Cutline>();

        if (section.InteriorVoids.Count == 0)
            return cutlines;

        // Collect all Y-coordinates from void polygons
        var allVoidYCoords = section.InteriorVoids
            .SelectMany(v => v.Points.Select(p => p.Y))
            .OrderByDescending(y => y)
            .ToList();

        if (allVoidYCoords.Count < 2)
            return cutlines;

        // Find second highest Y value
        var secondHighestY = FindSecondMostCommonOrAverage(allVoidYCoords, true);
        if (secondHighestY.HasValue)
        {
            var topCutlinePoints = new List<Point2D>
            {
                new Point2D(bounds.MinX, secondHighestY.Value),
                new Point2D(bounds.MaxX, secondHighestY.Value)
            };
            cutlines.Add(new Cutline(
                topCutlinePoints,
                CutlineType.HorizontalTop,
                "TopCutline",
                $"Top horizontal cutline at Y={secondHighestY.Value:F4} (second-highest void points)"
            ));
        }

        // Find second lowest Y value
        var allVoidYCoordsAscending = allVoidYCoords.OrderBy(y => y).ToList();
        var secondLowestY = FindSecondMostCommonOrAverage(allVoidYCoordsAscending, false);
        if (secondLowestY.HasValue)
        {
            var bottomCutlinePoints = new List<Point2D>
            {
                new Point2D(bounds.MinX, secondLowestY.Value),
                new Point2D(bounds.MaxX, secondLowestY.Value)
            };
            cutlines.Add(new Cutline(
                bottomCutlinePoints,
                CutlineType.HorizontalBottom,
                "BottomCutline",
                $"Bottom horizontal cutline at Y={secondLowestY.Value:F4} (second-lowest void points)"
            ));
        }

        return cutlines;
    }

    /// <summary>
    /// Finds the second-most common value, or average of second values
    /// </summary>
    private double? FindSecondMostCommonOrAverage(List<double> values, bool isDescending)
    {
        if (values.Count < 2)
            return null;

        // Group by value (with tolerance) and count occurrences
        var groups = values
            .GroupBy(v => Math.Round(v / Tolerance) * Tolerance) // Group within tolerance
            .OrderByDescending(g => g.Count())
            .ToList();

        if (groups.Count < 2)
        {
            // All values are the same, return the unique value
            if (values.Count >= 2)
                return values[1]; // Second value
            return null;
        }

        // Check if there's a clear second-most common value (majority)
        var mostCommon = groups[0].Key;
        var secondMostCommon = groups[1].Key;

        // If second group has significant count (>= 25% of total), use it
        if (groups[1].Count() >= values.Count * 0.25)
            return secondMostCommon;

        // Otherwise, find the second unique value
        var uniqueValues = values.Distinct().OrderBy(v => isDescending ? -v : v).ToList();
        if (uniqueValues.Count >= 2)
            return uniqueValues[1];

        return null;
    }

    /// <summary>
    /// Calculates vertical cutlines positioned between adjacent web centerlines
    /// </summary>
    private List<Cutline> CalculateVerticalCutlines(SectionGeometryBounds bounds, List<Centerline> centerlines)
    {
        var cutlines = new List<Cutline>();

        // Get all web centerlines (exterior and interior)
        var webCenterlines = centerlines
            .Where(cl => cl.Type == CenterlineType.WebExterior || cl.Type == CenterlineType.WebInterior)
            .OrderBy(cl => cl.Points[0].X) // Sort by X position
            .ToList();

        if (webCenterlines.Count < 2)
            return cutlines;

        // Create cutline between each adjacent pair of web centerlines
        for (int i = 0; i < webCenterlines.Count - 1; i++)
        {
            var leftWeb = webCenterlines[i];
            var rightWeb = webCenterlines[i + 1];

            // Calculate midpoint between the two web centerlines
            // For simple vertical webs, this is straightforward
            // For sloped webs, we interpolate at each Y position

            var cutlinePoints = InterpolateBetweenCenterlines(leftWeb.Points, rightWeb.Points);

            cutlines.Add(new Cutline(
                cutlinePoints,
                CutlineType.VerticalWeb,
                $"VerticalCut{i + 1}",
                $"Vertical cutline between {leftWeb.Name} and {rightWeb.Name}"
            ));
        }

        return cutlines;
    }

    /// <summary>
    /// Interpolates a polyline positioned at the midpoint between two centerlines
    /// </summary>
    private List<Point2D> InterpolateBetweenCenterlines(List<Point2D> leftPoints, List<Point2D> rightPoints)
    {
        var resultPoints = new List<Point2D>();

        // Simple case: both centerlines are 2-point vertical lines
        if (leftPoints.Count == 2 && rightPoints.Count == 2)
        {
            // Calculate midpoint at bottom
            double midXBottom = (leftPoints[0].X + rightPoints[0].X) / 2.0;
            double yBottom = Math.Min(leftPoints[0].Y, rightPoints[0].Y);

            // Calculate midpoint at top
            double midXTop = (leftPoints[1].X + rightPoints[1].X) / 2.0;
            double yTop = Math.Max(leftPoints[1].Y, rightPoints[1].Y);

            resultPoints.Add(new Point2D(midXBottom, yBottom));
            resultPoints.Add(new Point2D(midXTop, yTop));

            return resultPoints;
        }

        // TODO: Implement more sophisticated interpolation for multi-segment polylines
        // For now, fall back to simple midpoint calculation

        // Find Y range
        double minY = Math.Min(leftPoints.Min(p => p.Y), rightPoints.Min(p => p.Y));
        double maxY = Math.Max(leftPoints.Max(p => p.Y), rightPoints.Max(p => p.Y));

        // Get X at bottom and top
        double leftXBottom = GetXAtY(leftPoints, minY);
        double rightXBottom = GetXAtY(rightPoints, minY);
        double midXBottom = (leftXBottom + rightXBottom) / 2.0;

        double leftXTop = GetXAtY(leftPoints, maxY);
        double rightXTop = GetXAtY(rightPoints, maxY);
        double midXTop = (leftXTop + rightXTop) / 2.0;

        resultPoints.Add(new Point2D(midXBottom, minY));
        resultPoints.Add(new Point2D(midXTop, maxY));

        return resultPoints;
    }

    /// <summary>
    /// Gets X coordinate at a specific Y position by interpolating along polyline
    /// </summary>
    private double GetXAtY(List<Point2D> points, double targetY)
    {
        if (points.Count == 0)
            return 0;

        // Find the two points that bracket the target Y
        for (int i = 0; i < points.Count - 1; i++)
        {
            double y1 = points[i].Y;
            double y2 = points[i + 1].Y;

            if ((targetY >= y1 && targetY <= y2) || (targetY >= y2 && targetY <= y1))
            {
                // Interpolate X
                double x1 = points[i].X;
                double x2 = points[i + 1].X;

                if (Math.Abs(y2 - y1) < Tolerance)
                    return x1; // Horizontal segment

                double t = (targetY - y1) / (y2 - y1);
                return x1 + t * (x2 - x1);
            }
        }

        // If target Y is outside range, return closest point
        if (targetY < points.Min(p => p.Y))
            return points.OrderBy(p => p.Y).First().X;
        return points.OrderByDescending(p => p.Y).First().X;
    }

    /// <summary>
    /// Validates that all calculated lines are within section bounds
    /// </summary>
    private void ValidateLines(DeckSection section, SectionGeometryBounds bounds)
    {
        // Expand bounds slightly for tolerance
        double xMin = bounds.MinX - Tolerance;
        double xMax = bounds.MaxX + Tolerance;
        double yMin = bounds.BottomSurfaceY - Tolerance;
        double yMax = bounds.TopSurfaceY + Tolerance;

        // Validate centerlines
        foreach (var centerline in section.Centerlines)
        {
            foreach (var point in centerline.Points)
            {
                ValidatePoint(point, xMin, xMax, yMin, yMax, $"Centerline {centerline.Name}");
            }
        }

        // Validate cutlines
        foreach (var cutline in section.Cutlines)
        {
            foreach (var point in cutline.Points)
            {
                ValidatePoint(point, xMin, xMax, yMin, yMax, $"Cutline {cutline.Name}");
            }
        }
    }

    private void ValidatePoint(Point2D point, double xMin, double xMax, double yMin, double yMax, string description)
    {
        if (point.X < xMin || point.X > xMax || point.Y < yMin || point.Y > yMax)
        {
            throw new InvalidOperationException(
                $"Invalid {description}: ({point.X:F4}, {point.Y:F4}) is outside section bounds " +
                $"[X: {xMin:F4} to {xMax:F4}, Y: {yMin:F4} to {yMax:F4}]"
            );
        }
    }
}
