# Mid-Surface Lines (Plate Centerlines) in Box Girder Sections

## Overview

Mid-surface lines (also called plate centerlines) are reference lines that represent the center plane of each structural plate element in a box girder cross-section. These lines are critical for finite element modeling, section property calculations, and structural detailing.

---

## What Are Mid-Surface Lines?

In box girder bridge sections, the structural components (top slab, bottom slab, webs) are modeled as plate/shell elements. The **mid-surface** is the plane located at the center of each plate's thickness.

| Element | Mid-Surface Line Description |
|---------|------------------------------|
| **Top Slab/Deck** | Horizontal line at mid-thickness of top slab |
| **Bottom Slab** | Horizontal line at mid-thickness of bottom slab |
| **Webs** | Vertical or inclined lines at mid-thickness of each web |

---

## Visual Representation

```
                    Deck Top Surface
        ════════════════════════════════════════════
        ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   ← TOP SLAB MID-SURFACE LINE
        ════════════════════════════════════════════
       ║                                            ║
       ║                                            ║
     ← ║ WEB                                    WEB ║ →
       ║ MID-SURFACE                    MID-SURFACE ║
       ║ LINE                                  LINE ║
       ║                                            ║
       ║                                            ║
        ════════════════════════════════════════════
        ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   ← BOTTOM SLAB MID-SURFACE LINE
        ════════════════════════════════════════════
                    Deck Bottom Surface
```

### Multi-Cell Box Girder Example

```
        ══════════════════════════════════════════════════════
        ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   ← Top Slab Mid-Surface
        ══════════════════════════════════════════════════════
       ║              ║              ║              ║
       ║   Cell 1     ║    Cell 2    ║    Cell 3    ║
       ║              ║              ║              ║
       ↑              ↑              ↑              ↑
    Exterior       Interior      Interior       Exterior
    Web            Web           Web            Web
    Mid-Surface    Mid-Surface   Mid-Surface    Mid-Surface
       ║              ║              ║              ║
        ══════════════════════════════════════════════════════
        ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─ ─   ← Bottom Slab Mid-Surface
        ══════════════════════════════════════════════════════
```

---

## Purpose and Applications

### 1. Shell Element Modeling (FEA)

When CSiBridge or other structural analysis software creates a **shell model** (as opposed to a spine/beam model), shell elements are placed at the mid-surface of each plate component.

- Shell elements have no physical thickness in the geometric model
- The thickness is assigned as a property to the element
- Element nodes are located on the mid-surface
- Stresses are calculated at the mid-surface and extrapolated to top/bottom faces

**Reference:** [CSiBridge Features](https://www.csiamerica.com/products/csibridge/features)

### 2. Section Property Calculations

The position of mid-surface lines affects:

- **Moment of Inertia**: Calculated using parallel axis theorem from mid-surfaces
- **Centroid Location**: Composite section centroid depends on mid-surface positions
- **Composite Action**: If mid-surfaces are incorrectly positioned, composite behavior is compromised

> "The center line of deck slab coincides with the neutral axis of the section. Hence, the contribution of deck slab to the flexural stiffness of the section will be negligible."
>
> — [ResearchGate: Different Techniques for Modeling Post-Tensioned Concrete Box-Girder Bridges](https://www.researchgate.net/publication/324721207_DIFFERENT_TECHNIQUES_FOR_THE_MODELING_OF_POST-TENSIONED_CONCRETE_BOX-GIRDER_BRIDGES)

### 3. Structural Detailing

Design manuals reference web centerlines for detailing requirements:

- **Flange Extensions**: "Bottom flange edges should extend at least 1½ in. beyond the web centerline"
- **Weld Locations**: Fillet welds are typically placed at web-to-flange junctions
- **Stiffener Placement**: Referenced from web centerlines

**Reference:** [Indiana DOT Design Manual - Steel Structures](https://www.in.gov/dot/div/contracts/design/Part%204/Chapter%20407%20-%20Steel%20Structure.pdf)

### 4. Connection Modeling

Frame-to-shell connections require proper node positioning:

- Girder frame elements connect at web mid-surface
- Slab shell elements connect at slab mid-surface
- Body constraints or rigid links bridge the offset between elements

**Reference:** [Caltrans Bridge Design Practices - Chapter 4](https://dot.ca.gov/-/media/dot-media/programs/engineering/documents/bridge-design-practices/202210-bdp-chapter-4structuralmodelingandanalysis-a11y.pdf)

---

## Calculating Mid-Surface Positions

### For Horizontal Slabs

```
Mid-Surface Y = Top Surface Y - (Slab Thickness / 2)
```

**Example - Top Slab:**
- Top of deck elevation: Y = 2.500 m
- Slab thickness: t = 0.250 m
- Mid-surface Y = 2.500 - (0.250 / 2) = **2.375 m**

**Example - Bottom Slab:**
- Bottom of section elevation: Y = 0.000 m
- Slab thickness: t = 0.200 m
- Mid-surface Y = 0.000 + (0.200 / 2) = **0.100 m**

### For Vertical Webs

```
Mid-Surface X = Outer Face X + (Web Thickness / 2)  [for left web]
Mid-Surface X = Outer Face X - (Web Thickness / 2)  [for right web]
```

**Example - Left Exterior Web:**
- Outer face X: -6.000 m
- Web thickness: t = 0.400 m
- Mid-surface X = -6.000 + (0.400 / 2) = **-5.800 m**

### For Inclined Webs

For webs at an angle θ from vertical:

```
Mid-Surface Offset (perpendicular) = Web Thickness / 2
ΔX = (Web Thickness / 2) × cos(θ)
ΔY = (Web Thickness / 2) × sin(θ)
```

---

## CSiBridge Model Types

CSiBridge offers three structural model types that use mid-surfaces differently:

| Model Type | Mid-Surface Usage |
|------------|-------------------|
| **Spine Model** | Single line at section centroid; mid-surfaces not explicitly modeled |
| **Shell Model** | Shell elements placed at mid-surface of each plate element |
| **Solid Model** | 3D solid elements; mid-surfaces used for mesh generation |

### When to Use Shell Models

Shell models are recommended when:
- Analyzing **curved bridges** where warping stresses are important
- Detailed **local stress analysis** is required
- Modeling **steel girders** with slender webs
- Investigating **distortional behavior** of box sections

> "For curved steel girder bridges it is recommended that the steel girders be modelled as shell elements so that warping stresses can be captured."
>
> — [CSiBridge Documentation](https://www.csiamerica.com/products/csibridge/features)

---

## CSiBridge API: Important Distinction

### Two Different Concepts

There is a critical distinction between **deck section definitions** and **shell area objects** in CSiBridge:

| Concept | Description | API | Purpose |
|---------|-------------|-----|---------|
| **Deck Section Definition** | Polygons defining cross-section shape for the bridge modeler | `BridgeModeler.DeckSection.User.AddNewPolygon()` | Define section geometry for bridge object |
| **Shell Area Objects** | 2D finite elements placed in 3D model space | `SapModel.AreaObj.AddByCoord()` | Create manual shell finite element model |

**These are completely separate systems!**

### User-Defined Deck Section Limitations

CSiBridge **user-defined deck sections**:

- Only store polygon geometry (outer boundary + voids)
- **Do NOT have fields for mid-surface lines or plate thicknesses**
- Can only generate **spine models** (frame elements) automatically
- **Cannot automatically generate shell models**

For **parametric sections** (standard box girder, I-girder, etc.), CSiBridge internally knows the plate thicknesses and can auto-generate shell models with elements at mid-surface positions. But for user-defined sections, CSiBridge doesn't have this information.

### Available APIs for Each Approach

#### Approach A: User-Defined Deck Section (Spine Model Only)

```csharp
// Define section polygons - for bridge modeler spine model
ret = bridgeModeler.DeckSection.User.AddNewPolygon(
    sectionName,
    numPoints,
    ref xCoords,
    ref yCoords,
    polygonType  // 1=Solid, 2=Opening
);

// Set insertion/reference point
ret = bridgeModeler.DeckSection.User.SetInsertionPoint(
    sectionName,
    refX,
    refY
);
```

**Result:** Creates a deck section that can only be used with spine (frame element) models.

#### Approach B: Manual Shell Model (Not Using Bridge Modeler)

```csharp
// Step 1: Define shell section properties
ret = model.PropArea.SetShell_1(
    "TopSlab_Shell",  // Property name
    1,                // ShellType (1=Shell-Thin)
    true,             // IncludeDrillingDOF
    "Concrete",       // Material
    0,                // Material angle
    0.250,            // Membrane thickness
    0.250             // Bending thickness
);

// Step 2: Create area objects at mid-surface positions
double[] x = { -6.0, 6.0, 6.0, -6.0 };
double[] y = { 2.375, 2.375, 2.375, 2.375 };  // Mid-surface elevation
double[] z = { 0, 0, 10, 10 };                 // Along bridge length
string areaName = "";

ret = model.AreaObj.AddByCoord(4, ref x, ref y, ref z, ref areaName, "TopSlab_Shell");

// Step 3: Assign property (if not assigned during creation)
ret = model.AreaObj.SetProperty(areaName, "TopSlab_Shell");
```

**Result:** Creates standalone shell elements - completely separate from bridge modeler.

### API Reference

| Task | API Method | Documentation |
|------|-----------|---------------|
| Add polygon to user-defined section | `BridgeModeler.DeckSection.User.AddNewPolygon()` | CSiBridge API |
| Set section insertion point | `BridgeModeler.DeckSection.User.SetInsertionPoint()` | CSiBridge API |
| Define shell property | `SapModel.PropArea.SetShell_1()` | [SetShell](http://docs.csiamerica.com/help-files/common-api(from-sap-and-csibridge)/SAP2000_API_Fuctions/Obsolete_Functions/SetShell.htm) |
| Create area object by coordinates | `SapModel.AreaObj.AddByCoord()` | [cAreaObj.AddByCoord](https://docs.csiamerica.com/help-files/etabs-api-2016/html/2ae59c04-cac4-236c-f396-d9532759e4d9.htm) |
| Assign property to area | `SapModel.AreaObj.SetProperty()` | [cAreaObj.SetProperty](https://docs.csiamerica.com/help-files/etabs-api-2016/html/dbf7c729-66b9-6244-03e1-b688ad9301db.htm) |

### Shell Type Values

| Value | Type | Description |
|-------|------|-------------|
| 1 | Shell - Thin | Standard thin shell (Kirchhoff formulation) |
| 2 | Shell - Thick | Thick shell (Mindlin/Reissner formulation) |
| 3 | Plate - Thin | Bending only, no membrane |
| 4 | Plate - Thick | Bending only, thick formulation |
| 5 | Membrane | In-plane forces only, no bending |
| 6 | Shell - Layered | Composite/layered shell sections |

### Practical Options for Shell Models with Custom Sections

Given the CSiBridge limitations, here are the available options:

| Option | Description | Pros | Cons |
|--------|-------------|------|------|
| **1. Accept spine model** | Use user-defined section with bridge modeler | Simple, uses bridge modeler features | No local stress analysis |
| **2. Manual shell model** | Create shell elements via `AreaObj.AddByCoord()` | Full shell analysis capability | Doesn't use bridge modeler; manual work |
| **3. Export mid-surface as reference** | Draw mid-surfaces in Civil 3D for user reference | Helps manual modeling | Still requires manual shell creation |
| **4. Use parametric section** | If section fits standard template, use parametric | Auto shell model generation | Limited to standard shapes |

### Recommendation

For most workflows:

1. **Export gross section polygons** → User-defined deck section → Spine model analysis
2. **Export mid-surface lines as reference geometry** → User manually creates shell elements if needed

This tool (BridgeSectionTransfer) focuses on **Option 1** with support for **Option 3** as reference data.

---

## Implementation in BridgeSectionTransfer

### Planned Feature: Mid-Surface Line Generation

The tool can automatically calculate and export mid-surface lines for:

1. **Visualization in Civil 3D**
   - Draw dashed lines at mid-surface positions
   - Help users verify section geometry

2. **Shell Model Preparation**
   - Export mid-surface polylines for direct use in FE modeling
   - Include in JSON export for CSiBridge import

3. **Section Property Verification**
   - Calculate properties based on mid-surface geometry
   - Compare with gross section properties

### Data Structure

```csharp
public class MidSurfaceLine
{
    public string ElementName { get; set; }      // "TopSlab", "BottomSlab", "Web_1", etc.
    public ElementType Type { get; set; }        // Slab, Web
    public List<Point2D> Points { get; set; }    // Line geometry
    public double Thickness { get; set; }        // Plate thickness
    public double Offset { get; set; }           // Offset from reference
}

public enum ElementType
{
    TopSlab,
    BottomSlab,
    Web,
    Cantilever
}
```

---

## References

### Bridge Engineering & Modeling

1. **CSiBridge Features** - [https://www.csiamerica.com/products/csibridge/features](https://www.csiamerica.com/products/csibridge/features)

2. **Different Techniques for Modeling Post-Tensioned Concrete Box-Girder Bridges** - [ResearchGate](https://www.researchgate.net/publication/324721207_DIFFERENT_TECHNIQUES_FOR_THE_MODELING_OF_POST-TENSIONED_CONCRETE_BOX-GIRDER_BRIDGES)

3. **Caltrans Bridge Design Practices - Chapter 4: Structural Modeling and Analysis** - [California DOT](https://dot.ca.gov/-/media/dot-media/programs/engineering/documents/bridge-design-practices/202210-bdp-chapter-4structuralmodelingandanalysis-a11y.pdf)

4. **Indiana DOT Design Manual - Chapter 407: Steel Structures** - [Indiana DOT](https://www.in.gov/dot/div/contracts/design/Part%204/Chapter%20407%20-%20Steel%20Structure.pdf)

5. **FHWA LRFD Steel Girder Superstructure Design Example** - [Federal Highway Administration](https://www.fhwa.dot.gov/bridge/lrfd/us_ds3.cfm)

6. **CSiBridge Box Girder Advanced Form** - [CSI Documentation](https://docs.csiamerica.com/help-files/csibridge/Components_tab/Superstructure_Item_panel/Deck_Section_Types/Advanced.htm)

### CSiBridge API Documentation

7. **cAreaObj.AddByCoord Method** - [CSI API Docs](https://docs.csiamerica.com/help-files/etabs-api-2016/html/2ae59c04-cac4-236c-f396-d9532759e4d9.htm)

8. **cAreaObj.SetProperty Method** - [CSI API Docs](https://docs.csiamerica.com/help-files/etabs-api-2016/html/dbf7c729-66b9-6244-03e1-b688ad9301db.htm)

9. **PropArea.SetShell Method** - [CSI API Docs](http://docs.csiamerica.com/help-files/common-api(from-sap-and-csibridge)/SAP2000_API_Fuctions/Obsolete_Functions/SetShell.htm)

10. **Update Bridge Structural Model Form** - [CSI Documentation](https://docs.csiamerica.com/help-files/csibridge/Bridge_tab/Update/Update_Bridge_Structural_Model.htm)

11. **User-Defined Bridge Section Data Form** - [CSI Documentation](https://docs.csiamerica.com/help-files/csibridge/Components_tab/Superstructure_Item_panel/User-Defined_Bridge_Section_Data_Form.htm)

12. **Modeling Decks Not Available Through Bridge Modeler** - [CSI Knowledge Base](https://web.wiki.csiamerica.com/wiki/spaces/kb/pages/2002460/Modeling+decks+not+available+through+the+bridge+modeler)
