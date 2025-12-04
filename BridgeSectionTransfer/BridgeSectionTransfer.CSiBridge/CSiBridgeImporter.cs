using System;
using System.Collections.Generic;
using BridgeSectionTransfer.Core.Models;
using CSiAPIv1;

namespace BridgeSectionTransfer.CSiBridge
{
    public class CSiBridgeImporter
    {
        private cOAPI _bridgeObject;
        private cSapModel _model;

        public bool IsConnected => _bridgeObject != null && _model != null;

        public bool Connect()
        {
            try
            {
                cHelper helper = new cHelper();
                _bridgeObject = helper.GetObject("CSI.CSiBridge.API.SapObject") as cOAPI;

                if (_bridgeObject == null)
                {
                    Console.WriteLine("ERROR: Could not get CSiBridge API object. Is CSiBridge running?");
                    return false;
                }

                _model = _bridgeObject.SapModel;

                if (_model == null)
                {
                    Console.WriteLine("ERROR: Could not get SapModel from CSiBridge.");
                    return false;
                }

                Console.WriteLine("Successfully connected to CSiBridge.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR connecting to CSiBridge: {ex.Message}");
                return false;
            }
        }

        public bool ImportSection(DeckSection section, ImportOptions options)
        {
            if (!IsConnected)
            {
                Console.WriteLine("ERROR: Not connected to CSiBridge.");
                return false;
            }

            try
            {
                string sectionName = string.IsNullOrEmpty(options.TargetSectionName)
                    ? section.Name
                    : options.TargetSectionName;

                Console.WriteLine($"Importing section: {sectionName}");

                // Get bridge modeler object
                var bridgeModeler = _model.BridgeModeler_1;
                if (bridgeModeler == null)
                {
                    Console.WriteLine("ERROR: Could not access Bridge Modeler.");
                    return false;
                }

                // Create exterior polygon
                Console.WriteLine($"Creating exterior polygon with {section.ExteriorBoundary.Points.Count} vertices...");

                int numPoints = section.ExteriorBoundary.Points.Count;
                double[] xCoords = new double[numPoints];
                double[] yCoords = new double[numPoints];

                for (int i = 0; i < numPoints; i++)
                {
                    xCoords[i] = section.ExteriorBoundary.Points[i].X;
                    yCoords[i] = section.ExteriorBoundary.Points[i].Y;
                }

                // Add the deck section polygon
                int ret = bridgeModeler.DeckSection.User.AddNewPolygon(
                    sectionName,
                    numPoints,
                    ref xCoords,
                    ref yCoords,
                    1  // PolygonType.Solid
                );

                if (ret != 0)
                {
                    Console.WriteLine($"ERROR: Failed to create exterior polygon (code {ret})");
                    return false;
                }

                Console.WriteLine("Exterior polygon created successfully.");

                // Create void polygons
                foreach (var voidPoly in section.InteriorVoids)
                {
                    Console.WriteLine($"Creating void '{voidPoly.Name}' with {voidPoly.Points.Count} vertices...");

                    int voidNumPoints = voidPoly.Points.Count;
                    double[] voidXCoords = new double[voidNumPoints];
                    double[] voidYCoords = new double[voidNumPoints];

                    for (int i = 0; i < voidNumPoints; i++)
                    {
                        voidXCoords[i] = voidPoly.Points[i].X;
                        voidYCoords[i] = voidPoly.Points[i].Y;
                    }

                    ret = bridgeModeler.DeckSection.User.AddNewPolygon(
                        $"{sectionName}_{voidPoly.Name}",
                        voidNumPoints,
                        ref voidXCoords,
                        ref voidYCoords,
                        2  // PolygonType.Opening
                    );

                    if (ret != 0)
                    {
                        Console.WriteLine($"WARNING: Failed to create void '{voidPoly.Name}' (code {ret})");
                    }
                    else
                    {
                        Console.WriteLine($"Void '{voidPoly.Name}' created successfully.");
                    }
                }

                // Set reference point if requested
                if (options.SetReferencePoint && section.ReferencePoint != null)
                {
                    Console.WriteLine($"Setting reference point: ({section.ReferencePoint.X:F4}, {section.ReferencePoint.Y:F4})");

                    ret = bridgeModeler.DeckSection.User.SetInsertionPoint(
                        sectionName,
                        section.ReferencePoint.X,
                        section.ReferencePoint.Y
                    );

                    if (ret != 0)
                    {
                        Console.WriteLine($"WARNING: Failed to set reference point (code {ret})");
                    }
                    else
                    {
                        Console.WriteLine("Reference point set successfully.");
                    }
                }

                Console.WriteLine($"\n=== IMPORT COMPLETED ===");
                Console.WriteLine($"Section: {sectionName}");
                Console.WriteLine($"Exterior vertices: {section.ExteriorBoundary.Points.Count}");
                Console.WriteLine($"Voids: {section.InteriorVoids.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR during import: {ex.Message}");
                return false;
            }
        }

        public List<string> GetExistingSectionNames()
        {
            var names = new List<string>();

            if (!IsConnected)
                return names;

            try
            {
                int numSections = 0;
                string[] sectionNames = null;

                var bridgeModeler = _model.BridgeModeler_1;
                int ret = bridgeModeler.DeckSection.User.GetNameList(ref numSections, ref sectionNames);

                if (ret == 0 && sectionNames != null)
                {
                    names.AddRange(sectionNames);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WARNING: Could not retrieve section names: {ex.Message}");
            }

            return names;
        }

        public void Disconnect()
        {
            _model = null;
            _bridgeObject = null;
            Console.WriteLine("Disconnected from CSiBridge.");
        }
    }
}
