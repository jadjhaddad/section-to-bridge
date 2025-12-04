using System;
using System.IO;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;
using BridgeSectionTransfer.CSiBridge;

namespace BridgeSectionTransfer.Console
{
    class Program
    {
        static int Main(string[] args)
        {
            System.Console.WriteLine("=== Bridge Deck Section Transfer Tool ===\n");

            if (args.Length == 0)
            {
                ShowUsage();
                return 1;
            }

            string command = args[0].ToLower();

            switch (command)
            {
                case "import":
                    return HandleImport(args);

                case "validate":
                    return HandleValidate(args);

                case "info":
                    return HandleInfo(args);

                case "--help":
                case "-h":
                case "help":
                    ShowUsage();
                    return 0;

                default:
                    // If first arg is a file path, assume import
                    if (File.Exists(args[0]))
                    {
                        return HandleImport(new[] { "import", args[0] });
                    }
                    System.Console.WriteLine($"Unknown command: {command}");
                    ShowUsage();
                    return 1;
            }
        }

        static void ShowUsage()
        {
            System.Console.WriteLine("Usage:");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe <jsonfile>");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe import <jsonfile> [options]");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe validate <jsonfile>");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe info <jsonfile>");
            System.Console.WriteLine();
            System.Console.WriteLine("Options:");
            System.Console.WriteLine("  --target <name>    Target section name in CSiBridge");
            System.Console.WriteLine("  --no-ref-point     Don't set reference point");
            System.Console.WriteLine("  --keep-voids       Don't clear existing voids");
            System.Console.WriteLine();
            System.Console.WriteLine("Examples:");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe section.json");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe import section.json --target MySection");
            System.Console.WriteLine("  BridgeSectionTransfer.Console.exe validate section.json");
        }

        static int HandleImport(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("ERROR: No JSON file specified.");
                return 1;
            }

            string jsonPath = args[1];

            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"ERROR: File not found: {jsonPath}");
                return 1;
            }

            // Parse options
            var options = new ImportOptions
            {
                SetReferencePoint = true,
                ClearExistingVoids = true,
                CreateNewSection = true
            };

            for (int i = 2; i < args.Length; i++)
            {
                switch (args[i].ToLower())
                {
                    case "--target":
                        if (i + 1 < args.Length)
                        {
                            options.TargetSectionName = args[++i];
                        }
                        break;
                    case "--no-ref-point":
                        options.SetReferencePoint = false;
                        break;
                    case "--keep-voids":
                        options.ClearExistingVoids = false;
                        break;
                }
            }

            // Load JSON
            System.Console.WriteLine($"Loading: {jsonPath}");
            var serializer = new DeckSectionJsonSerializer();
            DeckSection section;

            try
            {
                section = serializer.DeserializeFromFile(jsonPath);
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: Failed to load JSON: {ex.Message}");
                return 1;
            }

            System.Console.WriteLine($"Loaded section: {section.Name}");
            System.Console.WriteLine($"  Exterior vertices: {section.ExteriorBoundary.Points.Count}");
            System.Console.WriteLine($"  Voids: {section.InteriorVoids.Count}");
            System.Console.WriteLine($"  Area: {section.Area:F4}");
            System.Console.WriteLine($"  Reference: ({section.ReferencePoint.X:F4}, {section.ReferencePoint.Y:F4})");
            System.Console.WriteLine();

            // Connect to CSiBridge
            System.Console.WriteLine("Connecting to CSiBridge...");
            var importer = new CSiBridgeImporter();

            if (!importer.Connect())
            {
                System.Console.WriteLine("ERROR: Could not connect to CSiBridge.");
                System.Console.WriteLine("Make sure CSiBridge is running with a model open.");
                return 1;
            }

            // Import
            System.Console.WriteLine();
            bool success = importer.ImportSection(section, options);

            importer.Disconnect();

            return success ? 0 : 1;
        }

        static int HandleValidate(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("ERROR: No JSON file specified.");
                return 1;
            }

            string jsonPath = args[1];

            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"ERROR: File not found: {jsonPath}");
                return 1;
            }

            System.Console.WriteLine($"Validating: {jsonPath}");

            var serializer = new DeckSectionJsonSerializer();
            var geomCalc = new GeometryCalculator();

            try
            {
                var section = serializer.DeserializeFromFile(jsonPath);

                System.Console.WriteLine("\n=== VALIDATION RESULTS ===\n");

                // Check exterior boundary
                if (section.ExteriorBoundary == null || section.ExteriorBoundary.Points.Count < 3)
                {
                    System.Console.WriteLine("[FAIL] Exterior boundary: Missing or invalid (need at least 3 points)");
                    return 1;
                }
                System.Console.WriteLine($"[PASS] Exterior boundary: {section.ExteriorBoundary.Points.Count} vertices");

                // Check area
                double calculatedArea = Math.Abs(geomCalc.CalculateArea(section.ExteriorBoundary.Points));
                if (calculatedArea <= 0)
                {
                    System.Console.WriteLine("[FAIL] Area: Zero or negative");
                    return 1;
                }
                System.Console.WriteLine($"[PASS] Calculated exterior area: {calculatedArea:F4}");

                // Check voids
                foreach (var v in section.InteriorVoids)
                {
                    if (v.Points.Count < 3)
                    {
                        System.Console.WriteLine($"[WARN] Void '{v.Name}': Less than 3 vertices");
                    }
                    else
                    {
                        double voidArea = Math.Abs(geomCalc.CalculateArea(v.Points));
                        System.Console.WriteLine($"[PASS] Void '{v.Name}': {v.Points.Count} vertices, area {voidArea:F4}");
                    }
                }

                // Calculate net area
                double netArea = geomCalc.CalculateNetArea(section);
                System.Console.WriteLine($"\n[INFO] Net area (exterior - voids): {netArea:F4}");

                if (netArea <= 0)
                {
                    System.Console.WriteLine("[WARN] Net area is zero or negative - voids may be larger than exterior!");
                }

                System.Console.WriteLine("\n=== VALIDATION PASSED ===");
                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\n[FAIL] JSON parsing error: {ex.Message}");
                return 1;
            }
        }

        static int HandleInfo(string[] args)
        {
            if (args.Length < 2)
            {
                System.Console.WriteLine("ERROR: No JSON file specified.");
                return 1;
            }

            string jsonPath = args[1];

            if (!File.Exists(jsonPath))
            {
                System.Console.WriteLine($"ERROR: File not found: {jsonPath}");
                return 1;
            }

            var serializer = new DeckSectionJsonSerializer();
            var geomCalc = new GeometryCalculator();

            try
            {
                var section = serializer.DeserializeFromFile(jsonPath);

                System.Console.WriteLine("\n=== SECTION INFO ===\n");
                System.Console.WriteLine($"Name:             {section.Name}");
                System.Console.WriteLine($"Station:          {section.Station:F4}");
                System.Console.WriteLine($"Area:             {section.Area:F4}");
                System.Console.WriteLine($"Centroid:         ({section.Centroid.X:F4}, {section.Centroid.Y:F4})");
                System.Console.WriteLine($"Reference Point:  ({section.ReferencePoint.X:F4}, {section.ReferencePoint.Y:F4}) - {section.ReferencePoint.Description}");

                System.Console.WriteLine($"\nExterior Boundary:");
                System.Console.WriteLine($"  Vertices:       {section.ExteriorBoundary.Points.Count}");
                double extArea = Math.Abs(geomCalc.CalculateArea(section.ExteriorBoundary.Points));
                System.Console.WriteLine($"  Area:           {extArea:F4}");
                double perimeter = geomCalc.CalculatePerimeter(section.ExteriorBoundary.Points);
                System.Console.WriteLine($"  Perimeter:      {perimeter:F4}");

                System.Console.WriteLine($"\nInterior Voids:   {section.InteriorVoids.Count}");
                double totalVoidArea = 0;
                foreach (var v in section.InteriorVoids)
                {
                    double voidArea = Math.Abs(geomCalc.CalculateArea(v.Points));
                    totalVoidArea += voidArea;
                    System.Console.WriteLine($"  - {v.Name}: {v.Points.Count} vertices, area {voidArea:F4}");
                }

                if (section.InteriorVoids.Count > 0)
                {
                    System.Console.WriteLine($"  Total void area: {totalVoidArea:F4}");
                }

                double netArea = extArea - totalVoidArea;
                System.Console.WriteLine($"\nNet Area:         {netArea:F4}");

                System.Console.WriteLine($"\nMaterial:");
                System.Console.WriteLine($"  Concrete Strength: {section.Material.ConcreteStrength} MPa");
                System.Console.WriteLine($"  Density:           {section.Material.Density} kg/m³");
                System.Console.WriteLine($"  Elastic Modulus:   {section.Material.ElasticModulus} MPa");

                return 0;
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"ERROR: {ex.Message}");
                return 1;
            }
        }
    }
}
