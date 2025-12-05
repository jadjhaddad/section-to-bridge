# CSiBridge Centerline/Cut Line Database Tables API

## Overview

This document describes how to add **centerlines** and **cut lines** to user-defined bridge deck sections in CSiBridge using the **Database Tables API**. This is currently the **only method** available for defining centerlines/cut lines for user-defined deck sections.

## Important Notes

- The Database Tables API (`cDatabaseTables` interface) is the **required method** for adding centerlines and cut lines to user-defined bridge deck sections
- Reference lines added via `SetRefLine()` are **NOT** the same as bridge section centerlines/cut lines
- Bridge section centerlines are stored in specific database tables that must be edited programmatically

---

## C# Implementation Example

```csharp
using CSiAPIv1;

/// <summary>
/// Adds centerlines and cut lines to a bridge deck section using the Database Tables API
/// </summary>
/// <param name="mySapModel">The CSiBridge SapModel object</param>
public void AddDeckSectionCenterlines(cSapModel mySapModel)
{
    int ret = 0;

    // Step 1: Read all available tables and find the centerline table
    int numberTables = 0;
    string[] tableKey = null;
    string[] tableName = null;
    int[] importType = null;
    bool[] isEmpty = null;

    ret = mySapModel.DatabaseTables.GetAllTables(
        ref numberTables,
        ref tableKey,
        ref tableName,
        ref importType,
        ref isEmpty
    );

    // Step 2: Find the index of the "Section Designer Properties 34 - Bridge Section Cutline" table
    int tableIndex = Array.IndexOf(tableName, "Section Designer Properties 34 - Bridge Section Cutline");

    if (tableIndex == -1)
    {
        throw new Exception("Bridge Section Cutline table not found");
    }

    // Step 3: Get the import type for the found table
    int importTypeValue = importType[tableIndex];
    string targetTableKey = tableKey[tableIndex];

    // Step 4: Read all field information from the table
    int tableVersion = 0;
    int numberFields = 0;
    string[] fieldKey = null;
    string[] fieldName = null;
    string[] description = null;
    string[] unitsString = null;
    bool[] isImportable = null;

    ret = mySapModel.DatabaseTables.GetAllFieldsInTable(
        targetTableKey,
        ref tableVersion,
        ref numberFields,
        ref fieldKey,
        ref fieldName,
        ref description,
        ref unitsString,
        ref isImportable
    );

    // Step 5: Get the current table data for editing
    int tableVersion2 = 0;
    string[] fieldsKeysIncluded = null;
    int numberRecords = 0;
    string[] tableData = null;

    ret = mySapModel.DatabaseTables.GetTableForEditingArray(
        targetTableKey,
        "All",                      // Group name - use "All" to get all records
        ref tableVersion2,
        ref fieldsKeysIncluded,
        ref numberRecords,
        ref tableData
    );

    // Step 6: Modify or add your centerline/cut line data
    // IMPORTANT: You should first create a sample model in CSiBridge with the centerlines
    // you want, then use this code to read the table structure and replicate it

    // Example: Add a new centerline record
    // The tableData array is a 1D array where data is stored row-by-row
    // Each row contains values for all fields in fieldsKeysIncluded

    // For demonstration, we'll just show how to work with the existing data
    // In practice, you would:
    // 1. Parse the existing tableData based on fieldsKeysIncluded
    // 2. Add new rows with your centerline definitions
    // 3. Rebuild the tableData array

    // Step 7: Set the modified table data back to CSiBridge
    ret = mySapModel.DatabaseTables.SetTableForEditingArray(
        targetTableKey,
        ref tableVersion2,
        ref fieldsKeysIncluded,
        numberRecords,
        ref tableData
    );

    // Step 8: Apply the changes and check for errors
    int numFatalErrors = 0;
    int numErrorMsgs = 0;
    int numWarnMsgs = 0;
    int numInfoMsgs = 0;
    string importLog = "";

    ret = mySapModel.DatabaseTables.ApplyEditedTables(
        true,                       // FillImportLog - set to true to get detailed log
        ref numFatalErrors,
        ref numErrorMsgs,
        ref numWarnMsgs,
        ref numInfoMsgs,
        ref importLog
    );

    // Step 9: Check for errors and warnings
    if (numFatalErrors > 0 || numErrorMsgs > 0)
    {
        Console.WriteLine($"Errors occurred during table import:");
        Console.WriteLine($"  Fatal Errors: {numFatalErrors}");
        Console.WriteLine($"  Error Messages: {numErrorMsgs}");
        Console.WriteLine($"  Warnings: {numWarnMsgs}");
        Console.WriteLine($"  Info Messages: {numInfoMsgs}");
        Console.WriteLine($"\nImport Log:\n{importLog}");
        throw new Exception("Failed to apply centerline data");
    }
    else if (numWarnMsgs > 0)
    {
        Console.WriteLine($"Centerlines applied with {numWarnMsgs} warnings:");
        Console.WriteLine(importLog);
    }
    else
    {
        Console.WriteLine("Centerlines successfully applied!");
    }
}
```

---

## Step-by-Step Explanation

### Step 1: Get All Available Tables

```csharp
ret = mySapModel.DatabaseTables.GetAllTables(
    ref numberTables,    // Output: Total number of tables
    ref tableKey,        // Output: Array of table keys (internal identifiers)
    ref tableName,       // Output: Array of table names (human-readable)
    ref importType,      // Output: Array of import types
    ref isEmpty          // Output: Array indicating if table is empty
);
```

This retrieves all database tables available in CSiBridge, including model data, section properties, loads, analysis results, etc.

### Step 2: Find the Centerline Table

```csharp
int tableIndex = Array.IndexOf(tableName, "Section Designer Properties 34 - Bridge Section Cutline");
```

The specific table name is:
- **"Section Designer Properties 34 - Bridge Section Cutline"**

This table stores centerline and cut line definitions for bridge deck sections.

### Step 3: Get Table Metadata

```csharp
ret = mySapModel.DatabaseTables.GetAllFieldsInTable(
    targetTableKey,       // Input: The table key from step 2
    ref tableVersion,     // Output: Table schema version
    ref numberFields,     // Output: Number of fields/columns
    ref fieldKey,         // Output: Field keys (internal identifiers)
    ref fieldName,        // Output: Field names (human-readable)
    ref description,      // Output: Field descriptions
    ref unitsString,      // Output: Units for each field
    ref isImportable      // Output: Whether field can be imported
);
```

This provides the schema/structure of the table so you know what fields are available.

### Step 4: Get Existing Table Data

```csharp
ret = mySapModel.DatabaseTables.GetTableForEditingArray(
    targetTableKey,              // Input: Table to retrieve
    "All",                       // Input: Group name ("All" for all records)
    ref tableVersion2,           // Output: Table version
    ref fieldsKeysIncluded,      // Output: Which fields are included
    ref numberRecords,           // Output: Number of data rows
    ref tableData                // Output: 1D array of all data
);
```

**Important:** The `tableData` array is a **1D flattened array** where:
- Data is stored row-by-row
- Each row contains values for all fields in `fieldsKeysIncluded`
- Example: If you have 3 fields and 2 records:
  ```
  tableData = [Field1_Row1, Field2_Row1, Field3_Row1, Field1_Row2, Field2_Row2, Field3_Row2]
  ```

### Step 5: Modify the Data

To add new centerlines:

```csharp
// Parse existing data
int numFields = fieldsKeysIncluded.Length;
List<string> newTableData = new List<string>(tableData);

// Add a new centerline record
// You need to know the field structure - best way is to create an example in CSiBridge first
// Example fields might be: SectionName, LineType, X1, Y1, X2, Y2, etc.
newTableData.Add("MyDeckSection");    // Section name
newTableData.Add("Centerline");       // Line type
newTableData.Add("0.0");              // X1 coordinate
newTableData.Add("-5.0");             // Y1 coordinate
newTableData.Add("0.0");              // X2 coordinate
newTableData.Add("5.0");              // Y2 coordinate
// ... add remaining fields

tableData = newTableData.ToArray();
numberRecords++;
```

### Step 6: Apply the Changes

```csharp
// First, set the modified data
ret = mySapModel.DatabaseTables.SetTableForEditingArray(
    targetTableKey,
    ref tableVersion2,
    ref fieldsKeysIncluded,
    numberRecords,
    ref tableData
);

// Then apply all edited tables to the model
ret = mySapModel.DatabaseTables.ApplyEditedTables(
    true,                    // Generate detailed import log
    ref numFatalErrors,
    ref numErrorMsgs,
    ref numWarnMsgs,
    ref numInfoMsgs,
    ref importLog
);
```

---

## Best Practice: Template-Based Approach

The **recommended approach** is to:

1. **Create a template model** in CSiBridge with sample centerlines/cut lines
2. **Export the table** to see the exact structure
3. **Use that structure** to populate your data programmatically

### Example: Exporting Template Data

```csharp
public void ExportCenterlineTableToCSV(cSapModel mySapModel, string csvFilePath)
{
    int ret = 0;
    int numberTables = 0;
    string[] tableKey = null;
    string[] tableName = null;
    int[] importType = null;
    bool[] isEmpty = null;

    // Get all tables
    ret = mySapModel.DatabaseTables.GetAllTables(
        ref numberTables,
        ref tableKey,
        ref tableName,
        ref importType,
        ref isEmpty
    );

    // Find centerline table
    int tableIndex = Array.IndexOf(tableName, "Section Designer Properties 34 - Bridge Section Cutline");

    if (tableIndex == -1)
    {
        throw new Exception("Bridge Section Cutline table not found");
    }

    string targetTableKey = tableKey[tableIndex];
    int tableVersion = 0;

    // Export to CSV file
    ret = mySapModel.DatabaseTables.GetTableForEditingCSVFile(
        targetTableKey,
        "All",
        ref tableVersion,
        csvFilePath,
        ","                     // Separator character
    );

    Console.WriteLine($"Table exported to: {csvFilePath}");
}
```

Then open the CSV file to see the exact field structure and sample data.

---

## Alternative: CSV Import Method

Instead of working with arrays, you can use CSV strings:

```csharp
public void ImportCenterlineFromCSV(cSapModel mySapModel, string csvContent)
{
    int ret = 0;
    int numberTables = 0;
    string[] tableKey = null;
    string[] tableName = null;
    int[] importType = null;
    bool[] isEmpty = null;

    ret = mySapModel.DatabaseTables.GetAllTables(
        ref numberTables,
        ref tableKey,
        ref tableName,
        ref importType,
        ref isEmpty
    );

    int tableIndex = Array.IndexOf(tableName, "Section Designer Properties 34 - Bridge Section Cutline");
    string targetTableKey = tableKey[tableIndex];

    int tableVersion = 0;

    // Set table data from CSV string
    ret = mySapModel.DatabaseTables.SetTableForEditingCSVString(
        targetTableKey,
        ref tableVersion,
        ref csvContent,
        ","
    );

    // Apply changes
    int numFatalErrors = 0;
    int numErrorMsgs = 0;
    int numWarnMsgs = 0;
    int numInfoMsgs = 0;
    string importLog = "";

    ret = mySapModel.DatabaseTables.ApplyEditedTables(
        true,
        ref numFatalErrors,
        ref numErrorMsgs,
        ref numWarnMsgs,
        ref numInfoMsgs,
        ref importLog
    );

    if (numFatalErrors > 0 || numErrorMsgs > 0)
    {
        throw new Exception($"Import failed:\n{importLog}");
    }
}
```

---

## Database Tables API Reference

### Main Methods

| Method | Purpose |
|--------|---------|
| `GetAllTables()` | Retrieve list of all database tables |
| `GetAllFieldsInTable()` | Get field schema for a specific table |
| `GetTableForEditingArray()` | Retrieve table data as array |
| `GetTableForEditingCSVFile()` | Export table to CSV file |
| `GetTableForEditingCSVString()` | Get table as CSV string |
| `SetTableForEditingArray()` | Modify table data using array |
| `SetTableForEditingCSVFile()` | Import table from CSV file |
| `SetTableForEditingCSVString()` | Import table from CSV string |
| `ApplyEditedTables()` | Apply all pending table edits to model |
| `CancelTableEditing()` | Cancel pending table edits |

### Return Codes

All methods return an integer return code:
- `0` = Success
- `Non-zero` = Error (check CSiBridge documentation for specific codes)

---

## Common Table Names for Bridge Sections

Here are other related tables you might need:

- `"Section Designer Properties 01 - General"` - General section properties
- `"Section Designer Properties 02 - Concrete Shapes"` - Concrete shape definitions
- `"Section Designer Properties 34 - Bridge Section Cutline"` - **Centerlines/cut lines**
- `"Section Designer Properties 35 - Bridge Section Variable Location"` - Variable location definitions

---

## Complete Example: Adding Centerline to Existing Section

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using CSiAPIv1;

public class CenterlineManager
{
    private cSapModel _model;

    public CenterlineManager(cSapModel model)
    {
        _model = model;
    }

    /// <summary>
    /// Adds a vertical centerline to a bridge deck section
    /// </summary>
    public void AddVerticalCenterline(string sectionName, double xPosition, double yStart, double yEnd)
    {
        // First, export current table to understand structure
        string csvContent = ExportCenterlineTableToCSV();

        Console.WriteLine("Current centerline table:");
        Console.WriteLine(csvContent);
        Console.WriteLine();

        // Parse and modify CSV (simplified example)
        // In practice, you'd use a proper CSV parser
        List<string> lines = csvContent.Split('\n').ToList();

        // Add new row (example - adjust fields based on actual table structure)
        string newLine = $"{sectionName},Centerline,{xPosition},{yStart},{xPosition},{yEnd}";
        lines.Add(newLine);

        csvContent = string.Join("\n", lines);

        // Import modified data
        ImportCenterlineFromCSV(csvContent);

        Console.WriteLine($"Centerline added to section '{sectionName}' at X={xPosition}");
    }

    private string ExportCenterlineTableToCSV()
    {
        string tableKey = FindCenterlineTableKey();

        int tableVersion = 0;
        string csvString = "";

        int ret = _model.DatabaseTables.GetTableForEditingCSVString(
            tableKey,
            "All",
            ref tableVersion,
            ref csvString,
            ","
        );

        if (ret != 0)
        {
            throw new Exception($"Failed to export centerline table (code {ret})");
        }

        return csvString;
    }

    private void ImportCenterlineFromCSV(string csvContent)
    {
        string tableKey = FindCenterlineTableKey();
        int tableVersion = 0;

        int ret = _model.DatabaseTables.SetTableForEditingCSVString(
            tableKey,
            ref tableVersion,
            ref csvContent,
            ","
        );

        if (ret != 0)
        {
            throw new Exception($"Failed to set centerline table data (code {ret})");
        }

        // Apply changes
        int numFatalErrors = 0;
        int numErrorMsgs = 0;
        int numWarnMsgs = 0;
        int numInfoMsgs = 0;
        string importLog = "";

        ret = _model.DatabaseTables.ApplyEditedTables(
            true,
            ref numFatalErrors,
            ref numErrorMsgs,
            ref numWarnMsgs,
            ref numInfoMsgs,
            ref importLog
        );

        if (numFatalErrors > 0 || numErrorMsgs > 0)
        {
            throw new Exception($"Failed to apply centerline changes:\n{importLog}");
        }
    }

    private string FindCenterlineTableKey()
    {
        int numberTables = 0;
        string[] tableKey = null;
        string[] tableName = null;
        int[] importType = null;
        bool[] isEmpty = null;

        int ret = _model.DatabaseTables.GetAllTables(
            ref numberTables,
            ref tableKey,
            ref tableName,
            ref importType,
            ref isEmpty
        );

        if (ret != 0)
        {
            throw new Exception($"Failed to get database tables (code {ret})");
        }

        int tableIndex = Array.IndexOf(tableName, "Section Designer Properties 34 - Bridge Section Cutline");

        if (tableIndex == -1)
        {
            throw new Exception("Bridge Section Cutline table not found. Ensure CSiBridge is loaded with a bridge model.");
        }

        return tableKey[tableIndex];
    }
}
```

---

## Integration with BridgeSectionTransfer

To integrate this into the current project:

1. **Add to `CSiBridgeImporter` class**:
   ```csharp
   private void ImportCenterlines(DeckSection section, string sectionName)
   {
       if (section.ReferenceLines == null || section.ReferenceLines.Count == 0)
       {
           return;
       }

       var centerlineManager = new CenterlineManager(_model);

       foreach (var refLine in section.ReferenceLines)
       {
           if (refLine.Type == ReferenceLineType.Centerline)
           {
               centerlineManager.AddVerticalCenterline(
                   sectionName,
                   refLine.StartPoint.X,
                   refLine.StartPoint.Y,
                   refLine.EndPoint.Y
               );
           }
       }
   }
   ```

2. **Call in `ImportSection()` method**:
   ```csharp
   // After importing polygons
   ImportCenterlines(section, sectionName);
   ```

---

## Notes and Warnings

1. **Table Structure**: The exact field structure may vary by CSiBridge version. Always export a sample table first.

2. **Error Handling**: Always check return codes and the import log for errors.

3. **Template Approach**: Create sample sections in CSiBridge GUI first, then use the API to replicate the structure.

4. **Ref Parameters**: C# requires `ref` keyword for all out/in-out parameters.

5. **Array Indexing**: The `tableData` array uses 1D indexing. Calculate positions as: `index = rowIndex * numberOfFields + fieldIndex`

6. **Version Compatibility**: This API is available in CSiBridge v25. Check compatibility for other versions.

---

## See Also

- [IMPLEMENTATION_PLAN.md](./IMPLEMENTATION_PLAN.md) - Overall project implementation plan
- CSiBridge API Documentation - cDatabaseTables interface
- Section Designer documentation in CSiBridge
