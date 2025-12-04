# Bridge Cross-Section Documentation - Part 2: CSiBridge Import

---

## 4. CSiBridge Import Function - Detailed Analysis

### 4.1 Entry Point: `ImportDeckSectionXML_Corrected()`

**Location:** Lines 514-626
**Purpose:** Parse XML and populate Excel worksheet with section data

#### Function Flow

```
ImportDeckSectionXML_Corrected()
├── Check/Create "DeckImport" worksheet
├── GetOpenFilename() - Select XML file
├── Load XML with MSXML2.DOMDocument
├── Parse error checking
└── Loop through DeckSection nodes
    ├── Extract metadata (name, station, centroid, void count)
    ├── Write section header to Excel
    ├── Parse ExteriorBoundary points
    └── Parse InteriorVoids (if any)
```

#### API Calls Used

1. **Excel Worksheet Management**
   ```vba
   Set ws = ThisWorkbook.Sheets("DeckImport")  ' Try to get existing
   Set ws = ThisWorkbook.Sheets.Add(After:=ThisWorkbook.Sheets(ThisWorkbook.Sheets.Count))
   ws.name = "DeckImport"
   ws.Cells.Clear  ' Clear existing data
   ```

2. **File Dialog**
   ```vba
   filePath = Application.GetOpenFilename("XML Files (*.xml), *.xml", , "Select Bridge Deck XML File")
   ```

3. **XML Parsing (MSXML2)**
   ```vba
   Set xmlDoc = CreateObject("MSXML2.DOMDocument")
   xmlDoc.Load filePath
   If xmlDoc.ParseError.ErrorCode <> 0 Then
       MsgBox "Error in XML File: " & xmlDoc.ParseError.Reason, vbCritical
   End If
   ```

4. **XPath Queries**
   ```vba
   For Each deckSection In xmlDoc.SelectNodes("//DeckSection")
       name = deckSection.getAttribute("Name")
       Set exterior = deckSection.SelectSingleNode("ExteriorBoundary")
       Set interiorVoids = deckSection.SelectSingleNode("InteriorVoids")
       For Each pt In exterior.SelectNodes("Point")
   ```

### 4.2 Excel Data Layout

#### Worksheet Structure

```
Row 1:  [Section Name] [Station] [CentroidX] [CentroidY] [VoidCount]
Row 2:  [Name Value]   [Value]   [Value]     [Value]     [Count]
Row 3:
Row 4:  [ExteriorBoundary]
Row 5:  [Index] [X] [Y]
Row 6:  [1]     [x1] [y1]
Row 7:  [2]     [x2] [y2]
...
Row N:  [InteriorVoids]
Row N+1: [Interior_0]
Row N+2: [Index] [X] [Y]
Row N+3: [1]     [x1] [y1]
...
```

#### Data Extraction Logic

```vba
' Metadata extraction
name = deckSection.getAttribute("Name")
station = deckSection.getAttribute("Station")
cx = deckSection.getAttribute("CentroidX")
cy = deckSection.getAttribute("CentroidY")

' Point iteration
Set exterior = deckSection.SelectSingleNode("ExteriorBoundary")
extPointCount = exterior.getAttribute("PointCount")

i = 1
For Each pt In exterior.SelectNodes("Point")
    ws.Cells(rowIndex, 1).Value = i
    ws.Cells(rowIndex, 2).Value = pt.getAttribute("X")
    ws.Cells(rowIndex, 3).Value = pt.getAttribute("Y")
    rowIndex = rowIndex + 1
    i = i + 1
Next

' Void handling
Set interiorVoids = deckSection.SelectSingleNode("InteriorVoids")
If Not interiorVoids Is Nothing Then
    ws.Cells(rowIndex, 5).Value = interiorVoids.getAttribute("VoidCount")
End If
```

### 4.3 CSiBridge Polygon Manager: `ComprehensivePolygonManager_FromExcel()`

**Location:** Lines 630-797
**Purpose:** Read Excel data and create deck section geometry in CSiBridge

#### Function Flow

```
ComprehensivePolygonManager_FromExcel()
├── Connect to CSiBridge (COM)
│   └── CSiBridge1.Helper → CSiBridge1.cOAPI → cSapModel → cBridgeModeler_1
├── Get User Deck Sections List
├── User Selects Section
├── Get Current Reference Point (verification)
├── Handle Existing Polygons
│   ├── Find Exterior Polygon (Type 1)
│   └── Delete Interior Polygons (Type 2) if requested
├── Select Excel Workbook and Worksheet
├── Read Void Count from Excel E2
├── Get Column Configuration (X, Y columns)
├── Modify/Create Exterior Polygon
│   ├── ModifyExistingPolygonFromExcel() - if exterior exists
│   └── CreatePolygonFromExcel() - if new section
└── Create Interior Polygons (Voids)
    └── Loop: CreatePolygonFromExcel() for each void
```

#### CSiBridge API Connection

**Lines 632-643**

```vba
Dim helper As CSiBridge1.cHelper
Dim bridgeObj As CSiBridge1.cOAPI
Dim sapModel As CSiBridge1.cSapModel
Dim BM As CSiBridge1.cBridgeModeler_1

Set helper = CreateObject("CSiBridge1.Helper")
Set bridgeObj = helper.GetObject("CSI.CSiBridge.API.SapObject")

If bridgeObj Is Nothing Then
    MsgBox "CSiBridge not running or model not open.", vbCritical
    Exit Sub
End If

Set sapModel = bridgeObj.sapModel
Set BM = sapModel.BridgeModeler_1
```

**API Hierarchy:**
```
CSiBridge1.Helper
└── CSiBridge1.cOAPI (SapObject)
    └── CSiBridge1.cSapModel
        └── CSiBridge1.cBridgeModeler_1
            └── CSiBridge1.cBMDeckSection
                └── CSiBridge1.cBMDeckSectionUser
```

### 4.4 CSiBridge API Methods Used

#### 4.4.1 Deck Section Enumeration

**Method:** `BM.deckSection.GetNameList()`

```vba
Dim nSections As Long
Dim secNames() As String
Dim bridgeSectionTypes() As Long
Dim ret As Long

ret = BM.deckSection.GetNameList(nSections, secNames, bridgeSectionTypes)
```

**Returns:**
- `nSections`: Count of deck sections
- `secNames`: Array of section names
- `bridgeSectionTypes`: Array of type codes
- `ret`: Return code (0 = success)

#### 4.4.2 Reference Point Management

**Method:** `BM.deckSection.GetReferencePoint()`

```vba
Dim XrefOriginal As Double, YrefOriginal As Double
ret = BM.deckSection.GetReferencePoint(sectionName, XrefOriginal, YrefOriginal)
```

**Purpose:**
- Get local coordinate system origin for the deck section
- Used for verification (should not change during polygon operations)

**Critical:** The code verifies reference point remains unchanged after operations (lines 783-791)

#### 4.4.3 Polygon Enumeration

**Method:** `BM.deckSection.User.GetPolygonNameList()`

```vba
Dim nPolygons As Long
Dim polygonNames() As String
Dim polygonTypes() As Long
Dim polygonNpts() As Long

ret = BM.deckSection.User.GetPolygonNameList(sectionName, nPolygons, _
                                               polygonNames, polygonTypes, polygonNpts)
```

**Polygon Types:**
- **Type 1:** Solid (exterior boundary)
- **Type 2:** Opening (interior void)

#### 4.4.4 Polygon Deletion

**Method:** `BM.deckSection.User.DeletePolygon()`

```vba
ret = BM.deckSection.User.DeletePolygon(sectionName, polygonNames(i))
```

**Strategy:** Delete only void polygons (Type 2), modify exterior instead of deleting

#### 4.4.5 Get Existing Polygon Data

**Method:** `BM.deckSection.User.GetPolygon()`

**Location:** Line 813 in `ModifyExistingPolygonFromExcel()`

```vba
Dim existingType As Long
Dim existingMaterial As String
Dim existingNpts As Long
Dim existingX() As Double
Dim existingY() As Double
Dim existingR() As Double

ret = BM.deckSection.User.GetPolygon(sectionName, polygonName, _
                                      existingType, existingMaterial, _
                                      existingNpts, existingX, existingY, existingR)
```

**Returns:**
- Polygon type (1=Solid, 2=Opening)
- Material name
- Number of points
- X coordinates array
- Y coordinates array
- Radius array (0 = straight segment)

#### 4.4.6 Modify Existing Polygon

**Method:** `BM.deckSection.User.SetPolygon()`

**Location:** Line 857 in `ModifyExistingPolygonFromExcel()`

```vba
ret = BM.deckSection.User.SetPolygon(sectionName, polygonName, material, _
                                      nPts, xPoly, yPoly, radiusPoly)
```

**Parameters:**
- `sectionName`: Name of deck section
- `polygonName`: Name of existing polygon to modify
- `material`: Material name (empty string preserves existing)
- `nPts`: Number of vertices
- `xPoly()`: X coordinates (0-based array)
- `yPoly()`: Y coordinates (0-based array)
- `radiusPoly()`: Radius for each segment (0 = straight)

**Key Insight:** This modifies existing polygon without changing reference point

#### 4.4.7 Create New Polygon

**Method:** `BM.deckSection.User.AddNewPolygon()`

**Location:** Line 907 in `CreatePolygonFromExcel()`

```vba
ret = BM.deckSection.User.AddNewPolygon(sectionName, polygonName, _
                                         polygonType, material, _
                                         nPts, xPoly, yPoly, radiusPoly)
```

**Parameters:**
- `sectionName`: Name of deck section
- `polygonName`: Name for new polygon (e.g., "Void_1")
- `polygonType`: 1=Solid, 2=Opening
- `material`: Material name (can be empty for voids)
- `nPts`: Number of vertices
- `xPoly()`: X coordinates
- `yPoly()`: Y coordinates
- `radiusPoly()`: Radius for each segment

### 4.5 Excel Data Reading Logic

#### Reading Coordinates from Excel

**Location:** Lines 826-853 (ModifyExistingPolygonFromExcel), Lines 873-903 (CreatePolygonFromExcel)

```vba
' Get user input for column configuration
colX = InputBox("Enter column letter for X coordinates (e.g., 'B'):", "X Column", "B")
colY = InputBox("Enter column letter for Y coordinates (e.g., 'C'):", "Y Column", "C")
startRow = Application.InputBox("Enter first data row number for EXTERIOR polygon:", _
                                "Exterior Start Row", 5, Type:=1)

' Count points until empty cell
Dim nPts As Long
nPts = 0
Do While Trim(ws.Range(colX & (startRow + nPts)).Value) <> ""
    nPts = nPts + 1
Loop

' Validate minimum points
If nPts < 3 Then
    MsgBox "Insufficient points (found " & nPts & ", need at least 3)", vbExclamation
    Exit Sub
End If

' Read coordinates into arrays
ReDim xPoly(nPts - 1)
ReDim yPoly(nPts - 1)
ReDim radiusPoly(nPts - 1)

For i = 0 To nPts - 1
    If Not IsNumeric(ws.Range(colX & (startRow + i)).Value) Or _
       Not IsNumeric(ws.Range(colY & (startRow + i)).Value) Then
        MsgBox "Invalid coordinate data at row " & (startRow + i), vbCritical
        Exit Sub
    End If
    xPoly(i) = CDbl(ws.Range(colX & (startRow + i)).Value)
    yPoly(i) = CDbl(ws.Range(colY & (startRow + i)).Value)
    radiusPoly(i) = 0  ' Straight segments only
Next i
```

#### Dynamic Point Detection
- Reads until first empty cell
- No hard-coded point counts
- Validates each cell is numeric before conversion

#### Array Structure
- **0-based indexing** (matches CSiBridge API)
- All radius values set to 0 (no curved segments)

### 4.6 User Interaction Design

#### Workbook/Worksheet Selection

**Location:** Lines 706-739

```vba
' Build list of open workbooks
wbCount = Application.Workbooks.Count
ReDim wbArray(1 To wbCount)
wbList = ""
i = 0
For Each wb In Application.Workbooks
    i = i + 1
    Set wbArray(i) = wb
    wbList = wbList & i & ". " & wb.name & vbCrLf
Next wb

' User selects by number
wbIndex = Application.InputBox("Select workbook by NUMBER:" & vbCrLf & wbList, _
                                "Pick Workbook", 1, Type:=1)

' Repeat for worksheet selection
```

**Design Pattern:**
1. Enumerate available items
2. Build numbered list string
3. Present in InputBox
4. Validate selection range
5. Use selected index to get object reference

---

*[Continue to next part...]*
