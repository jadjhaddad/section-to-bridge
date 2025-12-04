# C# Implementation Guide - Quick Reference

**Goal:** Convert VBA to C# with JSON format and multi-select void selection

---

## 📋 Implementation Checklist

### Phase 1: Setup Project Structure (1-2 hours)

- [x] **1.1** Create Visual Studio Solution
  ```
  File → New → Solution
  Name: BridgeSectionTransfer
  ```

- [x] **1.2** Create Core Class Library
  ```
  Add Project → Class Library (.NET Standard 2.0)
  Name: BridgeSectionTransfer.Core
  Target: netstandard2.0 (for compatibility with .NET Framework 4.8)
  Add NuGet: System.Text.Json 8.0.0
  ```

- [x] **1.3** Create Civil 3D Plugin Project
  ```
  Add Project → Class Library (.NET Framework 4.8)
  Name: BridgeSectionTransfer.Civil3D
  Add NuGet: AutoCAD.NET v24.0
  ```

- [x] **1.4** Create CSiBridge Plugin Project
  ```
  Add Project → Class Library (.NET Framework 4.8)
  Name: BridgeSectionTransfer.CSiBridge
  Add COM Reference: CSiBridge1.dll (from CSiBridge install folder)
  ```

- [x] **1.5** Create Console Application (Optional)
  ```
  Add Project → Console App (.NET 8)
  Name: BridgeSectionTransfer.Console
  ```

---

### Phase 2: Core Library - Models (30 minutes)

📂 **Location:** `BridgeSectionTransfer.Core/Models/`

- [x] **2.1** Create `Point2D.cs`
  ```csharp
  public class Point2D
  {
      public double X { get; set; }
      public double Y { get; set; }
      public Point2D(double x, double y) { X = x; Y = y; }
  }
  ```

- [x] **2.2** Create `Polygon.cs`
  ```csharp
  public class Polygon
  {
      public string Name { get; set; }
      public PolygonType Type { get; set; }
      public List<Point2D> Points { get; set; } = new();
      public string Handle { get; set; }
  }

  public enum PolygonType { Solid = 1, Opening = 2 }
  ```

- [x] **2.3** Create `ReferencePoint.cs`
  ```csharp
  public class ReferencePoint
  {
      public double X { get; set; }
      public double Y { get; set; }
      public string Description { get; set; }
  }
  ```

- [x] **2.4** Create `MaterialProperties.cs`
  ```csharp
  public class MaterialProperties
  {
      public double ConcreteStrength { get; set; } = 30.0;
      public double Density { get; set; } = 2400.0;
      public double ElasticModulus { get; set; } = 30000.0;
  }
  ```

- [x] **2.5** Create `DeckSection.cs`
  ```csharp
  public class DeckSection
  {
      public string Name { get; set; }
      public double Station { get; set; }
      public double Area { get; set; }
      public Point2D Centroid { get; set; }
      public ReferencePoint ReferencePoint { get; set; }
      public MaterialProperties Material { get; set; }
      public Polygon ExteriorBoundary { get; set; }
      public List<Polygon> InteriorVoids { get; set; } = new();
  }
  ```

---

### Phase 3: Core Library - JSON Serialization (45 minutes)

📂 **Location:** `BridgeSectionTransfer.Core/Services/`

- [x] **3.1** Create JSON DTOs in `JsonDtos.cs`
  ```csharp
  public class BridgeDeckSectionsData
  {
      public ExportInfo ExportInfo { get; set; }
      public List<DeckSectionDto> Sections { get; set; }
  }

  public class ExportInfo
  {
      public DateTime Date { get; set; }
      public string Tool { get; set; }
      public string Version { get; set; }
      public string Units { get; set; }
      public string CoordinateSystem { get; set; }
  }

  public class DeckSectionDto
  {
      public string Name { get; set; }
      public double Station { get; set; }
      public double Area { get; set; }
      public Point2DDto Centroid { get; set; }
      public ReferencePointDto ReferencePoint { get; set; }
      public MaterialPropertiesDto Material { get; set; }
      public List<Point2DDto> ExteriorBoundary { get; set; }
      public List<VoidDto> InteriorVoids { get; set; }
  }

  public class Point2DDto
  {
      public double X { get; set; }
      public double Y { get; set; }
  }

  public class VoidDto
  {
      public string Name { get; set; }
      public List<Point2DDto> Points { get; set; }
  }

  // Add ReferencePointDto and MaterialPropertiesDto similarly
  ```

- [x] **3.2** Create `DeckSectionJsonSerializer.cs` with mapping methods
  - See `DOCUMENTATION_PART6_JSON_FORMAT.md` for complete code
  - Implement `SerializeToFile()`
  - Implement `DeserializeFromFile()`
  - Add mapping methods `MapToDto()` and `MapFromDto()`

- [x] **3.3** Configure JSON options
  ```csharp
  private static readonly JsonSerializerOptions Options = new()
  {
      WriteIndented = true,
      PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
      DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
  };
  ```

---

### Phase 4: Core Library - Geometry Calculator (30 minutes)

📂 **Location:** `BridgeSectionTransfer.Core/Services/`

- [x] **4.1** Create `GeometryCalculator.cs`
  - Implement `CalculateArea()` - Shoelace formula
  - Implement `CalculateCentroid()` - Polygon centroid formula
  - Implement `EnsureClockwise()` - Direction validation

See `DOCUMENTATION_PART4_UIUX_REFLINES.md` section 10.3 for complete code.

---

### Phase 5: Civil 3D Plugin - Export Command (2-3 hours)

📂 **Location:** `BridgeSectionTransfer.Civil3D/`

#### 🔑 KEY CHANGE: Multi-Select Voids

- [x] **5.1** Create `Commands.cs` with `[CommandMethod("ExportDeckSection")]`

- [x] **5.2** Implement **Multi-Select for Voids**
  ```csharp
  [CommandMethod("ExportDeckSection")]
  public void ExportDeckSection()
  {
      Document doc = Application.DocumentManager.MdiActiveDocument;
      Editor ed = doc.Editor;

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
                  double area = geomCalc.CalculateArea(polygon.Points);

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

      // Step 4: Rest are voids
      for (int i = 1; i < polylines.Count; i++)
      {
          polylines[i].Polygon.Name = $"Void_{i - 1}";
          polylines[i].Polygon.Type = PolygonType.Opening;
          section.InteriorVoids.Add(polylines[i].Polygon);
      }

      ed.WriteMessage($"\nIdentified: 1 exterior + {section.InteriorVoids.Count} voids\n");

      // Continue with area calculation and export...
  }

  private class PolylineData
  {
      public ObjectId Id { get; set; }
      public Polygon Polygon { get; set; }
      public double Area { get; set; }
  }
  ```

- [x] **5.3** Implement polyline extraction
  ```csharp
  private Polygon ExtractPolygon(Polyline poly, string name)
  {
      var polygon = new Polygon { Name = name, Handle = poly.Handle.ToString() };

      for (int i = 0; i < poly.NumberOfVertices; i++)
      {
          Point2d pt = poly.GetPoint2dAt(i);
          polygon.Points.Add(new Point2D(pt.X, pt.Y));
      }

      return polygon;
  }
  ```

- [x] **5.4** Implement reference point selection
  ```csharp
  private ReferencePoint GetReferencePoint(Editor ed, Point2D centroid)
  {
      PromptKeywordOptions pko = new PromptKeywordOptions(
          "\nReference point [Centerline/Centroid/Pick]: "
      );
      pko.Keywords.Add("Centerline");
      pko.Keywords.Add("Centroid");
      pko.Keywords.Add("Pick");
      pko.Keywords.Default = "Centerline";

      PromptResult pr = ed.GetKeywords(pko);

      if (pr.Status != PromptStatus.OK)
          return new ReferencePoint { X = 0, Y = 0, Description = "Default" };

      switch (pr.StringResult)
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
  ```

- [x] **5.5** Export to JSON
  ```csharp
  // Get save file path
  SaveFileDialog sfd = new SaveFileDialog
  {
      Filter = "JSON Files (*.json)|*.json",
      Title = "Save Bridge Deck Section",
      FileName = "BridgeDeckSection.json"
  };

  if (sfd.ShowDialog() != DialogResult.OK)
      return;

  // Serialize and save
  var serializer = new DeckSectionJsonSerializer();
  serializer.SerializeToFile(section, sfd.FileName);

  ed.WriteMessage($"\n=== EXPORT COMPLETED ===\n");
  ed.WriteMessage($"File: {sfd.FileName}\n");
  ```

---

### Phase 6: CSiBridge Plugin - Import (2 hours)

📂 **Location:** `BridgeSectionTransfer.CSiBridge/`

- [x] **6.1** Create `CSiBridgeImporter.cs`
  - Implement `Connect()` - attach to running CSiBridge
  - Implement `ImportSection()` - main import logic
  - Implement `CreatePolygon()` - add new polygon
  - Implement `ModifyPolygon()` - modify existing polygon

- [x] **6.2** Key methods
  ```csharp
  public bool Connect()
  {
      cHelper helper = new cHelper();
      bridgeObject = helper.GetObject("CSI.CSiBridge.API.SapObject");

      if (bridgeObject == null)
          return false;

      model = bridgeObject.SapModel;
      bridgeModeler = model.BridgeModeler_1;
      return true;
  }

  public bool ImportSection(DeckSection section, ImportOptions options)
  {
      // Create or get section
      // Set exterior polygon via SetPolygon or AddNewPolygon
      // Create void polygons
      // Apply reference point via SetInsertionPoint
      // Verify
  }
  ```

- [x] **6.3** Apply reference point
  ```csharp
  if (section.ReferencePoint != null && options.SetReferencePoint)
  {
      int ret = bridgeModeler.deckSection.User.SetInsertionPoint(
          targetSectionName,
          section.ReferencePoint.X,
          section.ReferencePoint.Y
      );

      if (ret != 0)
          Console.WriteLine($"Warning: Failed to set reference point (code {ret})");
  }
  ```

See `DOCUMENTATION_PART5_CSHARP_IMPLEMENTATION.md` for complete CSiBridge importer code.

---

### Phase 7: Console Application (1 hour)

📂 **Location:** `BridgeSectionTransfer.Console/`

- [x] **7.1** Create `Program.cs`
  ```csharp
  static void Main(string[] args)
  {
      if (args.Length == 0)
      {
          Console.WriteLine("Usage: BridgeSectionTransfer.exe <jsonfile>");
          return;
      }

      string jsonPath = args[0];

      // Load JSON
      var serializer = new DeckSectionJsonSerializer();
      var section = serializer.DeserializeFromFile(jsonPath);

      Console.WriteLine($"Loaded: {section.Name}");
      Console.WriteLine($"Voids: {section.InteriorVoids.Count}");

      // Connect to CSiBridge
      var importer = new CSiBridgeImporter();
      if (!importer.Connect())
      {
          Console.WriteLine("ERROR: CSiBridge not running");
          return;
      }

      // Import
      var options = new ImportOptions
      {
          SetReferencePoint = true,
          ClearExistingVoids = true
      };

      bool success = importer.ImportSection(section, options);
      Console.WriteLine(success ? "SUCCESS" : "FAILED");
  }
  ```

---

### Phase 8: Testing (2-3 hours)

#### Test Export (Civil 3D)

- [ ] **8.1** Load plugin in Civil 3D
  ```
  Open Civil 3D
  Type: NETLOAD
  Select: BridgeSectionTransfer.Civil3D.dll
  ```

- [ ] **8.2** Run export command
  ```
  Type: ExportDeckSection
  Select: All polylines at once (exterior + voids)
  Choose: Reference point option
  Save: JSON file
  ```

- [ ] **8.3** Verify JSON output
  ```json
  {
    "exportInfo": { ... },
    "sections": [
      {
        "name": "DeckSection_01",
        "exteriorBoundary": [ ... ],
        "interiorVoids": [ ... ],
        "referencePoint": { ... }
      }
    ]
  }
  ```

#### Test Import (CSiBridge)

- [ ] **8.4** Open CSiBridge with bridge model

- [ ] **8.5** Run console importer
  ```powershell
  cd BridgeSectionTransfer.Console\bin\Release\net8.0
  .\BridgeSectionTransfer.Console.exe "C:\path\to\section.json"
  ```

- [ ] **8.6** Verify in CSiBridge
  - Check deck section exists
  - Verify exterior polygon
  - Verify void polygons
  - Check reference point location

---

## 🎯 Quick Start Commands

### Build Everything
```powershell
dotnet restore
dotnet build --configuration Release
```

### Test Export
```
1. Open Civil 3D
2. NETLOAD → BridgeSectionTransfer.Civil3D.dll
3. ExportDeckSection
4. Select all polylines (multi-select)
5. Save JSON
```

### Test Import
```powershell
BridgeSectionTransfer.Console.exe path\to\section.json
```

---

## 📝 Key Differences from VBA

| Feature | VBA | C# |
|---------|-----|-----|
| **Void Selection** | One-by-one with dialog | **Multi-select all at once** |
| **Data Format** | XML | **JSON (recommended)** |
| **Exterior Detection** | Manual selection | **Auto-detect largest area** |
| **Reference Point** | Not implemented | **Full support via SetInsertionPoint** |
| **Excel Step** | Required | **Eliminated - direct JSON→API** |
| **Error Handling** | Basic | **Comprehensive validation** |

---

## ⚡ Time Estimates

- **Phase 1-2:** Setup & Models → 2 hours
- **Phase 3-4:** JSON & Geometry → 1.5 hours
- **Phase 5:** Civil 3D Export → 3 hours
- **Phase 6:** CSiBridge Import → 2 hours
- **Phase 7:** Console App → 1 hour
- **Phase 8:** Testing → 2 hours

**Total: ~11-12 hours** for complete C# implementation

---

## 🔗 Reference Documents

- **Complete C# Code:** `DOCUMENTATION_PART5_CSHARP_IMPLEMENTATION.md`
- **JSON Format:** `DOCUMENTATION_PART6_JSON_FORMAT.md`
- **API Reference:** `DOCUMENTATION_PART3_API_IMPROVEMENTS.md`
- **Reference Lines:** `DOCUMENTATION_PART4_UIUX_REFLINES.md`

---

## ✅ Success Criteria

Your implementation is complete when:

- ✅ Can select all polylines at once (multi-select) in Civil 3D
- ✅ Exports to clean JSON format
- ✅ Auto-detects exterior boundary (largest area)
- ✅ Captures reference point with 3 options
- ✅ Imports directly to CSiBridge (no Excel)
- ✅ Sets reference point via SetInsertionPoint
- ✅ Console app works for automation

**Next Step:** Start with Phase 1 - Create the solution structure! 🚀
