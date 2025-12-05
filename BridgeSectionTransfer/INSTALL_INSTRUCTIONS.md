# BridgeSectionTransfer - Installation Instructions

## Building the Projects

1. Open `BridgeSectionTransfer.sln` in Visual Studio
2. Select configuration: `Debug|x64` or `Release|x64`
3. Build → Rebuild Solution
4. Verify no errors

## Installing the Civil 3D Plugin

### Method 1: Manual Load (Quick Testing)

1. Open AutoCAD Civil 3D 2024
2. Type `NETLOAD` and press Enter
3. Browse to: `BridgeSectionTransfer.Civil3D\bin\x64\Debug\BridgeSectionTransfer.Civil3D.dll`
4. Click Open
5. Type `ExportDeckSection` to run

**Note:** You'll need to do this every time you start Civil 3D.

### Method 2: Auto-load Bundle (Recommended)

1. Build the solution in Release mode: `Build → Configuration Manager → Active solution configuration: Release`
2. Create a bundle folder structure:
   ```
   C:\ProgramData\Autodesk\ApplicationPlugins\BridgeSectionTransfer.bundle\
   └── Contents\
       ├── PackageContents.xml
       ├── BridgeSectionTransfer.Civil3D.dll
       └── BridgeSectionTransfer.Core.dll
   ```

3. Copy files:
   - Copy `PackageContents.xml` from the Civil3D project folder
   - Copy `BridgeSectionTransfer.Civil3D.dll` from `BridgeSectionTransfer.Civil3D\bin\x64\Release\`
   - Copy `BridgeSectionTransfer.Core.dll` from `BridgeSectionTransfer.Core\bin\Release\netstandard2.0\`

4. Restart Civil 3D
5. The plugin will load automatically
6. Type `ExportDeckSection` to use it

### Alternative Auto-load Location (Per-User)
Instead of `C:\ProgramData\...`, you can use:
```
C:\Users\[YourUsername]\AppData\Roaming\Autodesk\ApplicationPlugins\BridgeSectionTransfer.bundle\
```

## Using the Civil 3D Plugin

1. In Civil 3D, draw your bridge deck section using LWPOLYLINEs:
   - Draw the exterior boundary (largest polyline)
   - Draw any voids/openings inside (smaller polylines)

2. Type `ExportDeckSection` and press Enter

3. Select all polylines at once (exterior + voids)

4. The plugin will automatically:
   - Identify the largest polyline as the exterior
   - Treat others as voids
   - Calculate area and centroid

5. Enter section name (default: DeckSection_01)

6. Enter station value (default: 0.0)

7. Choose reference point:
   - **Centerline**: Uses origin (0,0)
   - **Centroid**: Uses calculated centroid
   - **Pick**: Click a point in the drawing

8. Save the JSON file

## Installing the CSiBridge Plugin

1. Build the solution
2. Copy the following files to a folder:
   - `BridgeSectionTransfer.CSiBridge\bin\x64\Release\BridgeSectionTransfer.CSiBridge.dll`
   - `BridgeSectionTransfer.Core\bin\Release\netstandard2.0\BridgeSectionTransfer.Core.dll`

3. In CSiBridge:
   - Tools → Preferences → API
   - Add the plugin DLL

**Note:** CSiBridge plugin loading varies by version. Consult CSiBridge documentation for plugin loading specific to your version.

## Troubleshooting

### "Could not load file or assembly" errors
- Ensure both the plugin DLL and Core DLL are in the same folder
- Check that you're using the correct platform (x64)
- Verify Civil 3D/CSiBridge version compatibility

### Command not found
- Verify the DLL loaded successfully (check command line for messages)
- Try typing the full command name: `ExportDeckSection`
- Case-sensitive in some contexts

### Build errors
- Ensure AutoCAD Civil 3D NuGet packages are restored
- Verify .NET Framework 4.8 is installed
- Check that CSiBridge1.dll path is correct in the CSiBridge project

## Workflow

1. **Civil 3D**: Draw section → Run `ExportDeckSection` → Save JSON
2. **Transfer**: Move JSON file to another computer if needed
3. **CSiBridge**: Run import command (to be implemented) → Load JSON → Create section

## Support

For issues, check:
- Build output for errors
- Civil 3D command line for error messages
- Ensure all dependencies are in the same folder as the plugin DLL
