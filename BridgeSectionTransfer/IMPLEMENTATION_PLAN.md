# Bridge Section Transfer - Implementation Plan

## Project Overview
Transfer bridge deck sections from Civil 3D to CSiBridge, including geometry and reference lines for Section Designer.

## Current Implementation Status

### ✅ Completed Features
1. **Core Data Models**
   - `DeckSection` - Main section container
   - `Polygon` - Exterior boundary and voids
   - `Point2D` - 2D coordinates
   - `ReferencePoint` - Section reference/insertion point
   - `MaterialProperties` - Material data

2. **Civil 3D Export (`ExportDeckSection` command)**
   - Select multiple polylines (exterior + voids)
   - Auto-identify exterior (largest area) vs voids
   - Ensure proper winding order (CW for exterior, CCW for voids)
   - Calculate area and centroid
   - User-defined section name and station
   - Reference point options: Centerline/Centroid/Pick
   - Export to JSON format

3. **CSiBridge Import (`CSiBridgeImporter`)**
   - Connect to running CSiBridge instance via API
   - Import deck section polygons (exterior + voids)
   - Set insertion/reference point
   - Uses `DeckSection.User.AddNewPolygon()` API

---

## 🚧 Reference Lines and Centerlines Implementation

### Background
CSiBridge Section Designer uses **reference lines** (non-structural guide geometry) separate from structural shapes. These include:
- **Web/slab centerlines**
- **Girder cut lines**
- **Layout guides**

Reference lines are drawn separately in Section Designer because they:
- Do NOT contribute to section properties (Area, I, J, etc.)
- Serve as visual/geometric guides only
- Are created with different API methods than structural shapes

### API Methods Available
From `cPropFrameSDShape` interface:

```csharp
// Reference Line Methods
SetRefLine(string name, ref string shapeName, double x1, double y1, double x2, double y2)
GetRefLine(string name, string shapeName, ref double x1, ref double y1, ref double x2, ref double y2)

// Reference Circle Methods (for circular references)
SetRefCircle(string name, ref string shapeName, double xCenter, double yCenter, double diameter)
GetRefCircle(string name, string shapeName, ref double xCenter, ref double yCenter, ref double diameter)
```

### Lane Centerlines (Bridge Layout)
From `cBMLane` interface:
```csharp
GetLaneCenterlinePoints() - Gets centerline points for a lane
```

---

## Implementation Tasks

### A. Data Model Extensions
- [ ] Create `ReferenceLine` class
  ```csharp
  public class ReferenceLine
  {
      public string Name { get; set; }
      public Point2D StartPoint { get; set; }
      public Point2D EndPoint { get; set; }
      public ReferenceLineType Type { get; set; } // Centerline, CutLine, Guide
      public string Description { get; set; }
  }

  public enum ReferenceLineType
  {
      Centerline,
      CutLine,
      Guide
  }
  ```

- [ ] Create `ReferenceCircle` class
  ```csharp
  public class ReferenceCircle
  {
      public string Name { get; set; }
      public Point2D Center { get; set; }
      public double Diameter { get; set; }
      public string Description { get; set; }
  }
  ```

- [ ] Add to `DeckSection` class:
  ```csharp
  public List<ReferenceLine> ReferenceLines { get; set; } = new();
  public List<ReferenceCircle> ReferenceCircles { get; set; } = new();
  ```

### B. Civil 3D Export Enhancement
- [ ] Add method to extract reference lines from Civil 3D:
  - From specific layer (e.g., "REF-LINES", "CENTERLINES")
  - From LINE objects
  - From user selection after main section

- [ ] Add to `ExportDeckSection` command:
  1. After selecting section polygons, prompt for reference lines
  2. Allow user to select line objects
  3. Extract start/end points from each line
  4. Classify by name or layer (centerline vs cut line)
  5. Include in JSON export

- [ ] Implementation example:
  ```csharp
  // After section export, prompt for reference lines
  PromptSelectionOptions refLineOpts = new PromptSelectionOptions
  {
      MessageForAdding = "\nSelect reference lines (centerlines, cut lines) or press Enter to skip: "
  };

  TypedValue[] lineFilter = new TypedValue[]
  {
      new TypedValue((int)DxfCode.Start, "LINE")
  };
  SelectionFilter refLineFilter = new SelectionFilter(lineFilter);

  PromptSelectionResult refLinePsr = ed.GetSelection(refLineOpts, refLineFilter);

  if (refLinePsr.Status == PromptStatus.OK)
  {
      foreach (ObjectId lineId in refLinePsr.Value.GetObjectIds())
      {
          Line line = tr.GetObject(lineId, OpenMode.ForRead) as Line;
          if (line != null)
          {
              var refLine = new ReferenceLine
              {
                  Name = DetermineRefLineName(line), // Based on layer or user input
                  StartPoint = new Point2D(line.StartPoint.X, line.StartPoint.Y),
                  EndPoint = new Point2D(line.EndPoint.X, line.EndPoint.Y),
                  Type = DetermineRefLineType(line) // Based on layer name
              };
              section.ReferenceLines.Add(refLine);
          }
      }
  }
  ```

### C. CSiBridge Import Enhancement
- [ ] Update `CSiBridgeImporter.ImportSection()` to handle reference lines

- [ ] Add after polygon import:
  ```csharp
  // Import reference lines to Section Designer
  if (section.ReferenceLines.Count > 0)
  {
      Console.WriteLine($"\nImporting {section.ReferenceLines.Count} reference lines...");

      var sdShape = _model.PropFrame.SDShape;

      foreach (var refLine in section.ReferenceLines)
      {
          string refLineName = "";
          int ret = sdShape.SetRefLine(
              sectionName,           // Frame section name
              ref refLineName,       // Shape name (output)
              refLine.StartPoint.X,  // X1
              refLine.StartPoint.Y,  // Y1
              refLine.EndPoint.X,    // X2
              refLine.EndPoint.Y     // Y2
          );

          if (ret != 0)
          {
              Console.WriteLine($"WARNING: Failed to create reference line '{refLine.Name}' (code {ret})");
          }
          else
          {
              Console.WriteLine($"Reference line '{refLine.Name}' created: {refLineName}");
          }
      }
  }

  // Import reference circles
  if (section.ReferenceCircles.Count > 0)
  {
      Console.WriteLine($"\nImporting {section.ReferenceCircles.Count} reference circles...");

      var sdShape = _model.PropFrame.SDShape;

      foreach (var refCircle in section.ReferenceCircles)
      {
          string refCircleName = "";
          int ret = sdShape.SetRefCircle(
              sectionName,
              ref refCircleName,
              refCircle.Center.X,
              refCircle.Center.Y,
              refCircle.Diameter
          );

          if (ret != 0)
          {
              Console.WriteLine($"WARNING: Failed to create reference circle '{refCircle.Name}' (code {ret})");
          }
          else
          {
              Console.WriteLine($"Reference circle '{refCircle.Name}' created: {refCircleName}");
          }
      }
  }
  ```

### D. User Options
- [ ] Add to `ImportOptions` class:
  ```csharp
  public bool IncludeReferenceLines { get; set; } = true;
  public string ReferenceLineLayerFilter { get; set; } = "REF,CENTERLINE,CL";
  ```

- [ ] Add naming conventions:
  - Centerlines: "CL_Web", "CL_Slab", etc.
  - Cut lines: "Cut_Girder", "Cut_Deck", etc.

---

## Testing Checklist

### Reference Lines Implementation
- [ ] Export section with single centerline
- [ ] Export section with multiple reference lines (web + slab centerlines)
- [ ] Export section with reference circles
- [ ] Import reference lines to CSiBridge Section Designer
- [ ] Verify reference lines appear in Section Designer
- [ ] Verify reference lines don't affect section properties
- [ ] Test with complex section (multiple voids + multiple ref lines)
- [ ] Test layer-based automatic classification

---

## Usage Workflow

### In Civil 3D:
1. Draw section polygons (exterior + voids)
2. Draw reference lines on separate layer (e.g., "CENTERLINES")
   - Web centerline (vertical line at X=0)
   - Slab centerlines
   - Girder cut lines
3. Run `ExportDeckSection` command
4. Select all section polygons
5. Select reference lines when prompted
6. Save to JSON

### In CSiBridge:
1. Open CSiBridge model
2. Run import tool
3. Load JSON file
4. Section Designer will show:
   - Structural shapes (polygons)
   - Reference lines (guides)
5. Use reference lines for dimensioning and layout

---

## Notes
- Reference lines are non-structural - they do NOT contribute to:
  - Section area
  - Moment of inertia
  - Section modulus
  - Weight/mass
- Reference lines are visual guides only
- Must be created BEFORE or AFTER structural shapes (order may matter in Section Designer)
- Centerlines typically placed at X=0 or other significant locations
