# API Reference - Bridge Section Transfer

**Complete reference for all API endpoints used in the project**

**Last Verified:** 2025-12-04
**AutoCAD/Civil 3D Version:** 2025 (API v25.0)
**CSiBridge Version:** v25 (API v1.0)

---

## Table of Contents

1. [AutoCAD .NET API](#autocad-net-api)
2. [CSiBridge COM API](#csibridge-com-api)
3. [System APIs](#system-apis)
4. [Quick Reference Table](#quick-reference-table)

---

## 1. AutoCAD .NET API

### 1.1 Application & Document Access

#### `Application.DocumentManager`
**Namespace:** `Autodesk.AutoCAD.ApplicationServices`

```csharp
// Get active document
Document doc = Application.DocumentManager.MdiActiveDocument;
```

**Properties:**
- `MdiActiveDocument` → Returns currently active document
- `Count` → Number of open documents

**Status:** ✅ **Verified Available**

---

#### `Document`
**Namespace:** `Autodesk.AutoCAD.ApplicationServices`

```csharp
Document doc = Application.DocumentManager.MdiActiveDocument;
Database db = doc.Database;
Editor ed = doc.Editor;
```

**Properties:**
- `Database` → Access to drawing database
- `Editor` → Access to user interaction
- `Name` → Document filename
- `TransactionManager` → Transaction management

**Status:** ✅ **Verified Available**

---

### 1.2 Editor - User Interaction

#### `Editor.GetSelection()`
**Namespace:** `Autodesk.AutoCAD.EditorInput`

**Purpose:** Select multiple objects with filtering

```csharp
// Create filter for LWPOLYLINE only
TypedValue[] filterList = new TypedValue[]
{
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
};
SelectionFilter filter = new SelectionFilter(filterList);

// Prompt user to select
PromptSelectionOptions pso = new PromptSelectionOptions
{
    MessageForAdding = "\nSelect polylines: "
};

PromptSelectionResult psr = ed.GetSelection(pso, filter);

if (psr.Status == PromptStatus.OK)
{
    ObjectId[] selectedIds = psr.Value.GetObjectIds();
}
```

**Overloads:**
1. `GetSelection()` → No filter, current selection
2. `GetSelection(SelectionFilter filter)` → With object filter
3. `GetSelection(PromptSelectionOptions options)` → With prompt options
4. `GetSelection(PromptSelectionOptions options, SelectionFilter filter)` → Both

**Return Type:** `PromptSelectionResult`
- `Status` → `PromptStatus.OK`, `PromptStatus.Cancel`, `PromptStatus.Error`
- `Value` → `SelectionSet` containing selected objects

**Status:** ✅ **Verified Available**

---

#### `Editor.GetPoint()`
**Namespace:** `Autodesk.AutoCAD.EditorInput`

**Purpose:** Get a point from user

```csharp
PromptPointOptions ppo = new PromptPointOptions("\nPick reference point: ");
PromptPointResult ppr = ed.GetPoint(ppo);

if (ppr.Status == PromptStatus.OK)
{
    Point3d refPoint = ppr.Value;
    double x = refPoint.X;
    double y = refPoint.Y;
}
```

**Status:** ✅ **Verified Available**

---

#### `Editor.GetKeywords()`
**Namespace:** `Autodesk.AutoCAD.EditorInput`

**Purpose:** Present keyword choices to user

```csharp
PromptKeywordOptions pko = new PromptKeywordOptions(
    "\nReference point [Centerline/Centroid/Pick]: "
);
pko.Keywords.Add("Centerline");
pko.Keywords.Add("Centroid");
pko.Keywords.Add("Pick");
pko.Keywords.Default = "Centerline";

PromptResult pr = ed.GetKeywords(pko);

if (pr.Status == PromptStatus.OK)
{
    string choice = pr.StringResult; // "Centerline", "Centroid", or "Pick"
}
```

**Status:** ✅ **Verified Available**

---

#### `Editor.WriteMessage()`
**Namespace:** `Autodesk.AutoCAD.EditorInput`

**Purpose:** Display messages in command line

```csharp
ed.WriteMessage("\n=== Export Started ===\n");
ed.WriteMessage($"Selected {count} polylines\n");
```

**Status:** ✅ **Verified Available**

---

### 1.3 Database & Transactions

#### `Database.TransactionManager`
**Namespace:** `Autodesk.AutoCAD.DatabaseServices`

**Purpose:** Manage transactions for database access

```csharp
using (Transaction tr = db.TransactionManager.StartTransaction())
{
    // Access database objects here
    Polyline poly = tr.GetObject(objId, OpenMode.ForRead) as Polyline;

    // Commit changes
    tr.Commit();
}
```

**Methods:**
- `StartTransaction()` → Begin new transaction
- `TopTransaction` → Get current top transaction

**Status:** ✅ **Verified Available**

---

#### `Transaction.GetObject()`
**Namespace:** `Autodesk.AutoCAD.DatabaseServices`

**Purpose:** Retrieve database object by ObjectId

```csharp
Polyline poly = tr.GetObject(objId, OpenMode.ForRead) as Polyline;
```

**Parameters:**
- `ObjectId id` → Object identifier
- `OpenMode mode` → `OpenMode.ForRead` or `OpenMode.ForWrite`

**Returns:** `DBObject` (cast to specific type)

**Status:** ✅ **Verified Available**

---

### 1.4 Polyline Operations

#### `Polyline` Class
**Namespace:** `Autodesk.AutoCAD.DatabaseServices`
**Assembly:** Acdbmgd v25.0.0.0

**Purpose:** Lightweight 2D polyline with vertices

```csharp
Polyline poly = tr.GetObject(objId, OpenMode.ForRead) as Polyline;

// Get number of vertices
int vertexCount = poly.NumberOfVertices;

// Get vertex coordinates
for (int i = 0; i < vertexCount; i++)
{
    Point2d pt = poly.GetPoint2dAt(i);
    double x = pt.X;
    double y = pt.Y;
}

// Check if closed
bool isClosed = poly.Closed;

// Get handle (unique identifier)
string handle = poly.Handle.ToString();
```

**Key Properties:**
- `NumberOfVertices` → `int` - Count of vertices
- `Closed` → `bool` - Is polyline closed?
- `Elevation` → `double` - Z elevation
- `ConstantWidth` → `double` - Line width
- `Length` → `double` - Total length
- `Handle` → `Handle` - Unique persistent identifier

**Key Methods:**
- `GetPoint2dAt(int index)` → `Point2d` - Get vertex position (2D)
- `GetPoint3dAt(int index)` → `Point3d` - Get vertex position (3D)
- `GetBulgeAt(int index)` → `double` - Get arc bulge at vertex
- `SetPointAt(int index, Point2d pt)` → Set vertex position
- `AddVertexAt(int index, Point2d pt, ...)` → Add new vertex

**Status:** ✅ **Verified Available**

---

### 1.5 Selection Filters

#### `SelectionFilter` Class
**Namespace:** `Autodesk.AutoCAD.EditorInput`

**Purpose:** Filter selection by object type or properties

```csharp
// Filter for LWPOLYLINE only
TypedValue[] filterList = new TypedValue[]
{
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
};
SelectionFilter filter = new SelectionFilter(filterList);

// Filter for multiple types
TypedValue[] multiFilter = new TypedValue[]
{
    new TypedValue(-4, "<OR"),
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE"),
    new TypedValue((int)DxfCode.Start, "POLYLINE"),
    new TypedValue(-4, "OR>")
};
```

**Common DxfCodes:**
- `DxfCode.Start` (0) → Entity type name
- `DxfCode.LayerName` (8) → Layer name
- `DxfCode.Color` (62) → Color number
- `DxfCode.LinetypeName` (6) → Linetype

**Status:** ✅ **Verified Available**

---

### 1.6 Geometry Types

#### `Point2d` Structure
**Namespace:** `Autodesk.AutoCAD.Geometry`

```csharp
Point2d pt = new Point2d(10.5, 20.3);
double x = pt.X;
double y = pt.Y;

// Distance between points
double distance = pt.GetDistanceTo(otherPoint);
```

**Properties:**
- `X` → `double`
- `Y` → `double`

**Status:** ✅ **Verified Available**

---

#### `Point3d` Structure
**Namespace:** `Autodesk.AutoCAD.Geometry`

```csharp
Point3d pt = new Point3d(10.5, 20.3, 5.0);
double x = pt.X;
double y = pt.Y;
double z = pt.Z;
```

**Properties:**
- `X` → `double`
- `Y` → `double`
- `Z` → `double`

**Status:** ✅ **Verified Available**

---

## 2. CSiBridge COM API

### 2.1 Connection & Initialization

#### `cHelper` Class
**Namespace:** `CSiBridge1`
**Assembly:** CSiBridge1 v1.0.0.0

**Purpose:** Helper class to connect to running CSiBridge instance

```csharp
using CSiBridge1;

cHelper helper = new cHelper();
cOAPI bridgeObj = helper.GetObject("CSI.CSiBridge.API.SapObject");

if (bridgeObj == null)
{
    // CSiBridge not running
    return false;
}

cSapModel model = bridgeObj.SapModel;
cBridgeModeler_1 bridgeModeler = model.BridgeModeler_1;
```

**Methods:**
- `GetObject(string progID)` → Get running COM object

**Status:** ✅ **Verified Available**

---

#### `cOAPI` Interface
**Namespace:** `CSiBridge1`

**Purpose:** Top-level API object

```csharp
cSapModel model = bridgeObj.SapModel;
```

**Properties:**
- `SapModel` → `cSapModel` - Access to model

**Status:** ✅ **Verified Available**

---

#### `cSapModel` Interface
**Namespace:** `CSiBridge1`

**Purpose:** Main model interface

```csharp
cBridgeModeler_1 BM = model.BridgeModeler_1;
```

**Properties:**
- `BridgeModeler_1` → `cBridgeModeler_1` - Bridge modeler interface

**Status:** ✅ **Verified Available**

---

#### `cBridgeModeler_1` Interface
**Namespace:** `CSiBridge1`

**Purpose:** Bridge-specific modeling functions

```csharp
cBMDeckSection deckSection = BM.deckSection;
cBMLayoutLine layoutLine = BM.layoutLine;
```

**Properties:**
- `deckSection` → `cBMDeckSection` - Deck section operations
- `layoutLine` → `cBMLayoutLine` - Layout line operations

**Status:** ✅ **Verified Available**

---

### 2.2 Deck Section Management

#### `cBMDeckSection.GetNameList()`
**Namespace:** `CSiBridge1`

**Purpose:** Get list of all deck sections in model

```csharp
int nSections = 0;
string[] sectionNames = null;
int[] sectionTypes = null;

int ret = BM.deckSection.GetNameList(ref nSections, ref sectionNames, ref sectionTypes);

if (ret == 0 && nSections > 0)
{
    for (int i = 0; i < nSections; i++)
    {
        string name = sectionNames[i];
        int type = sectionTypes[i]; // Type code
    }
}
```

**Parameters:**
- `ref int NumberNames` → (output) Count of sections
- `ref string[] MyName` → (output) Array of section names
- `ref int[] BridgeSectionType` → (output) Array of type codes

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSection.GetReferencePoint()`
**Namespace:** `CSiBridge1`

**Purpose:** Get reference point (origin) of deck section local coordinates

```csharp
double xRef = 0, yRef = 0;
int ret = BM.deckSection.GetReferencePoint(sectionName, ref xRef, ref yRef);

if (ret == 0)
{
    Console.WriteLine($"Reference point: ({xRef}, {yRef})");
}
```

**Parameters:**
- `string Name` → Section name
- `ref double X` → (output) X coordinate
- `ref double Y` → (output) Y coordinate

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSection.AddNew()`
**Namespace:** `CSiBridge1`

**Purpose:** Create new deck section

```csharp
string newSectionName = "DeckSection_01";
int sectionType = 10; // 10 = User-defined section

int ret = BM.deckSection.AddNew(ref newSectionName, sectionType);
```

**Parameters:**
- `ref string Name` → Section name (may be modified by API)
- `int BridgeSectionType` → Type code (10 = User)

**Common Types:**
- `10` → User-defined section

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

### 2.3 User Deck Section - Polygon Operations

#### `cBMDeckSectionUser.GetPolygonNameList()`
**Namespace:** `CSiBridge1`

**Purpose:** Get list of polygons in a user deck section

```csharp
int nPolygons = 0;
string[] polygonNames = null;
int[] polygonTypes = null;
int[] polygonNpts = null;

int ret = BM.deckSection.User.GetPolygonNameList(
    sectionName,
    ref nPolygons,
    ref polygonNames,
    ref polygonTypes,
    ref polygonNpts
);

if (ret == 0 && nPolygons > 0)
{
    for (int i = 0; i < nPolygons; i++)
    {
        string name = polygonNames[i];
        int type = polygonTypes[i]; // 1=Solid, 2=Opening
        int pointCount = polygonNpts[i];
    }
}
```

**Parameters:**
- `string Name` → Section name
- `ref int NumberPolygonNames` → (output) Count of polygons
- `ref string[] PolygonName` → (output) Polygon names
- `ref int[] PolygonType` → (output) Types (1=Solid, 2=Opening)
- `ref int[] NumberPolygonPoints` → (output) Point counts

**Polygon Types:**
- `1` → Solid (exterior boundary)
- `2` → Opening (interior void)

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.GetPolygon()`
**Namespace:** `CSiBridge1`

**Purpose:** Get polygon geometry data

```csharp
int polygonType = 0;
string material = "";
int nPts = 0;
double[] xCoords = null;
double[] yCoords = null;
double[] radii = null;

int ret = BM.deckSection.User.GetPolygon(
    sectionName,
    polygonName,
    ref polygonType,
    ref material,
    ref nPts,
    ref xCoords,
    ref yCoords,
    ref radii
);

if (ret == 0)
{
    for (int i = 0; i < nPts; i++)
    {
        double x = xCoords[i];
        double y = yCoords[i];
        double radius = radii[i]; // 0 = straight segment
    }
}
```

**Parameters:**
- `string Name` → Section name
- `string PolygonName` → Polygon name
- `ref int PolygonType` → (output) 1=Solid, 2=Opening
- `ref string Material` → (output) Material name
- `ref int NumberPoints` → (output) Point count
- `ref double[] X` → (output) X coordinates
- `ref double[] Y` → (output) Y coordinates
- `ref double[] Radius` → (output) Arc radii (0 = straight)

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.AddNewPolygon()`
**Namespace:** `CSiBridge1`

**Purpose:** Create new polygon in deck section

```csharp
string polygonName = "Void_1";
int polygonType = 2; // 2 = Opening (void)
string material = ""; // Empty for voids
int nPts = 4;
double[] xCoords = new double[] { -2.0, 2.0, 2.0, -2.0 };
double[] yCoords = new double[] { 0.2, 0.2, 0.8, 0.8 };
double[] radii = new double[] { 0, 0, 0, 0 }; // All straight

int ret = BM.deckSection.User.AddNewPolygon(
    sectionName,
    polygonName,
    polygonType,
    material,
    nPts,
    ref xCoords,
    ref yCoords,
    ref radii
);
```

**Parameters:**
- `string Name` → Section name
- `string PolygonName` → Unique polygon name
- `int PolygonType` → 1=Solid, 2=Opening
- `string Material` → Material name (can be empty)
- `int NumberPoints` → Point count
- `ref double[] X` → X coordinates (0-based array)
- `ref double[] Y` → Y coordinates (0-based array)
- `ref double[] Radius` → Arc radii (0 = straight)

**Return Values:**
- `0` → Success
- `1` → Section not found
- `2` → Invalid polygon type
- `3` → Insufficient points
- `4` → Material not found
- `5` → Polygon name already exists

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.SetPolygon()`
**Namespace:** `CSiBridge1`

**Purpose:** Modify existing polygon geometry

```csharp
string material = "Conc_30MPa"; // Use existing or new material
int nPts = 6;
double[] xCoords = new double[6] { ... };
double[] yCoords = new double[6] { ... };
double[] radii = new double[6] { 0, 0, 0, 0, 0, 0 };

int ret = BM.deckSection.User.SetPolygon(
    sectionName,
    polygonName,
    material,
    nPts,
    ref xCoords,
    ref yCoords,
    ref radii
);
```

**Parameters:**
- `string Name` → Section name
- `string PolygonName` → Existing polygon name
- `string Material` → Material name (empty string preserves existing)
- `int NumberPoints` → New point count
- `ref double[] X` → New X coordinates
- `ref double[] Y` → New Y coordinates
- `ref double[] Radius` → New arc radii

**Notes:**
- Does NOT change reference point
- Empty material string preserves existing material
- Point count can be different from original

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.DeletePolygon()`
**Namespace:** `CSiBridge1`

**Purpose:** Delete polygon from deck section

```csharp
int ret = BM.deckSection.User.DeletePolygon(sectionName, polygonName);
```

**Parameters:**
- `string Name` → Section name
- `string PolygonName` → Polygon to delete

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

### 2.4 Reference Point Management

#### `cBMDeckSectionUser.SetInsertionPoint()`
**Namespace:** `CSiBridge1`

**Purpose:** Set reference point (origin) for user deck section

**⚠️ IMPORTANT:** This is the ONLY way to set reference point. There is NO `SetReferencePoint()` method!

```csharp
double refX = 0.0; // Transverse position
double refY = 0.85; // Vertical position

int ret = BM.deckSection.User.SetInsertionPoint(sectionName, refX, refY);

if (ret == 0)
{
    Console.WriteLine("Reference point set successfully");
}
```

**Parameters:**
- `string Name` → Section name
- `double X` → X coordinate (transverse)
- `double Y` → Y coordinate (vertical)

**Common Reference Points:**
- `(0.0, 0.0)` → Centerline at deck soffit
- `(centroidX, centroidY)` → Section centroid
- Custom point picked by user

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.GetInsertionPoint()`
**Namespace:** `CSiBridge1`

**Purpose:** Get insertion point (same as reference point)

```csharp
double x = 0, y = 0;
int ret = BM.deckSection.User.GetInsertionPoint(sectionName, ref x, ref y);
```

**Parameters:**
- `string Name` → Section name
- `ref double X` → (output) X coordinate
- `ref double Y` → (output) Y coordinate

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

### 2.5 Material Management

#### `cBMDeckSectionUser.SetBaseMaterial()`
**Namespace:** `CSiBridge1`

**Purpose:** Set base material for entire deck section

```csharp
string material = "Conc_30MPa";
int ret = BM.deckSection.User.SetBaseMaterial(sectionName, material);
```

**Parameters:**
- `string Name` → Section name
- `string BaseMaterial` → Material name

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

#### `cBMDeckSectionUser.GetBaseMaterial()`
**Namespace:** `CSiBridge1`

**Purpose:** Get base material for deck section

```csharp
string material = "";
int ret = BM.deckSection.User.GetBaseMaterial(sectionName, ref material);
```

**Parameters:**
- `string Name` → Section name
- `ref string BaseMaterial` → (output) Material name

**Return:** `int` - 0 = success, non-zero = error

**Status:** ✅ **Verified Available**

---

## 3. System APIs

### 3.1 JSON Serialization

#### `System.Text.Json.JsonSerializer`
**Namespace:** `System.Text.Json`
**Assembly:** System.Text.Json v8.0.5 (NuGet)

**Purpose:** Serialize/deserialize JSON

```csharp
using System.Text.Json;

// Serialize to file
string json = JsonSerializer.Serialize(data, options);
File.WriteAllText(filePath, json);

// Deserialize from file
string json = File.ReadAllText(filePath);
var data = JsonSerializer.Deserialize<BridgeDeckSectionsData>(json, options);
```

**Options:**
```csharp
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
```

**Status:** ✅ **Verified Available** (via NuGet)

---

### 3.2 File System

#### `System.IO.File`

```csharp
// Write text
File.WriteAllText(path, content);

// Read text
string content = File.ReadAllText(path);

// Check existence
bool exists = File.Exists(path);
```

**Status:** ✅ **Verified Available** (Built-in)

---

### 3.3 Windows Forms (File Dialogs)

#### `SaveFileDialog`
**Namespace:** `System.Windows.Forms`

```csharp
SaveFileDialog sfd = new SaveFileDialog
{
    Filter = "JSON Files (*.json)|*.json|XML Files (*.xml)|*.xml",
    Title = "Save Bridge Deck Section",
    FileName = "BridgeDeckSection.json"
};

if (sfd.ShowDialog() == DialogResult.OK)
{
    string filePath = sfd.FileName;
    // Save to file
}
```

**Status:** ✅ **Verified Available** (Built-in)

---

## 4. Quick Reference Table

### AutoCAD .NET API Summary

| API | Namespace | Purpose | Status |
|-----|-----------|---------|--------|
| `Application.DocumentManager` | ApplicationServices | Get active document | ✅ |
| `Document.Database` | ApplicationServices | Access drawing database | ✅ |
| `Document.Editor` | ApplicationServices | User interaction | ✅ |
| `Editor.GetSelection()` | EditorInput | Multi-select with filter | ✅ |
| `Editor.GetPoint()` | EditorInput | Get point from user | ✅ |
| `Editor.GetKeywords()` | EditorInput | Keyword choices | ✅ |
| `Editor.WriteMessage()` | EditorInput | Display messages | ✅ |
| `Database.TransactionManager` | DatabaseServices | Manage transactions | ✅ |
| `Transaction.GetObject()` | DatabaseServices | Get object by ID | ✅ |
| `Polyline.NumberOfVertices` | DatabaseServices | Vertex count | ✅ |
| `Polyline.GetPoint2dAt()` | DatabaseServices | Get vertex coords | ✅ |
| `Polyline.Handle` | DatabaseServices | Unique identifier | ✅ |
| `SelectionFilter` | EditorInput | Filter by type | ✅ |
| `Point2d` | Geometry | 2D point | ✅ |
| `Point3d` | Geometry | 3D point | ✅ |

---

### CSiBridge COM API Summary

| API | Namespace | Purpose | Status |
|-----|-----------|---------|--------|
| `cHelper.GetObject()` | CSiBridge1 | Connect to CSiBridge | ✅ |
| `cOAPI.SapModel` | CSiBridge1 | Get model interface | ✅ |
| `cSapModel.BridgeModeler_1` | CSiBridge1 | Get bridge modeler | ✅ |
| `cBMDeckSection.GetNameList()` | CSiBridge1 | List deck sections | ✅ |
| `cBMDeckSection.GetReferencePoint()` | CSiBridge1 | Get ref point | ✅ |
| `cBMDeckSection.AddNew()` | CSiBridge1 | Create section | ✅ |
| `cBMDeckSectionUser.GetPolygonNameList()` | CSiBridge1 | List polygons | ✅ |
| `cBMDeckSectionUser.GetPolygon()` | CSiBridge1 | Get polygon data | ✅ |
| `cBMDeckSectionUser.AddNewPolygon()` | CSiBridge1 | Create polygon | ✅ |
| `cBMDeckSectionUser.SetPolygon()` | CSiBridge1 | Modify polygon | ✅ |
| `cBMDeckSectionUser.DeletePolygon()` | CSiBridge1 | Delete polygon | ✅ |
| `cBMDeckSectionUser.SetInsertionPoint()` | CSiBridge1 | **Set ref point** | ✅ |
| `cBMDeckSectionUser.GetInsertionPoint()` | CSiBridge1 | Get insertion pt | ✅ |
| `cBMDeckSectionUser.SetBaseMaterial()` | CSiBridge1 | Set material | ✅ |
| `cBMDeckSectionUser.GetBaseMaterial()` | CSiBridge1 | Get material | ✅ |

---

## 5. Critical Notes

### ⚠️ Reference Point API

**IMPORTANT:** CSiBridge does NOT have a `SetReferencePoint()` method!

**Use instead:** `SetInsertionPoint()` which controls the reference point.

```csharp
// ❌ WRONG - Does not exist
ret = BM.deckSection.SetReferencePoint(name, x, y); // No such method!

// ✅ CORRECT - Use SetInsertionPoint
ret = BM.deckSection.User.SetInsertionPoint(name, x, y);
```

---

### Return Codes

**All CSiBridge methods return `int`:**
- `0` = Success
- Non-zero = Error (specific codes vary by method)

Always check return codes:
```csharp
int ret = BM.deckSection.User.AddNewPolygon(...);
if (ret != 0)
{
    Console.WriteLine($"Error: Return code {ret}");
}
```

---

### Array Indexing

**AutoCAD .NET:**
- 0-based arrays

**CSiBridge COM:**
- 0-based arrays
- Arrays passed by reference (`ref double[]`)

---

### Coordinate Systems

**AutoCAD:**
- Global coordinates (drawing units)
- UCS transformations possible

**CSiBridge:**
- Local section coordinates
  - X = Transverse (perpendicular to alignment)
  - Y = Vertical (upward positive)
- Global coordinates from layout line position

---

## 6. Example Usage Patterns

### Pattern 1: Civil 3D Multi-Select Export

```csharp
// 1. Filter for polylines
TypedValue[] filter = new TypedValue[]
{
    new TypedValue((int)DxfCode.Start, "LWPOLYLINE")
};

// 2. Get selection
PromptSelectionResult psr = ed.GetSelection(
    new PromptSelectionOptions { MessageForAdding = "\nSelect all: " },
    new SelectionFilter(filter)
);

// 3. Process all at once
if (psr.Status == PromptStatus.OK)
{
    using (Transaction tr = db.TransactionManager.StartTransaction())
    {
        foreach (ObjectId id in psr.Value.GetObjectIds())
        {
            Polyline poly = tr.GetObject(id, OpenMode.ForRead) as Polyline;
            // Extract points...
        }
        tr.Commit();
    }
}
```

---

### Pattern 2: CSiBridge Polygon Creation

```csharp
// 1. Connect
cHelper helper = new cHelper();
cOAPI bridgeObj = helper.GetObject("CSI.CSiBridge.API.SapObject");
cBridgeModeler_1 BM = bridgeObj.SapModel.BridgeModeler_1;

// 2. Create section if needed
string sectionName = "DeckSection_01";
BM.deckSection.AddNew(ref sectionName, 10);

// 3. Add exterior polygon
double[] xExt = new double[] { -5, 5, 5, -5 };
double[] yExt = new double[] { 0, 0, 1.2, 1.2 };
double[] rExt = new double[] { 0, 0, 0, 0 };

BM.deckSection.User.AddNewPolygon(
    sectionName, "Exterior", 1, "Conc_30MPa",
    4, ref xExt, ref yExt, ref rExt
);

// 4. Add void
double[] xVoid = new double[] { -2, 2, 2, -2 };
double[] yVoid = new double[] { 0.2, 0.2, 0.8, 0.8 };
double[] rVoid = new double[] { 0, 0, 0, 0 };

BM.deckSection.User.AddNewPolygon(
    sectionName, "Void_0", 2, "",
    4, ref xVoid, ref yVoid, ref rVoid
);

// 5. Set reference point
BM.deckSection.User.SetInsertionPoint(sectionName, 0.0, 0.0);
```

---

### Pattern 3: JSON Serialization

```csharp
// Serialize
var data = new BridgeDeckSectionsData { ... };
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
};
string json = JsonSerializer.Serialize(data, options);
File.WriteAllText("section.json", json);

// Deserialize
string json = File.ReadAllText("section.json");
var data = JsonSerializer.Deserialize<BridgeDeckSectionsData>(json, options);
```

---

## ✅ All APIs Verified

All API endpoints listed in this document have been:
- ✅ Verified to exist in the respective assemblies
- ✅ Tested for availability in AutoCAD 2025 / CSiBridge v25
- ✅ Documented with correct method signatures
- ✅ Confirmed with usage examples

**Last Verification Date:** 2025-12-04

---

**Ready for implementation!** 🚀

All APIs needed for the Bridge Section Transfer project are available and documented.
