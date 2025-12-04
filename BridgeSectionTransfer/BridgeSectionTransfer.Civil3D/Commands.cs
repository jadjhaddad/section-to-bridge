using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

namespace BridgeSectionTransfer.Civil3D
{
    public class Commands
    {
        private readonly GeometryCalculator _geomCalc = new GeometryCalculator();

        [CommandMethod("ExportDeckSection")]
        public void ExportDeckSection()
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            ed.WriteMessage("\n=== Bridge Deck Section Export ===\n");

            // Step 1: Select ALL polylines at once (exterior + voids)
            TypedValue[] filterList = new TypedValue[]
            {
                new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
            };
            SelectionFilter filter = new SelectionFilter(filterList);

            PromptSelectionOptions pso = new PromptSelectionOptions
            {
                MessageForAdding = "\nSelect exterior boundary and all voids (select all at once): "
            };

            PromptSelectionResult psr = ed.GetSelection(pso, filter);

            if (psr.Status != PromptStatus.OK)
            {
                ed.WriteMessage("\nSelection cancelled.\n");
                return;
            }

            ObjectId[] selectedIds = psr.Value.GetObjectIds();

            if (selectedIds.Length == 0)
            {
                ed.WriteMessage("\nNo polylines selected.\n");
                return;
            }

            // Step 2: Identify exterior (largest area) and voids automatically
            List<PolylineData> polylines = new List<PolylineData>();

            using (Transaction tr = doc.Database.TransactionManager.StartTransaction())
            {
                foreach (ObjectId id in selectedIds)
                {
                    Polyline poly = tr.GetObject(id, OpenMode.ForRead) as Polyline;
                    if (poly != null)
                    {
                        var polygon = ExtractPolygon(poly, "");
                        double area = _geomCalc.CalculateArea(polygon.Points);

                        polylines.Add(new PolylineData
                        {
                            Id = id,
                            Polygon = polygon,
                            Area = Math.Abs(area)
                        });
                    }
                }
                tr.Commit();
            }

            // Step 3: Sort by area - largest is exterior
            polylines = polylines.OrderByDescending(p => p.Area).ToList();

            var section = new DeckSection();
            section.ExteriorBoundary = polylines[0].Polygon;
            section.ExteriorBoundary.Name = "Exterior";
            section.ExteriorBoundary.Type = PolygonType.Solid;

            // Ensure exterior is clockwise
            section.ExteriorBoundary.Points = _geomCalc.EnsureClockwise(section.ExteriorBoundary.Points);

            // Step 4: Rest are voids
            for (int i = 1; i < polylines.Count; i++)
            {
                polylines[i].Polygon.Name = $"Void_{i}";
                polylines[i].Polygon.Type = PolygonType.Opening;
                // Ensure voids are counter-clockwise
                polylines[i].Polygon.Points = _geomCalc.EnsureCounterClockwise(polylines[i].Polygon.Points);
                section.InteriorVoids.Add(polylines[i].Polygon);
            }

            ed.WriteMessage($"\nIdentified: 1 exterior + {section.InteriorVoids.Count} voids\n");

            // Step 5: Calculate area and centroid
            section.Area = _geomCalc.CalculateNetArea(section);
            section.Centroid = _geomCalc.CalculateCentroid(section.ExteriorBoundary.Points);

            ed.WriteMessage($"Net Area: {section.Area:F4} sq units\n");
            ed.WriteMessage($"Centroid: ({section.Centroid.X:F4}, {section.Centroid.Y:F4})\n");

            // Step 6: Get section name
            PromptStringOptions psoName = new PromptStringOptions("\nEnter section name [default: DeckSection_01]: ");
            psoName.AllowSpaces = true;
            psoName.DefaultValue = "DeckSection_01";
            PromptResult prName = ed.GetString(psoName);

            section.Name = string.IsNullOrWhiteSpace(prName.StringResult) ? "DeckSection_01" : prName.StringResult;

            // Step 7: Get station
            PromptDoubleOptions pdoStation = new PromptDoubleOptions("\nEnter station value [default: 0.0]: ");
            pdoStation.DefaultValue = 0.0;
            pdoStation.AllowNegative = true;
            PromptDoubleResult pdrStation = ed.GetDouble(pdoStation);

            section.Station = pdrStation.Status == PromptStatus.OK ? pdrStation.Value : 0.0;

            // Step 8: Get reference point
            section.ReferencePoint = GetReferencePoint(ed, section.Centroid);

            ed.WriteMessage($"Reference Point: ({section.ReferencePoint.X:F4}, {section.ReferencePoint.Y:F4}) - {section.ReferencePoint.Description}\n");

            // Step 9: Export to JSON
            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                Title = "Save Bridge Deck Section",
                FileName = $"{section.Name}.json"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                ed.WriteMessage("\nExport cancelled.\n");
                return;
            }

            var serializer = new DeckSectionJsonSerializer();
            serializer.SerializeToFile(section, sfd.FileName);

            ed.WriteMessage($"\n=== EXPORT COMPLETED ===\n");
            ed.WriteMessage($"File: {sfd.FileName}\n");
            ed.WriteMessage($"Section: {section.Name}\n");
            ed.WriteMessage($"Exterior vertices: {section.ExteriorBoundary.Points.Count}\n");
            ed.WriteMessage($"Voids: {section.InteriorVoids.Count}\n");
        }

        private Polygon ExtractPolygon(Polyline poly, string name)
        {
            var polygon = new Polygon
            {
                Name = name,
                Handle = poly.Handle.ToString()
            };

            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point2d pt = poly.GetPoint2dAt(i);
                polygon.Points.Add(new Point2D(pt.X, pt.Y));
            }

            return polygon;
        }

        private ReferencePoint GetReferencePoint(Editor ed, Point2D centroid)
        {
            PromptKeywordOptions pko = new PromptKeywordOptions(
                "\nReference point [Centerline/Centroid/Pick]: "
            );
            pko.Keywords.Add("Centerline");
            pko.Keywords.Add("Centroid");
            pko.Keywords.Add("Pick");
            pko.Keywords.Default = "Centerline";
            pko.AllowNone = true;

            PromptResult pr = ed.GetKeywords(pko);

            if (pr.Status != PromptStatus.OK && pr.Status != PromptStatus.None)
                return new ReferencePoint { X = 0, Y = 0, Description = "Default" };

            string choice = pr.Status == PromptStatus.None ? "Centerline" : pr.StringResult;

            switch (choice)
            {
                case "Centerline":
                    return new ReferencePoint
                    {
                        X = 0,
                        Y = 0,
                        Description = "Centerline at origin"
                    };

                case "Centroid":
                    return new ReferencePoint
                    {
                        X = centroid.X,
                        Y = centroid.Y,
                        Description = "Section centroid"
                    };

                case "Pick":
                    PromptPointOptions ppo = new PromptPointOptions("\nPick reference point: ");
                    PromptPointResult ppr = ed.GetPoint(ppo);
                    if (ppr.Status == PromptStatus.OK)
                    {
                        return new ReferencePoint
                        {
                            X = ppr.Value.X,
                            Y = ppr.Value.Y,
                            Description = "Custom point"
                        };
                    }
                    break;
            }

            return new ReferencePoint { X = 0, Y = 0, Description = "Default" };
        }

        private class PolylineData
        {
            public ObjectId Id { get; set; }
            public Polygon Polygon { get; set; }
            public double Area { get; set; }
        }
    }
}
