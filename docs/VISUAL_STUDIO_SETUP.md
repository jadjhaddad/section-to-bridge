# Visual Studio Project Setup Guide

**Step-by-step instructions to create the BridgeSectionTransfer solution**

---

## Prerequisites

Before starting, ensure you have:
- ✅ Visual Studio 2022 (Community, Professional, or Enterprise)
- ✅ .NET 8 SDK installed
- ✅ .NET Framework 4.8 SDK installed
- ✅ AutoCAD/Civil 3D 2025 installed (for testing)
- ✅ CSiBridge v25 installed (for testing)

---

## Step 1: Create Solution

1. **Open Visual Studio 2022**

2. **Click "Create a new project"**

3. **Search for "Blank Solution"**
   - In the search box, type: `blank solution`
   - Select: **Blank Solution**
   - Click: **Next**

4. **Configure Solution**
   - **Solution name:** `BridgeSectionTransfer`
   - **Location:** Choose your development folder (e.g., `C:\Dev\`)
   - ✅ Check: **Place solution and project in the same directory**
   - Click: **Create**

You should now see an empty solution in Solution Explorer.

---

## Step 2: Create Core Library (.NET Standard 2.0)

1. **Right-click on Solution** in Solution Explorer
   - Select: **Add → New Project...**

2. **Search for "Class Library"**
   - Type: `class library`
   - Select: **Class Library** (with C# icon, **NOT** .NET Framework)
   - Click: **Next**

3. **Configure Project**
   - **Project name:** `BridgeSectionTransfer.Core`
   - **Location:** Should auto-fill to solution folder
   - Click: **Next**

4. **Framework Selection**
   - **Framework:** `.NET 8.0 (Long Term Support)` (we'll change this to .NET Standard 2.0 next)
   - Click: **Create**

5. **Change to .NET Standard 2.0**
   - Right-click `BridgeSectionTransfer.Core` → **Edit Project File**
   - Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>netstandard2.0</TargetFramework>`
   - Add `<LangVersion>latest</LangVersion>` under TargetFramework
   - Add NuGet package for JSON: `<PackageReference Include="System.Text.Json" Version="8.0.0" />`
   - Save and close

6. **Delete Default File**
   - In Solution Explorer, delete `Class1.cs`

7. **Create Folder Structure**
   - Right-click `BridgeSectionTransfer.Core`
   - Add → New Folder → Name: `Models`
   - Repeat to create:
     - `Services`
     - `Utilities`

Your structure should look like:
```
BridgeSectionTransfer.Core/
├── Models/
├── Services/
└── Utilities/
```

---

## Step 3: Create Civil 3D Plugin (.NET Framework 4.8)

1. **Right-click on Solution**
   - Select: **Add → New Project...**

2. **Search for "Class Library (.NET Framework)"**
   - Type: `class library framework`
   - Select: **Class Library (.NET Framework)** (C# icon with older style)
   - ⚠️ **Important:** Make sure it says ".NET Framework" not just "Class Library"
   - Click: **Next**

3. **Configure Project**
   - **Project name:** `BridgeSectionTransfer.Civil3D`
   - Click: **Create**

4. **Framework Selection**
   - **Framework:** `.NET Framework 4.8`
   - Click: **Create**

5. **Add Reference to Core Library**
   - Right-click `BridgeSectionTransfer.Civil3D`
   - Select: **Add → Project Reference...**
   - ✅ Check: `BridgeSectionTransfer.Core`
   - Click: **OK**

6. **Add AutoCAD NuGet Packages**
   - Right-click `BridgeSectionTransfer.Civil3D`
   - Select: **Manage NuGet Packages...**
   - Click: **Browse** tab
   - Search: `AutoCAD.NET`
   - Install these packages:
     - `AutoCAD.NET` (version 24.0.0 for Civil 3D 2025)
     - `AutoCAD.NET.Core` (version 24.0.0)
     - `AutoCAD.NET.Model` (version 24.0.0)
   - Click: **Install** and accept licenses

7. **Delete Default File**
   - Delete `Class1.cs`

---

## Step 4: Create CSiBridge Plugin (.NET Framework 4.8)

1. **Right-click on Solution**
   - Select: **Add → New Project...**

2. **Select "Class Library (.NET Framework)"**
   - Same as Step 3
   - Click: **Next**

3. **Configure Project**
   - **Project name:** `BridgeSectionTransfer.CSiBridge`
   - **Framework:** `.NET Framework 4.8`
   - Click: **Create**

4. **Add Reference to Core Library**
   - Right-click `BridgeSectionTransfer.CSiBridge`
   - Select: **Add → Project Reference...**
   - ✅ Check: `BridgeSectionTransfer.Core`
   - Click: **OK**

5. **Add CSiBridge COM Reference**
   - Right-click `BridgeSectionTransfer.CSiBridge`
   - Select: **Add → Reference...**
   - Click: **Browse...** button (bottom right)
   - Navigate to CSiBridge installation folder:
     - Default: `C:\Program Files\Computers and Structures\CSiBridge 25\`
   - Select: `CSiBridge1.dll`
   - Click: **Add**
   - Click: **OK**

   **If you can't find CSiBridge1.dll:**
   - Click: **COM** tab instead of Browse
   - Look for: `CSiBridge 1.0 Type Library`
   - ✅ Check it
   - Click: **OK**

6. **Set Embed Interop Types = False**
   - Expand **References** under `BridgeSectionTransfer.CSiBridge`
   - Find: `CSiBridge1`
   - Right-click → **Properties**
   - Set: **Embed Interop Types** = `False`

7. **Delete Default File**
   - Delete `Class1.cs`

---

## Step 5: Create Console Application (.NET 8)

1. **Right-click on Solution**
   - Select: **Add → New Project...**

2. **Search for "Console App"**
   - Type: `console app`
   - Select: **Console App** (C# icon, **NOT** .NET Framework)
   - Click: **Next**

3. **Configure Project**
   - **Project name:** `BridgeSectionTransfer.Console`
   - Click: **Next**

4. **Framework Selection**
   - **Framework:** `.NET 8.0 (Long Term Support)`
   - Click: **Create**

5. **Add Project Reference**
   - Right-click `BridgeSectionTransfer.Console`
   - Select: **Add → Project Reference...**
   - ✅ Check:
     - `BridgeSectionTransfer.Core`
   - ⚠️ **Do NOT check `BridgeSectionTransfer.CSiBridge`** (it will use COM at runtime)
   - Click: **OK**

6. **Keep Program.cs** (we'll edit this later)

---

## Step 6: Configure Build Settings

### 6.1 Set Platform Target for Plugins

Both AutoCAD and CSiBridge are 64-bit applications.

**For BridgeSectionTransfer.Civil3D:**
1. Right-click project → **Properties**
2. Go to: **Build** tab
3. **Platform target:** `x64`
4. **Save** (Ctrl+S)

**For BridgeSectionTransfer.CSiBridge:**
1. Same steps as above
2. **Platform target:** `x64`

### 6.2 Disable "Copy Local" for AutoCAD References

**For BridgeSectionTransfer.Civil3D:**
1. Expand **Dependencies → Packages**
2. For each AutoCAD.NET reference:
   - Right-click → **Properties**
   - Set: **Copy Local** = `False`

This prevents copying AutoCAD DLLs to output folder (they're already installed with Civil 3D).

### 6.3 Set Solution Build Configuration

1. **Menu:** Build → Configuration Manager...
2. **Active solution platform:** Select `x64` (or create if doesn't exist)
3. Ensure all projects are checked to build
4. Click: **Close**

---

## Step 7: Verify Solution Structure

Your Solution Explorer should now look like this:

```
Solution 'BridgeSectionTransfer' (4 of 4 projects)
├── BridgeSectionTransfer.Core (.NET Standard 2.0)
│   ├── Dependencies
│   │   └── Packages
│   │       └── System.Text.Json 8.0.0
│   ├── Models/
│   ├── Services/
│   └── Utilities/
│
├── BridgeSectionTransfer.Civil3D (.NET Framework 4.8)
│   ├── Dependencies
│   │   ├── Packages
│   │   │   ├── AutoCAD.NET 24.0.0
│   │   │   ├── AutoCAD.NET.Core 24.0.0
│   │   │   └── AutoCAD.NET.Model 24.0.0
│   │   └── Projects
│   │       └── BridgeSectionTransfer.Core
│
├── BridgeSectionTransfer.CSiBridge (.NET Framework 4.8)
│   ├── Dependencies
│   │   ├── Assemblies
│   │   │   └── CSiBridge1
│   │   └── Projects
│   │       └── BridgeSectionTransfer.Core
│
└── BridgeSectionTransfer.Console (.NET 8.0)
    ├── Dependencies
    │   └── Projects
    │       └── BridgeSectionTransfer.Core
    └── Program.cs
```

**Note:** Console app does not reference CSiBridge project - it will load CSiBridge via COM at runtime.

---

## Step 8: Initial Build Test

1. **Build Solution**
   - Menu: **Build → Build Solution**
   - Or press: **Ctrl+Shift+B**

2. **Check Output Window**
   - Should see: `Build: 4 succeeded, 0 failed`
   - If errors, check:
     - All projects target correct frameworks
     - NuGet packages installed
     - References properly added

---

## Step 9: Configure Output Paths (Optional but Recommended)

Create a common output folder for easier testing:

**For each project:**
1. Right-click project → **Properties**
2. Go to: **Build** tab
3. **Output path:**
   - Debug: `..\..\..\bin\Debug\`
   - Release: `..\..\..\bin\Release\`

This creates a shared `bin` folder at solution level:
```
BridgeSectionTransfer/
├── bin/
│   ├── Debug/
│   │   ├── net8.0/           (Core & Console)
│   │   └── net48/            (Civil3D & CSiBridge)
│   └── Release/
└── [Projects...]
```

---

## Step 10: Add Git Ignore (Optional)

If using Git version control:

1. Right-click **Solution** → **Add → New Item...**
2. Select: **Text File**
3. Name: `.gitignore`
4. Add this content:

```gitignore
## Ignore Visual Studio temporary files, build results, and
## files generated by popular Visual Studio add-ons.

# User-specific files
*.suo
*.user
*.userosscache
*.sln.docstates

# Build results
[Dd]ebug/
[Dd]ebugPublic/
[Rr]elease/
[Rr]eleases/
x64/
x86/
[Bb]in/
[Oo]bj/

# Visual Studio cache/options
.vs/

# NuGet Packages
*.nupkg
**/packages/*
!**/packages/build/

# Others
*.log
*.dll
*.pdb
```

---

## 🎉 Setup Complete!

Your Visual Studio solution is now ready. You should have:

✅ Empty solution structure with 4 projects
✅ Correct framework targets (.NET 8 and .NET Framework 4.8)
✅ Project references configured
✅ NuGet packages installed (AutoCAD.NET)
✅ COM reference added (CSiBridge1)
✅ 64-bit platform configuration
✅ Solution builds successfully

---

## Next Steps

Now you're ready to start coding! Follow the **IMPLEMENTATION_GUIDE.md**:

1. **Phase 2:** Create model classes in `BridgeSectionTransfer.Core/Models/`
2. **Phase 3:** Implement JSON serialization in `BridgeSectionTransfer.Core/Services/`
3. **Phase 4:** Add geometry calculator
4. **Phase 5:** Build Civil 3D export command
5. **Phase 6:** Build CSiBridge importer
6. **Phase 7:** Create console application
7. **Phase 8:** Test everything!

---

## Troubleshooting

### "Project targets 'net8.0'. It cannot be referenced by a project that targets '.NETFramework,Version=v4.8'"

**Problem:** .NET Framework 4.8 cannot reference .NET 8 projects directly.

**Solution:** Core library must target .NET Standard 2.0 (which is compatible with both .NET Framework 4.8 and .NET 8).

The fix is already applied in Step 2 above. If you still see this error:
1. Open `BridgeSectionTransfer.Core.csproj`
2. Change `<TargetFramework>net8.0</TargetFramework>` to `<TargetFramework>netstandard2.0</TargetFramework>`
3. Clean and rebuild solution

### "Metadata file could not be found" for CSiBridge.dll

**Problem:** Console app cannot reference .NET Framework 4.8 project directly.

**Solution:** Console app doesn't need compile-time reference to CSiBridge plugin. Remove it:
1. Right-click Console project → Add → Project Reference
2. Uncheck `BridgeSectionTransfer.CSiBridge`
3. Keep only `BridgeSectionTransfer.Core` checked

Console will load CSiBridge via COM at runtime.

### "AutoCAD.NET package not found"

**Solution:**
- Ensure you're using .NET Framework 4.8 (not .NET 8)
- Check NuGet package source includes nuget.org
- Try: Tools → NuGet Package Manager → Package Manager Settings → Package Sources

### "Cannot add reference to .NET 8 project from .NET Framework project"

**Solution:**
- This is correct! .NET Framework projects CAN reference .NET Standard/.NET 8 Core libraries
- But .NET 8 Console app CANNOT reference .NET Framework plugins (this is OK, we don't need it)

### "CSiBridge1.dll not found"

**Solution:**
- Use COM reference instead: Add Reference → COM → CSiBridge 1.0 Type Library
- Or search entire C:\ drive for CSiBridge1.dll
- Typical locations:
  - `C:\Program Files\Computers and Structures\CSiBridge 25\`
  - `C:\Program Files (x86)\Computers and Structures\CSiBridge 25\`

### "Build fails with CS0234: The type or namespace name does not exist"

**Solution:**
- Right-click Solution → Restore NuGet Packages
- Clean Solution (Build → Clean Solution)
- Rebuild Solution (Build → Rebuild Solution)

---

## Quick Reference: File Locations

**After installation, your DLLs will be here:**

Civil 3D Plugin:
```
BridgeSectionTransfer\BridgeSectionTransfer.Civil3D\bin\x64\Debug\net48\
└── BridgeSectionTransfer.Civil3D.dll
```

CSiBridge Plugin:
```
BridgeSectionTransfer\BridgeSectionTransfer.CSiBridge\bin\x64\Debug\net48\
└── BridgeSectionTransfer.CSiBridge.dll
```

Console App:
```
BridgeSectionTransfer\BridgeSectionTransfer.Console\bin\Debug\net8.0\
└── BridgeSectionTransfer.Console.exe
```

---

**Ready to code! 🚀**

Proceed to **IMPLEMENTATION_GUIDE.md Phase 2** to start adding code.
