# Bridge Cross-Section Documentation - Part 5: Complete C# Implementation

---

## 10.4 Civil 3D Exporter (C# .NET Plugin)

### Civil3DExporter.cs

```csharp
using System;
using System.Collections.Generic;
using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

[assembly: CommandClass(typeof(BridgeSectionTransfer.Civil3D.Commands))]

namespace BridgeSectionTransfer.Civil3D
{
    public class Commands
    {
        [CommandMethod("ExportDeckSection")]
        public void ExportDeckSection()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            try
            {
                ed.WriteMessage("\n=== BRIDGE DECK SECTION EXPORTER (C# v1.0) ===\n");
                ed.WriteMessage("Exports bridge deck section geometry to XML for CSI Bridge.\n");

                // Step 1: Get export configuration
                var config = GetExportConfiguration(ed);
                if (config == null)
                {
                    ed.WriteMessage("\nExport cancelled by user.\n");
                    return;
                }

                // Step 2: Select polylines
                var exporter = new Civil3DExporter();
                var section = new DeckSection
                {
                    Name = config.SectionName,
                    Station = config.Station,
                    Material = config.Material,
                    ReferencePoint = config.ReferencePoint
                };

                using (Transaction tr = db.TransactionManager.StartTransaction())
                {
                    // Select exterior polyline
                    ed.WriteMessage("\n--- Select EXTERIOR boundary polyline ---\n");
                    var exteriorId = SelectPolyline(ed, "exterior boundary");
                    if (exteriorId == ObjectId.Null)
                    {
                        ed.WriteMessage("\nERROR: No valid exterior boundary selected.\n");
                        return;
                    }

                    Polyline exteriorPoly = tr.GetObject(exteriorId, OpenMode.ForRead) as Polyline;
                    section.ExteriorBoundary = exporter.ExtractPolygon(exteriorPoly, "Exterior", PolygonType.Solid);

                    // Calculate geometric properties
                    var geomCalc = new GeometryCalculator();
                    section.Area = geomCalc.CalculateArea(section.ExteriorBoundary.Points);
                    section.Centroid = geomCalc.CalculateCentroid(section.ExteriorBoundary.Points);

                    ed.WriteMessage($"\nExterior area: {section.Area:F4} m²\n");
                    ed.WriteMessage($"Centroid: ({section.Centroid.X:F3}, {section.Centroid.Y:F3})\n");

                    // Select interior voids
                    int voidIndex = 0;
                    while (true)
                    {
                        PromptKeywordOptions pko = new PromptKeywordOptions("\nAdd interior void? [Yes/No]");
                        pko.Keywords.Add("Yes");
                        pko.Keywords.Add("No");
                        pko.Keywords.Default = "No";
                        PromptResult pkr = ed.GetKeywords(pko);

                        if (pkr.Status != PromptStatus.OK || pkr.StringResult == "No")
                            break;

                        ed.WriteMessage($"\n--- Select VOID #{voidIndex + 1} polyline ---\n");
                        var voidId = SelectPolyline(ed, $"void #{voidIndex + 1}");
                        if (voidId != ObjectId.Null)
                        {
                            Polyline voidPoly = tr.GetObject(voidId, OpenMode.ForRead) as Polyline;
                            var voidPolygon = exporter.ExtractPolygon(voidPoly, $"Void_{voidIndex}", PolygonType.Opening);

                            double voidArea = geomCalc.CalculateArea(voidPolygon.Points);
                            section.Area -= Math.Abs(voidArea);
                            section.InteriorVoids.Add(voidPolygon);

                            ed.WriteMessage($"Void #{voidIndex + 1} added (area: {voidArea:F4} m²)\n");
                            voidIndex++;
                        }
                    }

                    tr.Commit();
                }

                // Step 3: Export to XML
                var serializer = new DeckSectionXmlSerializer();
                serializer.SerializeToFile(section, config.FilePath);

                ed.WriteMessage("\n=== EXPORT COMPLETED SUCCESSFULLY ===\n");
                ed.WriteMessage($"Section Name: {section.Name}\n");
                ed.WriteMessage($"Station: {section.Station:F3} m\n");
                ed.WriteMessage($"Area: {section.Area:F4} m²\n");
                ed.WriteMessage($"Voids: {section.InteriorVoids.Count}\n");
                ed.WriteMessage($"File: {config.FilePath}\n");
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage($"\nERROR: {ex.Message}\n");
            }
        }

        private ObjectId SelectPolyline(Editor ed, string description)
        {
            const int MAX_RETRIES = 5;

            for (int attempt = 1; attempt <= MAX_RETRIES; attempt++)
            {
                PromptEntityOptions peo = new PromptEntityOptions($"\nSelect {description} (attempt {attempt}/{MAX_RETRIES}): ");
                peo.SetRejectMessage("\nMust be a lightweight polyline.");
                peo.AddAllowedClass(typeof(Polyline), true);

                PromptEntityResult per = ed.GetEntity(peo);

                if (per.Status == PromptStatus.OK)
                {
                    return per.ObjectId;
                }
                else if (per.Status == PromptStatus.Cancel)
                {
                    if (attempt < MAX_RETRIES)
                    {
                        PromptKeywordOptions pko = new PromptKeywordOptions("\nRetry selection? [Yes/No]");
                        pko.Keywords.Add("Yes");
                        pko.Keywords.Add("No");
                        pko.Keywords.Default = "Yes";
                        PromptResult pkr = ed.GetKeywords(pko);

                        if (pkr.Status != PromptStatus.OK || pkr.StringResult == "No")
                            break;
                    }
                }
            }

            return ObjectId.Null;
        }

        private ExportConfig GetExportConfiguration(Editor ed)
        {
            // In production, show WPF dialog
            // For now, use simple prompts

            var config = new ExportConfig();

            // File path
            System.Windows.Forms.SaveFileDialog sfd = new System.Windows.Forms.SaveFileDialog();
            sfd.Filter = "XML Files (*.xml)|*.xml";
            sfd.Title = "Save Bridge Deck Section";
            sfd.FileName = "BridgeDeckSection.xml";

            if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return null;

            config.FilePath = sfd.FileName;

            // Section name
            PromptStringOptions pso = new PromptStringOptions("\nEnter section name: ");
            pso.DefaultValue = "DeckSection_01";
            PromptResult pr = ed.GetString(pso);
            config.SectionName = pr.Status == PromptStatus.OK ? pr.StringResult : pso.DefaultValue;

            // Station
            PromptDoubleOptions pdo = new PromptDoubleOptions("\nEnter station (m): ");
            pdo.DefaultValue = 0.0;
            pdo.AllowNegative = false;
            PromptDoubleResult pdr = ed.GetDouble(pdo);
            config.Station = pdr.Status == PromptStatus.OK ? pdr.Value : 0.0;

            // Material properties (use defaults for now)
            config.Material = new MaterialProperties();

            // Reference point
            PromptKeywordOptions pko = new PromptKeywordOptions("\nReference point: [Centerline/Centroid/Custom]");
            pko.Keywords.Add("Centerline");
            pko.Keywords.Add("Centroid");
            pko.Keywords.Add("Custom");
            pko.Keywords.Default = "Centerline";
            PromptResult pkr = ed.GetKeywords(pko);

            config.ReferencePoint = new ReferencePoint();

            if (pkr.Status == PromptStatus.OK)
            {
                switch (pkr.StringResult)
                {
                    case "Centerline":
                        config.ReferencePoint.X = 0.0;
                        config.ReferencePoint.Y = 0.0;
                        config.ReferencePoint.Description = "Centerline at origin";
                        break;

                    case "Centroid":
                        config.ReferencePointUsesCentroid = true;
                        config.ReferencePoint.Description = "Section centroid";
                        break;

                    case "Custom":
                        PromptPointOptions ppo = new PromptPointOptions("\nPick reference point: ");
                        PromptPointResult ppr = ed.GetPoint(ppo);
                        if (ppr.Status == PromptStatus.OK)
                        {
                            config.ReferencePoint.X = ppr.Value.X;
                            config.ReferencePoint.Y = ppr.Value.Y;
                            config.ReferencePoint.Description = "Custom point";
                        }
                        break;
                }
            }

            return config;
        }
    }

    public class Civil3DExporter
    {
        public Polygon ExtractPolygon(Polyline poly, string name, PolygonType type)
        {
            var polygon = new Polygon
            {
                Name = name,
                Type = type,
                Handle = poly.Handle.ToString()
            };

            for (int i = 0; i < poly.NumberOfVertices; i++)
            {
                Point2d pt = poly.GetPoint2dAt(i);
                polygon.Points.Add(new Point2D(pt.X, pt.Y));
            }

            return polygon;
        }
    }

    public class ExportConfig
    {
        public string FilePath { get; set; }
        public string SectionName { get; set; }
        public double Station { get; set; }
        public MaterialProperties Material { get; set; }
        public ReferencePoint ReferencePoint { get; set; }
        public bool ReferencePointUsesCentroid { get; set; }
    }
}
```

### Building the Civil 3D Plugin

**Project Setup:**
1. Create Class Library targeting .NET Framework 4.8 (Civil 3D 2025 requirement)
2. Add NuGet references:
   - `AutoCAD.NET` (v24.0 for Civil 3D 2025)
   - Reference to BridgeSectionTransfer.Core project

**Build Configuration:**
```xml
<!-- BridgeSectionTransfer.Civil3D.csproj -->
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net48</TargetFramework>
    <CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="AutoCAD.NET" Version="24.0.0" />
    <PackageReference Include="AutoCAD.NET.Core" Version="24.0.0" />
    <PackageReference Include="AutoCAD.NET.Model" Version="24.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\BridgeSectionTransfer.Core\BridgeSectionTransfer.Core.csproj" />
  </ItemGroup>
</Project>
```

**Loading into Civil 3D:**
```
NETLOAD command → select BridgeSectionTransfer.Civil3D.dll
Then run: ExportDeckSection
```

---

## 10.5 CSiBridge Importer (C# Plugin)

### CSiBridgeImporter.cs

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CSiBridge1;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

namespace BridgeSectionTransfer.CSiBridge
{
    public class CSiBridgeImporter
    {
        private cOAPI bridgeObject;
        private cSapModel model;
        private cBridgeModeler_1 bridgeModeler;

        public bool Connect()
        {
            try
            {
                cHelper helper = new cHelper();
                bridgeObject = helper.GetObject("CSI.CSiBridge.API.SapObject");

                if (bridgeObject == null)
                {
                    Console.WriteLine("CSiBridge not running or no model open.");
                    return false;
                }

                model = bridgeObject.SapModel;
                bridgeModeler = model.BridgeModeler_1;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error connecting to CSiBridge: {ex.Message}");
                return false;
            }
        }

        public bool ImportSection(DeckSection section, ImportOptions options)
        {
            try
            {
                // Get or create deck section
                string targetSectionName = options.TargetSectionName ?? section.Name;

                // Check if section exists
                int nSections = 0;
                string[] sectionNames = null;
                int[] sectionTypes = null;

                int ret = bridgeModeler.deckSection.GetNameList(ref nSections, ref sectionNames, ref sectionTypes);

                bool sectionExists = sectionNames != null && sectionNames.Contains(targetSectionName);

                if (!sectionExists)
                {
                    // Create new user section (type 10)
                    string newName = targetSectionName;
                    ret = bridgeModeler.deckSection.AddNew(ref newName, 10);
                    if (ret != 0)
                    {
                        Console.WriteLine($"Failed to create section. Return code: {ret}");
                        return false;
                    }
                    targetSectionName = newName;
                }

                // Get current reference point (for verification)
                double xRefOriginal = 0, yRefOriginal = 0;
                bridgeModeler.deckSection.GetReferencePoint(targetSectionName, ref xRefOriginal, ref yRefOriginal);

                // Handle existing polygons
                if (sectionExists && options.ClearExistingVoids)
                {
                    DeleteExistingVoids(targetSectionName);
                }

                // Create/modify exterior polygon
                bool hasExterior = false;
                string exteriorName = "";

                if (sectionExists)
                {
                    // Check for existing exterior
                    var existingPolygons = GetExistingPolygons(targetSectionName);
                    var exterior = existingPolygons.FirstOrDefault(p => p.Type == PolygonType.Solid);
                    if (exterior != null)
                    {
                        hasExterior = true;
                        exteriorName = exterior.Name;
                    }
                }

                if (hasExterior)
                {
                    // Modify existing exterior
                    ret = ModifyPolygon(targetSectionName, exteriorName, section.ExteriorBoundary, options.Material);
                }
                else
                {
                    // Create new exterior
                    ret = CreatePolygon(targetSectionName, "Exterior", section.ExteriorBoundary,
                                       PolygonType.Solid, options.Material);
                }

                if (ret != 0)
                {
                    Console.WriteLine($"Failed to create/modify exterior polygon. Return code: {ret}");
                    return false;
                }

                // Create void polygons
                for (int i = 0; i < section.InteriorVoids.Count; i++)
                {
                    string voidName = $"Void_{i}";
                    ret = CreatePolygon(targetSectionName, voidName, section.InteriorVoids[i],
                                       PolygonType.Opening, "");

                    if (ret != 0)
                    {
                        Console.WriteLine($"Warning: Failed to create void {voidName}. Return code: {ret}");
                    }
                }

                // Set insertion point (reference point)
                if (section.ReferencePoint != null && options.SetReferencePoint)
                {
                    ret = bridgeModeler.deckSection.User.SetInsertionPoint(
                        targetSectionName,
                        section.ReferencePoint.X,
                        section.ReferencePoint.Y
                    );

                    if (ret != 0)
                    {
                        Console.WriteLine($"Warning: Failed to set insertion point. Return code: {ret}");
                    }
                }

                // Verify reference point unchanged (unless we explicitly set it)
                if (!options.SetReferencePoint)
                {
                    double xRefFinal = 0, yRefFinal = 0;
                    bridgeModeler.deckSection.GetReferencePoint(targetSectionName, ref xRefFinal, ref yRefFinal);

                    if (Math.Abs(xRefFinal - xRefOriginal) > 0.001 || Math.Abs(yRefFinal - yRefOriginal) > 0.001)
                    {
                        Console.WriteLine($"WARNING: Reference point changed during operation!");
                        Console.WriteLine($"Original: ({xRefOriginal:F3}, {yRefOriginal:F3})");
                        Console.WriteLine($"Final: ({xRefFinal:F3}, {yRefFinal:F3})");
                    }
                }

                Console.WriteLine($"Successfully imported section '{targetSectionName}'");
                Console.WriteLine($"  Exterior points: {section.ExteriorBoundary.Points.Count}");
                Console.WriteLine($"  Interior voids: {section.InteriorVoids.Count}");

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing section: {ex.Message}");
                return false;
            }
        }

        private int CreatePolygon(string sectionName, string polygonName, Polygon polygon,
                                 PolygonType type, string material)
        {
            int nPts = polygon.Points.Count;
            double[] xCoords = new double[nPts];
            double[] yCoords = new double[nPts];
            double[] radii = new double[nPts];

            for (int i = 0; i < nPts; i++)
            {
                xCoords[i] = polygon.Points[i].X;
                yCoords[i] = polygon.Points[i].Y;
                radii[i] = 0.0;  // Straight segments
            }

            return bridgeModeler.deckSection.User.AddNewPolygon(
                sectionName,
                polygonName,
                (int)type,
                material,
                nPts,
                ref xCoords,
                ref yCoords,
                ref radii
            );
        }

        private int ModifyPolygon(string sectionName, string polygonName, Polygon polygon, string material)
        {
            int nPts = polygon.Points.Count;
            double[] xCoords = new double[nPts];
            double[] yCoords = new double[nPts];
            double[] radii = new double[nPts];

            for (int i = 0; i < nPts; i++)
            {
                xCoords[i] = polygon.Points[i].X;
                yCoords[i] = polygon.Points[i].Y;
                radii[i] = 0.0;
            }

            // If material is empty, preserve existing material
            if (string.IsNullOrEmpty(material))
            {
                // Get existing material
                int existingType = 0;
                string existingMaterial = "";
                int existingNpts = 0;
                double[] existingX = null;
                double[] existingY = null;
                double[] existingR = null;

                int ret = bridgeModeler.deckSection.User.GetPolygon(
                    sectionName, polygonName,
                    ref existingType, ref existingMaterial, ref existingNpts,
                    ref existingX, ref existingY, ref existingR
                );

                if (ret == 0)
                {
                    material = existingMaterial;
                }
            }

            return bridgeModeler.deckSection.User.SetPolygon(
                sectionName,
                polygonName,
                material,
                nPts,
                ref xCoords,
                ref yCoords,
                ref radii
            );
        }

        private void DeleteExistingVoids(string sectionName)
        {
            var polygons = GetExistingPolygons(sectionName);

            foreach (var poly in polygons.Where(p => p.Type == PolygonType.Opening))
            {
                bridgeModeler.deckSection.User.DeletePolygon(sectionName, poly.Name);
            }
        }

        private List<PolygonInfo> GetExistingPolygons(string sectionName)
        {
            var result = new List<PolygonInfo>();

            int nPolygons = 0;
            string[] polygonNames = null;
            int[] polygonTypes = null;
            int[] polygonNpts = null;

            int ret = bridgeModeler.deckSection.User.GetPolygonNameList(
                sectionName,
                ref nPolygons,
                ref polygonNames,
                ref polygonTypes,
                ref polygonNpts
            );

            if (ret == 0 && polygonNames != null)
            {
                for (int i = 0; i < nPolygons; i++)
                {
                    result.Add(new PolygonInfo
                    {
                        Name = polygonNames[i],
                        Type = (PolygonType)polygonTypes[i],
                        PointCount = polygonNpts[i]
                    });
                }
            }

            return result;
        }

        private class PolygonInfo
        {
            public string Name { get; set; }
            public PolygonType Type { get; set; }
            public int PointCount { get; set; }
        }
    }

    public class ImportOptions
    {
        public string TargetSectionName { get; set; }
        public bool ClearExistingVoids { get; set; } = true;
        public bool SetReferencePoint { get; set; } = true;
        public string Material { get; set; } = "";
    }
}
```

### Console Application Example

```csharp
// Program.cs - Console app for testing
using System;
using BridgeSectionTransfer.Core.Services;
using BridgeSectionTransfer.CSiBridge;

namespace BridgeSectionTransfer.Console
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("=== Bridge Section Importer ===\n");

            if (args.Length == 0)
            {
                System.Console.WriteLine("Usage: BridgeSectionTransfer.exe <xmlfile>");
                return;
            }

            string xmlPath = args[0];

            if (!System.IO.File.Exists(xmlPath))
            {
                System.Console.WriteLine($"ERROR: File not found: {xmlPath}");
                return;
            }

            try
            {
                // Load XML
                System.Console.WriteLine($"Loading XML: {xmlPath}");
                var serializer = new DeckSectionXmlSerializer();
                var section = serializer.DeserializeFromFile(xmlPath);

                System.Console.WriteLine($"Section: {section.Name}");
                System.Console.WriteLine($"Station: {section.Station:F3} m");
                System.Console.WriteLine($"Area: {section.Area:F4} m²");
                System.Console.WriteLine($"Voids: {section.InteriorVoids.Count}");

                // Connect to CSiBridge
                System.Console.WriteLine("\nConnecting to CSiBridge...");
                var importer = new CSiBridgeImporter();

                if (!importer.Connect())
                {
                    System.Console.WriteLine("ERROR: Failed to connect to CSiBridge.");
                    System.Console.WriteLine("Make sure CSiBridge is running with a model open.");
                    return;
                }

                System.Console.WriteLine("Connected successfully.");

                // Import section
                System.Console.WriteLine("\nImporting section...");
                var options = new ImportOptions
                {
                    TargetSectionName = section.Name,
                    ClearExistingVoids = true,
                    SetReferencePoint = true,
                    Material = ""  // Use default
                };

                bool success = importer.ImportSection(section, options);

                if (success)
                {
                    System.Console.WriteLine("\n=== IMPORT COMPLETED SUCCESSFULLY ===");
                }
                else
                {
                    System.Console.WriteLine("\n=== IMPORT FAILED ===");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"\nERROR: {ex.Message}");
                System.Console.WriteLine(ex.StackTrace);
            }

            System.Console.WriteLine("\nPress any key to exit...");
            System.Console.ReadKey();
        }
    }
}
```

---

## 10.6 WPF User Interface

### MainWindow.xaml

```xml
<Window x:Class="BridgeSectionTransfer.UI.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="Bridge Section Transfer" Height="600" Width="900"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="*"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>

        <!-- Header -->
        <Border Grid.Row="0" Background="#FF2D2D30" Padding="15">
            <StackPanel>
                <TextBlock Text="Bridge Section Transfer Tool"
                          FontSize="24" FontWeight="Bold"
                          Foreground="White"/>
                <TextBlock Text="Civil 3D to CSiBridge Cross-Section Exporter/Importer"
                          FontSize="12" Foreground="#FFCCCCCC"
                          Margin="0,5,0,0"/>
            </StackPanel>
        </Border>

        <!-- Main Content -->
        <TabControl Grid.Row="1" Margin="10">
            <!-- Export Tab -->
            <TabItem Header="Export from Civil 3D">
                <Grid Margin="10">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <GroupBox Header="Export Configuration" Grid.Row="0">
                        <StackPanel Margin="10">
                            <Label Content="Section Name:"/>
                            <TextBox x:Name="txtExportSectionName" Text="DeckSection_01" Margin="0,0,0,10"/>

                            <Label Content="Station (m):"/>
                            <TextBox x:Name="txtExportStation" Text="0.0" Margin="0,0,0,10"/>

                            <Label Content="Reference Point:"/>
                            <ComboBox x:Name="cboReferencePoint" SelectedIndex="0" Margin="0,0,0,10">
                                <ComboBoxItem Content="Centerline at Origin (0, 0)"/>
                                <ComboBoxItem Content="Section Centroid (calculated)"/>
                                <ComboBoxItem Content="Custom Point..."/>
                            </ComboBox>

                            <Label Content="Material Properties:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition/>
                                    <ColumnDefinition/>
                                    <ColumnDefinition/>
                                </Grid.ColumnDefinitions>
                                <StackPanel Grid.Column="0" Margin="0,0,5,0">
                                    <Label Content="Strength (MPa):"/>
                                    <TextBox x:Name="txtConcreteStrength" Text="30.0"/>
                                </StackPanel>
                                <StackPanel Grid.Column="1" Margin="5,0,5,0">
                                    <Label Content="Density (kg/m³):"/>
                                    <TextBox x:Name="txtDensity" Text="2400.0"/>
                                </StackPanel>
                                <StackPanel Grid.Column="2" Margin="5,0,0,0">
                                    <Label Content="E (MPa):"/>
                                    <TextBox x:Name="txtModulus" Text="30000.0"/>
                                </StackPanel>
                            </Grid>
                        </StackPanel>
                    </GroupBox>

                    <GroupBox Header="Export Log" Grid.Row="1" Margin="0,10,0,0">
                        <ScrollViewer>
                            <TextBox x:Name="txtExportLog" IsReadOnly="True"
                                    TextWrapping="Wrap" FontFamily="Consolas"
                                    Background="#FF1E1E1E" Foreground="#FFDCDCDC"/>
                        </ScrollViewer>
                    </GroupBox>

                    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
                        <Button Content="Export to XML" Width="120" Height="35"
                               Click="btnExport_Click" Margin="0,0,10,0"/>
                        <Button Content="Clear Log" Width="120" Height="35"
                               Click="btnClearExportLog_Click"/>
                    </StackPanel>
                </Grid>
            </TabItem>

            <!-- Import Tab -->
            <TabItem Header="Import to CSiBridge">
                <Grid Margin="10">
                    <Grid.RowDefinitions>
                        <RowDefinition Height="Auto"/>
                        <RowDefinition Height="*"/>
                        <RowDefinition Height="Auto"/>
                    </Grid.RowDefinitions>

                    <GroupBox Header="Import Configuration" Grid.Row="0">
                        <StackPanel Margin="10">
                            <Label Content="XML File:"/>
                            <Grid>
                                <Grid.ColumnDefinitions>
                                    <ColumnDefinition/>
                                    <ColumnDefinition Width="Auto"/>
                                </Grid.ColumnDefinitions>
                                <TextBox x:Name="txtXmlFilePath" IsReadOnly="True"
                                        VerticalContentAlignment="Center"/>
                                <Button Grid.Column="1" Content="Browse..." Width="80"
                                       Margin="5,0,0,0" Click="btnBrowseXml_Click"/>
                            </Grid>

                            <CheckBox x:Name="chkClearVoids" Content="Clear existing voids before import"
                                     IsChecked="True" Margin="0,10,0,0"/>
                            <CheckBox x:Name="chkSetReferencePoint" Content="Set reference point from XML"
                                     IsChecked="True" Margin="0,5,0,0"/>
                        </StackPanel>
                    </GroupBox>

                    <GroupBox Header="Import Log" Grid.Row="1" Margin="0,10,0,0">
                        <ScrollViewer>
                            <TextBox x:Name="txtImportLog" IsReadOnly="True"
                                    TextWrapping="Wrap" FontFamily="Consolas"
                                    Background="#FF1E1E1E" Foreground="#FFDCDCDC"/>
                        </ScrollViewer>
                    </GroupBox>

                    <StackPanel Grid.Row="2" Orientation="Horizontal" HorizontalAlignment="Right" Margin="0,10,0,0">
                        <Button Content="Import to CSiBridge" Width="140" Height="35"
                               Click="btnImport_Click" Margin="0,0,10,0"/>
                        <Button Content="Clear Log" Width="120" Height="35"
                               Click="btnClearImportLog_Click"/>
                    </StackPanel>
                </Grid>
            </TabItem>
        </TabControl>

        <!-- Status Bar -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <TextBlock x:Name="txtStatus" Text="Ready"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

---

## 10.7 Deployment Guide

### Building the Solution

1. **Prerequisites:**
   - Visual Studio 2022
   - .NET 8 SDK
   - .NET Framework 4.8 SDK
   - AutoCAD/Civil 3D 2025 ObjectARX SDK
   - CSiBridge v25 API libraries

2. **Build Steps:**
   ```powershell
   # Restore NuGet packages
   dotnet restore

   # Build all projects
   dotnet build --configuration Release

   # Publish standalone console app
   dotnet publish BridgeSectionTransfer.Console -c Release -r win-x64 --self-contained
   ```

3. **Output Files:**
   - `BridgeSectionTransfer.Civil3D.dll` → Load into Civil 3D
   - `BridgeSectionTransfer.CSiBridge.dll` → Reference for CSiBridge plugins
   - `BridgeSectionTransfer.UI.exe` → Standalone WPF application
   - `BridgeSectionTransfer.Console.exe` → Command-line tool

---

## Summary

This comprehensive C# implementation provides:

✅ **Type-safe, modern code** replacing VBA
✅ **Reusable core library** for geometry and XML handling
✅ **Civil 3D plugin** for direct export from drawings
✅ **CSiBridge API integration** with full polygon management
✅ **Reference point support** via insertion point API
✅ **Professional UI** with WPF
✅ **Command-line tools** for automation

**Next Steps:**
1. Test with actual Civil 3D/CSiBridge installations
2. Add error handling and logging (NLog, Serilog)
3. Implement unit tests (xUnit, NUnit)
4. Create installer (WiX Toolset)
5. Add configuration file support (JSON)
