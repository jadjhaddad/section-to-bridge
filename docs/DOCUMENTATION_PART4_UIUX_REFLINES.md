# Bridge Cross-Section Documentation - Part 4: UI/UX, Reference Lines & C# Conversion

---

## 8. UI/UX Improvements

### 8.1 Current User Experience Issues

#### Issue 1: Multiple Input Prompts

**Problem:** User must answer many sequential prompts
- File name
- Folder location
- Section name
- Station value
- Select exterior polyline (up to 5 retries)
- For each void: Yes/No/Cancel dialog + selection (up to 5 retries each)

**Impact:** Time-consuming, error-prone, interrupts workflow

**Solution 1: Configuration Form (VBA UserForm)**

```vba
' Create a UserForm with all inputs
Private Sub ShowExportConfigForm()
    Dim frm As New frmExportConfig

    ' Pre-populate defaults
    frm.txtFileName.Value = "BridgeDeckSection"
    frm.txtSectionName.Value = "DeckSection_01"
    frm.txtStation.Value = "0.0"
    frm.txtConcreteStrength.Value = "30.0"
    frm.txtDensity.Value = "2400.0"

    ' Show modal
    If frm.Show = vbOK Then
        ' Get all values at once
        Dim config As ExportConfig
        config.fileName = frm.txtFileName.Value
        config.sectionName = frm.txtSectionName.Value
        config.station = CDbl(frm.txtStation.Value)
        ' ... etc

        ' Proceed with export
        ExecuteExport config
    End If
End Sub
```

**Solution 2: Configuration File**

```vba
' Load settings from INI or JSON file
Private Function LoadExportSettings() As ExportConfig
    Dim fso As Object, tf As Object
    Dim configPath As String
    Dim config As ExportConfig

    Set fso = CreateObject("Scripting.FileSystemObject")
    configPath = Environ("USERPROFILE") & "\BridgeExport.ini"

    If fso.FileExists(configPath) Then
        Set tf = fso.OpenTextFile(configPath)
        ' Parse INI format
        Do While Not tf.AtEndOfStream
            Dim line As String, parts() As String
            line = tf.ReadLine
            If InStr(line, "=") > 0 Then
                parts = Split(line, "=")
                Select Case Trim(parts(0))
                    Case "DefaultFileName"
                        config.defaultFileName = Trim(parts(1))
                    Case "DefaultStation"
                        config.defaultStation = CDbl(Trim(parts(1)))
                    ' ... etc
                End Select
            End If
        Loop
        tf.Close
    End If

    LoadExportSettings = config
End Function
```

#### Issue 2: No Visual Feedback During Processing

**Problem:** User doesn't see progress during:
- XML creation
- Polygon processing
- API calls

**Solution: Progress Form with Status Updates**

```vba
Public Class frmProgress
    Private currentStep As Integer
    Private totalSteps As Integer

    Public Sub Initialize(steps As Integer)
        totalSteps = steps
        currentStep = 0
        Me.progressBar.Max = steps
        Me.lblStatus.Caption = "Initializing..."
        Me.Show vbModeless
        DoEvents
    End Sub

    Public Sub UpdateProgress(step As Integer, message As String)
        currentStep = step
        Me.progressBar.Value = step
        Me.lblStatus.Caption = message
        Me.lblPercent.Caption = Format((step / totalSteps) * 100, "0") & "%"
        DoEvents
    End Sub

    Public Sub Complete()
        Me.progressBar.Value = totalSteps
        Me.lblStatus.Caption = "Complete!"
        Application.Wait Now + TimeValue("00:00:01")
        Me.Hide
    End Sub
End Class

' Usage in main code
Dim progress As New frmProgress
progress.Initialize 5

progress.UpdateProgress 1, "Connecting to AutoCAD..."
Set acad = AttachToRunningAutoCAD()

progress.UpdateProgress 2, "Selecting exterior boundary..."
ProcessSectionGeometry ...

progress.UpdateProgress 3, "Processing voids..."
' ...

progress.Complete
```

#### Issue 3: Poor Error Messages

**Current:** Generic error messages
```vba
MsgBox "Failed to add polygon. Return code: " & ret, vbCritical
```

**Improved:** Specific, actionable error messages

```vba
Private Function GetCSiBridgeErrorMessage(returnCode As Long) As String
    Select Case returnCode
        Case 1
            GetCSiBridgeErrorMessage = "Section name not found. Verify the section exists in the model."
        Case 2
            GetCSiBridgeErrorMessage = "Invalid polygon type. Use 1 for Solid or 2 for Opening."
        Case 3
            GetCSiBridgeErrorMessage = "Insufficient points. Minimum 3 points required for a polygon."
        Case 4
            GetCSiBridgeErrorMessage = "Material not found. Create the material in CSiBridge first."
        Case 5
            GetCSiBridgeErrorMessage = "Polygon name already exists. Use a unique name or delete the existing polygon."
        Case Else
            GetCSiBridgeErrorMessage = "Unknown error (code " & returnCode & "). Check CSiBridge API documentation."
    End Select
End Function

' Usage
If ret <> 0 Then
    MsgBox "Error adding polygon:" & vbCrLf & vbCrLf & _
           GetCSiBridgeErrorMessage(ret) & vbCrLf & vbCrLf & _
           "Section: " & sectionName & vbCrLf & _
           "Polygon: " & polygonName, vbCritical, "CSiBridge API Error"
    Exit Sub
End If
```

### 8.2 Improved Workflow Design

#### Workflow Option 1: Single-Click Export

```
1. User selects polylines in Civil 3D (exterior + voids) BEFORE running macro
2. Macro auto-detects selection
3. Shows single confirmation dialog with preview
4. Exports directly
```

**Implementation:**

```vba
Public Sub QuickExportSelectedPolylines()
    Dim acad As Object, doc As Object, selSet As Object
    Set acad = GetObject(, "AutoCAD.Application")
    Set doc = acad.ActiveDocument

    ' Get current selection
    Set selSet = doc.SelectionSets.Add("TEMP_EXPORT")
    On Error Resume Next
    selSet.SelectOnScreen  ' Let user select if nothing selected
    On Error GoTo 0

    If selSet.Count = 0 Then
        MsgBox "No polylines selected. Please select at least one polyline.", vbExclamation
        selSet.Delete
        Exit Sub
    End If

    ' Identify exterior (largest area) and voids
    Dim i As Integer
    Dim maxArea As Double, maxIndex As Integer
    Dim areas() As Double
    ReDim areas(selSet.Count - 1)

    For i = 0 To selSet.Count - 1
        If LCase(selSet(i).ObjectName) = "acdbpolyline" Then
            areas(i) = CalculatePolylineArea(selSet(i))
            If areas(i) > maxArea Then
                maxArea = areas(i)
                maxIndex = i
            End If
        End If
    Next

    ' Show confirmation
    Dim msg As String
    msg = "Export Configuration:" & vbCrLf & vbCrLf
    msg = msg & "Exterior: " & selSet(maxIndex).Handle & " (Area: " & Format(maxArea, "0.00") & " m²)" & vbCrLf
    msg = msg & "Voids: " & (selSet.Count - 1) & vbCrLf & vbCrLf
    msg = msg & "Proceed with export?"

    If MsgBox(msg, vbYesNo + vbQuestion, "Confirm Export") = vbYes Then
        ' Export with pre-identified polylines
        ExportPreselectedPolylines selSet, maxIndex
    End If

    selSet.Delete
End Sub
```

#### Workflow Option 2: Template-Based Export

```vba
' Save frequently-used configurations as templates
Public Sub SaveExportTemplate()
    Dim config As ExportConfig
    ' ... populate config from current settings

    Dim templateName As String
    templateName = InputBox("Enter template name:", "Save Template", "Default")

    SaveConfigToFile config, templateName
End Sub

Public Sub LoadExportTemplate()
    Dim templates As Collection
    Set templates = GetAvailableTemplates()

    ' Show list picker
    Dim templateName As String
    templateName = ShowTemplatePicker(templates)

    If Len(templateName) > 0 Then
        Dim config As ExportConfig
        config = LoadConfigFromFile(templateName)
        ExecuteExport config
    End If
End Sub
```

### 8.3 CSiBridge Import UI Improvements

#### Issue: Manual Column/Row Input

**Current:** User manually types column letters and row numbers
**Improved:** Visual range selector

```vba
Public Sub SelectDataRange()
    Dim ws As Worksheet
    Set ws = ThisWorkbook.Sheets("DeckImport")

    Dim rng As Range
    On Error Resume Next
    Set rng = Application.InputBox("Select the range containing coordinate data (X, Y columns):", _
                                    "Data Range", Type:=8)
    On Error GoTo 0

    If Not rng Is Nothing Then
        ' Parse range to determine columns and rows
        Dim firstCol As Long, lastCol As Long
        Dim firstRow As Long, lastRow As Long

        firstCol = rng.Column
        lastCol = rng.Column + rng.Columns.Count - 1
        firstRow = rng.Row
        lastRow = rng.Row + rng.Rows.Count - 1

        ' Validate: need exactly 2 columns (X, Y)
        If rng.Columns.Count = 2 Then
            CreatePolygonFromRange rng, firstRow, lastRow
        Else
            MsgBox "Please select exactly 2 columns (X and Y)", vbExclamation
        End If
    End If
End Sub
```

#### Data Preview Before Import

```vba
Public Sub PreviewImport()
    ' Show form with data grid preview
    Dim frm As New frmImportPreview

    frm.LoadData xmlFilePath
    frm.gridPreview.DataSource = GetPreviewData()

    If frm.Show = vbOK Then
        ExecuteImport frm.GetSelectedSections()
    End If
End Sub
```

---

## 9. New Feature: Reference Line Implementation

### 9.1 Understanding CSiBridge Reference Lines

**Reference Line Purpose:**
- Defines the origin and orientation for the deck section
- Used by CSiBridge to position the section along the layout line
- Critical for proper section placement in 3D bridge model

**Reference Line in CSiBridge:**
- Specified by a point (X, Y) in the local section coordinate system
- Typically at centerline of alignment or specific girder
- Retrieved via `BM.deckSection.GetReferencePoint()`

### 9.2 Current Limitation

**Problem:** Code monitors but doesn't SET reference point

```vba
' Lines 665-670, 783-791
ret = BM.deckSection.GetReferencePoint(sectionName, XrefOriginal, YrefOriginal)
' ... later ...
ret = BM.deckSection.GetReferencePoint(sectionName, XrefFinal, YrefFinal)
If Abs(XrefFinal - XrefOriginal) > 0.001 Or Abs(YrefFinal - YrefOriginal) > 0.001 Then
    MsgBox "WARNING: Reference point changed during operation!"
```

**Missing:** No method to explicitly set/define reference point from Civil 3D data

### 9.3 Solution: Reference Line Feature

#### Step 1: Capture Reference Point in Civil 3D Export

**Add to XML Schema:**

```xml
<DeckSection Name="..." Station="..." ...>
    <!-- NEW: Reference line definition -->
    <ReferencePoint X="0.000000" Y="0.850000" Description="Centerline at deck soffit"/>

    <MaterialProperties .../>
    <ExteriorBoundary ...>
    ...
</DeckSection>
```

**Export Code Enhancement:**

```vba
Public Sub ExportDeckSection()
    ' ... existing code ...

    ' NEW: After GetStationInfo
    Dim refX As Double, refY As Double, refDescription As String
    If Not GetReferencePoint(util, refX, refY, refDescription) Then
        ' Use default (0, 0)
        refX = 0#
        refY = 0#
        refDescription = "Default origin"
    End If

    ' Pass to XML creation
    If Not CreateAndSaveXML(sectionName, station, exteriorPoints, voidPoints, voidCount, _
                            sectionArea, centroidX, centroidY, _
                            refX, refY, refDescription, _  ' NEW parameters
                            filePath, util) Then
End Sub

Private Function GetReferencePoint(util As Object, ByRef refX As Double, _
                                   ByRef refY As Double, ByRef description As String) As Boolean
    On Error GoTo ErrHandler

    Dim choice As VbMsgBoxResult
    choice = MsgBox("Define a custom reference point for this section?" & vbCrLf & _
                    "(Default is 0,0 at origin)", vbYesNo + vbQuestion, "Reference Point")

    If choice = vbNo Then
        refX = 0#
        refY = 0#
        description = "Default origin (0, 0)"
        GetReferencePoint = True
        Exit Function
    End If

    ' Method 1: Pick a point in drawing
    Dim pickMethod As Integer
    pickMethod = MsgBox("Pick reference point graphically?" & vbCrLf & vbCrLf & _
                        "Yes = Pick point in drawing" & vbCrLf & _
                        "No = Enter coordinates manually", _
                        vbYesNoCancel + vbQuestion, "Reference Point Method")

    If pickMethod = vbCancel Then
        GetReferencePoint = False
        Exit Function
    ElseIf pickMethod = vbYes Then
        ' Graphical selection
        Dim pickPt As Variant
        On Error Resume Next
        pickPt = util.GetPoint(, "Pick reference point: ")
        On Error GoTo ErrHandler

        If IsArray(pickPt) Then
            refX = pickPt(0)
            refY = pickPt(1)
        Else
            GetReferencePoint = False
            Exit Function
        End If
    Else
        ' Manual entry
        Dim input As String
        input = util.GetString(False, "Enter reference point X coordinate <0.0>: ")
        refX = IIf(LenB(input) = 0, 0#, CDbl(input))

        input = util.GetString(False, "Enter reference point Y coordinate <0.0>: ")
        refY = IIf(LenB(input) = 0, 0#, CDbl(input))
    End If

    description = util.GetString(False, "Enter reference point description <Centerline>: ")
    If LenB(description) = 0 Then description = "Centerline"

    GetReferencePoint = True
    Exit Function

ErrHandler:
    util.prompt "Error getting reference point: " & Err.Description & vbCrLf
    GetReferencePoint = False
End Function
```

**Update XML Generation:**

```vba
Private Function CreateSectionXMLElement(..., refX As Double, refY As Double, _
                                         refDescription As String) As String
    ' ... existing code ...

    xml = "  <DeckSection Name=""" & sectionName & """ ...>" & vbCrLf

    ' NEW: Add reference point
    xml = xml & "    <ReferencePoint X=""" & Format$(refX, "0.000000") & _
                """ Y=""" & Format$(refY, "0.000000") & _
                """ Description=""" & refDescription & """/>" & vbCrLf

    xml = xml & "    <MaterialProperties .../>" & vbCrLf
    ' ... rest of XML
End Function
```

#### Step 2: Apply Reference Point in CSiBridge Import

**Note:** CSiBridge API does NOT have a `SetReferencePoint()` method!

**Research Finding:** After analyzing the CSiBridge API, the `GetReferencePoint()` method exists but there's NO corresponding `SetReferencePoint()`. The reference point is automatically set by CSiBridge based on the first polygon added.

**Solution: Control via Insertion Point**

```vba
' CSiBridge API DOES have SetInsertionPoint for User sections
ret = BM.deckSection.User.SetInsertionPoint(sectionName, X, Y)
```

**Implementation:**

```vba
Public Sub ApplyReferencePointFromXML(deckNode As Object, BM As Object, sectionName As String)
    Dim refNode As Object
    Set refNode = deckNode.SelectSingleNode("ReferencePoint")

    If Not refNode Is Nothing Then
        Dim refX As Double, refY As Double
        refX = CDbl(refNode.getAttribute("X"))
        refY = CDbl(refNode.getAttribute("Y"))

        ' Set insertion point (this controls the reference point)
        Dim ret As Long
        ret = BM.deckSection.User.SetInsertionPoint(sectionName, refX, refY)

        If ret <> 0 Then
            MsgBox "Warning: Failed to set insertion point to (" & refX & ", " & refY & ")", vbExclamation
        Else
            MsgBox "Reference point set to (" & Format(refX, "0.000") & ", " & Format(refY, "0.000") & ")", vbInformation
        End If
    End If
End Sub
```

**Update Import Function:**

```vba
Public Sub ComprehensivePolygonManager_FromExcel()
    ' ... existing code ...

    ' After creating/modifying polygons
    If xmlContainsReferencePoint Then
        ApplyReferencePointFromXML deckNode, BM, sectionName
    End If

    ' ... verification code ...
End Sub
```

### 9.4 Alternative: Reference Line as Annotation

If explicit setting isn't supported, add reference line as visual aid:

```vba
Public Sub AddReferenceLineAnnotation(BM As Object, sectionName As String, refX As Double, refY As Double)
    ' Create a small reference marker polygon
    Dim xRef(3) As Double, yRef(3) As Double, rRef(3) As Double

    ' Cross marker at reference point
    Dim markerSize As Double
    markerSize = 0.1  ' 10 cm marker

    xRef(0) = refX - markerSize: yRef(0) = refY
    xRef(1) = refX + markerSize: yRef(1) = refY
    xRef(2) = refX: yRef(2) = refY + markerSize
    xRef(3) = refX: yRef(3) = refY - markerSize

    ' Note: This creates visual lines but may not be supported by API
    ' Alternative: Add to section notes/properties
End Sub
```

### 9.5 Recommended Reference Point Strategy

**Best Practice for Bridge Deck Sections:**

1. **Centerline at Deck Soffit:**
   - X = 0.0 (transverse centerline)
   - Y = 0.0 (soffit level)
   - Most common for symmetrical sections

2. **Centerline at Deck Centroid:**
   - X = centroidX
   - Y = centroidY
   - Useful for structural analysis

3. **Left Girder:**
   - X = leftmost girder position
   - Y = top of girder
   - For multi-girder bridges

**Export with Multiple Options:**

```vba
Private Function SelectReferencePointPreset(centroidX As Double, centroidY As Double) As String
    Dim frm As New frmReferencePoint

    frm.optCenterlineSoffit.Caption = "Centerline at Soffit (0.0, 0.0)"
    frm.optCentroid.Caption = "Section Centroid (" & Format(centroidX, "0.00") & ", " & Format(centroidY, "0.00") & ")"
    frm.optCustom.Caption = "Custom Point..."

    If frm.Show = vbOK Then
        If frm.optCenterlineSoffit.Value Then
            SelectReferencePointPreset = "0.0|0.0|Centerline at Soffit"
        ElseIf frm.optCentroid.Value Then
            SelectReferencePointPreset = centroidX & "|" & centroidY & "|Section Centroid"
        Else
            ' Custom entry
            ' ...
        End If
    End If
End Function
```

---

## 10. C# Conversion Guide

### 10.1 Why Convert to C#?

**Advantages of C#:**
1. **Better API Support:** CSiBridge 2025 API updated for .NET 8
2. **Type Safety:** Compile-time error checking
3. **Performance:** Faster execution than VBA
4. **Modern IDE:** Visual Studio with IntelliSense, debugging, refactoring tools
5. **Package Management:** NuGet for dependencies (XML parsing, UI frameworks)
6. **Deployment:** Standalone executables, no Excel/AutoCAD VBA dependencies

**CSiBridge 2025 API Update:**
- Native .NET 8 support
- Improved plugin development interface
- Speed enhancements for external clients

### 10.2 Architecture for C# Implementation

```
Solution: BridgeSectionTransfer
│
├── BridgeSectionTransfer.Core (Class Library)
│   ├── Models/
│   │   ├── DeckSection.cs
│   │   ├── Polygon.cs
│   │   ├── MaterialProperties.cs
│   │   └── ReferencePoint.cs
│   ├── Services/
│   │   ├── ICivil3DExporter.cs
│   │   ├── ICSiBridgeImporter.cs
│   │   ├── XmlSerializer.cs
│   │   └── GeometryCalculator.cs
│   └── Utilities/
│       └── CoordinateTransformer.cs
│
├── BridgeSectionTransfer.Civil3D (Class Library - AutoCAD/.NET plugin)
│   ├── Civil3DExporter.cs
│   ├── Commands.cs
│   └── PolylineSelector.cs
│
├── BridgeSectionTransfer.CSiBridge (Class Library - CSiBridge plugin)
│   ├── CSiBridgeImporter.cs
│   ├── PolygonManager.cs
│   └── ReferenceLineManager.cs
│
└── BridgeSectionTransfer.UI (WPF Application)
    ├── ViewModels/
    ├── Views/
    │   ├── ExportConfigView.xaml
    │   ├── ImportConfigView.xaml
    │   └── ProgressView.xaml
    └── App.xaml
```

### 10.3 Key Class Conversions

#### VBA ExportDeckSection → C# DeckSectionExporter

**Data Models:**

```csharp
// Models/DeckSection.cs
using System;
using System.Collections.Generic;

namespace BridgeSectionTransfer.Core.Models
{
    public class DeckSection
    {
        public string Name { get; set; }
        public double Station { get; set; }
        public double Area { get; set; }
        public Point2D Centroid { get; set; }
        public ReferencePoint ReferencePoint { get; set; }
        public MaterialProperties Material { get; set; }
        public Polygon ExteriorBoundary { get; set; }
        public List<Polygon> InteriorVoids { get; set; }

        public DeckSection()
        {
            InteriorVoids = new List<Polygon>();
        }
    }

    public class Point2D
    {
        public double X { get; set; }
        public double Y { get; set; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public class Polygon
    {
        public string Name { get; set; }
        public PolygonType Type { get; set; }
        public List<Point2D> Points { get; set; }
        public string Handle { get; set; }  // AutoCAD object handle

        public Polygon()
        {
            Points = new List<Point2D>();
        }
    }

    public enum PolygonType
    {
        Solid = 1,
        Opening = 2
    }

    public class MaterialProperties
    {
        public double ConcreteStrength { get; set; } = 30.0;  // MPa
        public double Density { get; set; } = 2400.0;  // kg/m³
        public double ElasticModulus { get; set; } = 30000.0;  // MPa
    }

    public class ReferencePoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Description { get; set; }

        public ReferencePoint()
        {
            X = 0.0;
            Y = 0.0;
            Description = "Default origin";
        }
    }
}
```

**Geometry Calculator:**

```csharp
// Services/GeometryCalculator.cs
using System;
using System.Collections.Generic;
using BridgeSectionTransfer.Core.Models;

namespace BridgeSectionTransfer.Core.Services
{
    public class GeometryCalculator
    {
        /// <summary>
        /// Calculate polygon area using the Shoelace formula
        /// </summary>
        public double CalculateArea(List<Point2D> points)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 points");

            double area = 0.0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                area += points[i].X * points[j].Y;
                area -= points[j].X * points[i].Y;
            }

            return Math.Abs(area) / 2.0;
        }

        /// <summary>
        /// Calculate polygon centroid
        /// </summary>
        public Point2D CalculateCentroid(List<Point2D> points)
        {
            if (points == null || points.Count < 3)
                throw new ArgumentException("Polygon must have at least 3 points");

            double area = CalculateArea(points);

            if (Math.Abs(area) < 1e-10)
            {
                // Degenerate polygon, use geometric center
                double sumX = 0, sumY = 0;
                foreach (var pt in points)
                {
                    sumX += pt.X;
                    sumY += pt.Y;
                }
                return new Point2D(sumX / points.Count, sumY / points.Count);
            }

            double cx = 0, cy = 0;
            int n = points.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                double factor = points[i].X * points[j].Y - points[j].X * points[i].Y;
                cx += (points[i].X + points[j].X) * factor;
                cy += (points[i].Y + points[j].Y) * factor;
            }

            double divisor = 6.0 * area;
            return new Point2D(cx / divisor, cy / divisor);
        }

        /// <summary>
        /// Validate and reverse polygon direction if needed
        /// </summary>
        public void EnsureClockwise(Polygon polygon, bool clockwise = true)
        {
            double signedArea = 0.0;
            int n = polygon.Points.Count;

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                signedArea += polygon.Points[i].X * polygon.Points[j].Y;
                signedArea -= polygon.Points[j].X * polygon.Points[i].Y;
            }

            bool isClockwise = signedArea < 0;

            if (isClockwise != clockwise)
            {
                polygon.Points.Reverse();
            }
        }
    }
}
```

**XML Serialization:**

```csharp
// Services/XmlSerializer.cs
using System;
using System.IO;
using System.Xml.Linq;
using System.Collections.Generic;
using BridgeSectionTransfer.Core.Models;

namespace BridgeSectionTransfer.Core.Services
{
    public class DeckSectionXmlSerializer
    {
        public void SerializeToFile(DeckSection section, string filePath)
        {
            var root = new XElement("BridgeDeckSections",
                new XAttribute("ExportDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")),
                new XAttribute("ExportTool", "BridgeSection Transfer C# v1.0"),
                new XAttribute("Units", "Meters"),
                new XAttribute("CoordinateSystem", "X=Transverse(CenterlineAt0), Y=Vertical(UpwardPositive)"));

            var sectionElement = CreateSectionElement(section);
            root.Add(sectionElement);

            var doc = new XDocument(
                new XDeclaration("1.0", "UTF-8", null),
                root
            );

            doc.Save(filePath);
        }

        private XElement CreateSectionElement(DeckSection section)
        {
            var elem = new XElement("DeckSection",
                new XAttribute("Name", section.Name),
                new XAttribute("Station", section.Station.ToString("F3")),
                new XAttribute("Area", section.Area.ToString("F6")),
                new XAttribute("CentroidX", section.Centroid.X.ToString("F6")),
                new XAttribute("CentroidY", section.Centroid.Y.ToString("F6"))
            );

            // Reference point
            if (section.ReferencePoint != null)
            {
                elem.Add(new XElement("ReferencePoint",
                    new XAttribute("X", section.ReferencePoint.X.ToString("F6")),
                    new XAttribute("Y", section.ReferencePoint.Y.ToString("F6")),
                    new XAttribute("Description", section.ReferencePoint.Description)
                ));
            }

            // Material properties
            elem.Add(new XElement("MaterialProperties",
                new XAttribute("ConcreteStrength", section.Material.ConcreteStrength.ToString("F1")),
                new XAttribute("Density", section.Material.Density.ToString("F1")),
                new XAttribute("ElasticModulus", section.Material.ElasticModulus.ToString("F1"))
            ));

            // Exterior boundary
            elem.Add(CreatePolygonElement(section.ExteriorBoundary, "ExteriorBoundary"));

            // Interior voids
            if (section.InteriorVoids.Count > 0)
            {
                var voidsElem = new XElement("InteriorVoids",
                    new XAttribute("VoidCount", section.InteriorVoids.Count)
                );

                for (int i = 0; i < section.InteriorVoids.Count; i++)
                {
                    var voidElem = CreatePolygonElement(section.InteriorVoids[i], "Void");
                    voidElem.Add(new XAttribute("Index", i));
                    voidsElem.Add(voidElem);
                }

                elem.Add(voidsElem);
            }

            return elem;
        }

        private XElement CreatePolygonElement(Polygon polygon, string elementName)
        {
            var elem = new XElement(elementName,
                new XAttribute("PointCount", polygon.Points.Count)
            );

            foreach (var pt in polygon.Points)
            {
                elem.Add(new XElement("Point",
                    new XAttribute("X", pt.X.ToString("F6")),
                    new XAttribute("Y", pt.Y.ToString("F6"))
                ));
            }

            return elem;
        }

        public DeckSection DeserializeFromFile(string filePath)
        {
            var doc = XDocument.Load(filePath);
            var sectionElem = doc.Root.Element("DeckSection");

            if (sectionElem == null)
                throw new InvalidDataException("No DeckSection element found in XML");

            var section = new DeckSection
            {
                Name = sectionElem.Attribute("Name")?.Value,
                Station = double.Parse(sectionElem.Attribute("Station")?.Value ?? "0"),
                Area = double.Parse(sectionElem.Attribute("Area")?.Value ?? "0"),
                Centroid = new Point2D(
                    double.Parse(sectionElem.Attribute("CentroidX")?.Value ?? "0"),
                    double.Parse(sectionElem.Attribute("CentroidY")?.Value ?? "0")
                )
            };

            // Reference point
            var refElem = sectionElem.Element("ReferencePoint");
            if (refElem != null)
            {
                section.ReferencePoint = new ReferencePoint
                {
                    X = double.Parse(refElem.Attribute("X")?.Value ?? "0"),
                    Y = double.Parse(refElem.Attribute("Y")?.Value ?? "0"),
                    Description = refElem.Attribute("Description")?.Value ?? ""
                };
            }

            // Material
            var matElem = sectionElem.Element("MaterialProperties");
            if (matElem != null)
            {
                section.Material = new MaterialProperties
                {
                    ConcreteStrength = double.Parse(matElem.Attribute("ConcreteStrength")?.Value ?? "30"),
                    Density = double.Parse(matElem.Attribute("Density")?.Value ?? "2400"),
                    ElasticModulus = double.Parse(matElem.Attribute("ElasticModulus")?.Value ?? "30000")
                };
            }

            // Exterior boundary
            var exteriorElem = sectionElem.Element("ExteriorBoundary");
            section.ExteriorBoundary = ParsePolygonElement(exteriorElem, "Exterior", PolygonType.Solid);

            // Voids
            var voidsElem = sectionElem.Element("InteriorVoids");
            if (voidsElem != null)
            {
                foreach (var voidElem in voidsElem.Elements("Void"))
                {
                    var index = int.Parse(voidElem.Attribute("Index")?.Value ?? "0");
                    var voidPoly = ParsePolygonElement(voidElem, $"Void_{index}", PolygonType.Opening);
                    section.InteriorVoids.Add(voidPoly);
                }
            }

            return section;
        }

        private Polygon ParsePolygonElement(XElement elem, string name, PolygonType type)
        {
            var polygon = new Polygon
            {
                Name = name,
                Type = type
            };

            foreach (var ptElem in elem.Elements("Point"))
            {
                polygon.Points.Add(new Point2D(
                    double.Parse(ptElem.Attribute("X")?.Value ?? "0"),
                    double.Parse(ptElem.Attribute("Y")?.Value ?? "0")
                ));
            }

            return polygon;
        }
    }
}
```

*[Continue to final part...]*
