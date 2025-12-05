using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using CSiBridge1;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

namespace BridgeSectionTransfer.CSiBridge
{
    /// <summary>
    /// Handles assembly loading for System.Text.Json dependencies
    /// </summary>
    public class DependencyResolver
    {
        private static bool _isRegistered = false;

        public static void Register()
        {
            if (!_isRegistered)
            {
                AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
                _isRegistered = true;
            }
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName requestedAssembly = new AssemblyName(args.Name);
            string pluginDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            // Handle all System.Text.Json dependencies
            string[] dependencies = new[]
            {
                "System.Text.Json",
                "System.Runtime.CompilerServices.Unsafe",
                "System.Text.Encodings.Web",
                "System.Memory",
                "System.Buffers",
                "Microsoft.Bcl.AsyncInterfaces",
                "System.Threading.Tasks.Extensions",
                "System.ValueTuple",
                "System.Numerics.Vectors"
            };

            if (dependencies.Contains(requestedAssembly.Name))
            {
                string assemblyPath = Path.Combine(pluginDir, requestedAssembly.Name + ".dll");
                if (File.Exists(assemblyPath))
                {
                    return Assembly.LoadFrom(assemblyPath);
                }
            }

            return null;
        }
    }

    /// <summary>
    /// CSiBridge Plugin implementation for importing bridge deck sections
    /// IMPORTANT: The class MUST be named "cPlugin" for CSiBridge to recognize it
    /// </summary>
    public class cPlugin : CSiBridge1.cPluginContract
    {
        private cSapModel _sapModel;
        private cPluginCallback _callback;

        /// <summary>
        /// Returns information about this plugin
        /// </summary>
        public int Info(ref string Text)
        {
            Text = "Bridge Section Transfer Plugin v1.0\n" +
                   "Import bridge deck sections from JSON files exported from Civil 3D.\n" +
                   "Author: Your Name\n" +
                   "Copyright (c) 2024";
            return 0;
        }

        /// <summary>
        /// Main entry point called by CSiBridge when plugin is loaded
        /// </summary>
        public void Main(ref cSapModel SapModel, ref cPluginCallback ISapPlugin)
        {
            // Register assembly resolver for dependencies
            DependencyResolver.Register();

            _sapModel = SapModel;
            _callback = ISapPlugin;

            try
            {
                // Show dialog to select JSON file
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Select Bridge Deck Section JSON"
                };

                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("Import cancelled.", "CSiBridge Section Import",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _callback.Finish(0);
                    return;
                }

                // Load the section from JSON
                var serializer = new DeckSectionJsonSerializer();
                DeckSection section = serializer.DeserializeFromFile(ofd.FileName);

                MessageBox.Show($"Loaded section: {section.Name}\n" +
                               $"Area: {section.Area:F4}\n" +
                               $"Voids: {section.InteriorVoids.Count}",
                    "Section Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Import the section
                bool success = ImportSection(section);

                if (success)
                {
                    MessageBox.Show($"Successfully imported section '{section.Name}' into CSiBridge!",
                        "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    _callback.Finish(0); // Success
                }
                else
                {
                    MessageBox.Show("Failed to import section. Check for errors.",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    _callback.Finish(1); // Error
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during import: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                _callback.Finish(1); // Error
            }
        }

        private bool ImportSection(DeckSection section)
        {
            try
            {
                if (_sapModel == null)
                {
                    MessageBox.Show("ERROR: SapModel is null.");
                    return false;
                }

                string sectionName = section.Name;

                // Ensure model is unlocked for editing
                int ret = _sapModel.SetModelIsLocked(false);
                if (ret != 0)
                {
                    MessageBox.Show($"WARNING: Could not unlock model (code {ret}). Continuing anyway...");
                }

                // Access Bridge Modeler interface
                var bridgeModeler = _sapModel.BridgeModeler_1;
                if (bridgeModeler == null)
                {
                    MessageBox.Show("ERROR: BridgeModeler_1 is null.");
                    return false;
                }

                // Access Deck Section interface
                var deckSection = bridgeModeler.DeckSection;
                if (deckSection == null)
                {
                    MessageBox.Show("ERROR: DeckSection is null.");
                    return false;
                }


                // Create new concrete flat slab deck section first, then convert to user-defined
                // BridgeSectionType: 1 = ConcreteFlatSlab (we'll convert this to user-defined)
                string newSectionName = sectionName;
                ret = deckSection.AddNew(ref newSectionName, 2);

                if (ret != 0)
                {
                    MessageBox.Show($"ERROR: Failed to create deck section '{sectionName}' (code {ret})");
                    return false;
                }


                // Convert to user-defined section
                string convertedName = "";
                ret = deckSection.ConvertToUserSection(newSectionName, ref convertedName);

                if (ret != 0)
                {
                    MessageBox.Show($"ERROR: Failed to convert to user section (code {ret})");
                    return false;
                }


                // Delete the original parametric section (it's no longer needed after conversion)
                ret = deckSection.Delete(newSectionName);
                if (ret != 0)
                {
                    MessageBox.Show($"WARNING: Failed to delete original parametric section '{newSectionName}' (code {ret}). It may not exist or already be deleted.");
                }
                else
                {
                }

                // Now rename the converted section to the desired name
                ret = deckSection.ChangeName(convertedName, newSectionName);
                if (ret != 0)
                {
                    MessageBox.Show($"WARNING: Failed to rename '{convertedName}' to '{newSectionName}' (code {ret}). Will continue with name '{convertedName}'");
                    // Keep using convertedName
                }
                else
                {
                    convertedName = newSectionName; // Update to the new name
                }

                // Verify the section exists by searching for it
                int numSections = 0;
                string[] sectionNames = null;
                int[] sectionTypes = null;
                ret = deckSection.GetNameList(ref numSections, ref sectionNames, ref sectionTypes);

                bool sectionFound = false;
                string finalSectionName = convertedName;

                if (ret == 0 && sectionNames != null)
                {
                    string sectionList = $"Total deck sections in model: {numSections}\n\n";

                    // Search for our section
                    for (int i = 0; i < numSections; i++)
                    {
                        sectionList += $"{i + 1}. '{sectionNames[i]}' (Type: {sectionTypes[i]})\n";

                        // Check if this matches what we're looking for
                        if (sectionNames[i] == convertedName)
                        {
                            sectionFound = true;
                            finalSectionName = sectionNames[i];
                        }
                    }

                    sectionList += $"\nSearching for: '{convertedName}'\nFound: {sectionFound}";

                    if (sectionFound)
                    {
                        sectionList += $"\nWill use section: '{finalSectionName}'";
                    }

                    MessageBox.Show(sectionList, "Section Verification");
                }

                if (!sectionFound)
                {
                    MessageBox.Show($"ERROR: Could not find section '{convertedName}' in the model after conversion and rename.", "Section Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }

                // Update convertedName to the verified name
                convertedName = finalSectionName;


                // Access User section interface for adding polygons
                var userSection = deckSection.User;
                if (userSection == null)
                {
                    MessageBox.Show("ERROR: DeckSection.User is null.");
                    return false;
                }


                // Get or create a concrete material
                string concreteMaterial = GetOrCreateConcreteMaterial();


                // Set base material
                ret = userSection.SetBaseMaterial(convertedName, concreteMaterial);
                if (ret != 0)
                {
                    MessageBox.Show($"WARNING: Failed to set base material (code {ret})");
                }


                // Get existing polygons from the converted section
                int numPolygons = 0;
                string[] polygonNames = null;
                int[] polygonTypes = null;
                int[] polygonPoints = null;

                ret = userSection.GetPolygonNameList(convertedName, ref numPolygons, ref polygonNames, ref polygonTypes, ref polygonPoints);

                string exteriorPolygonName = null;
                bool hasExterior = false;

                if (ret == 0 && polygonNames != null && numPolygons > 0)
                {
                    // Find exterior polygon (Type 1 = Solid) and delete only voids (Type 2)
                    for (int i = 0; i < numPolygons; i++)
                    {
                        if (polygonTypes[i] == 1) // Solid polygon (exterior)
                        {
                            exteriorPolygonName = polygonNames[i];
                            hasExterior = true;
                        }
                        else if (polygonTypes[i] == 2) // Void polygon - delete it
                        {
                            ret = userSection.DeletePolygon(convertedName, polygonNames[i]);
                            if (ret != 0)
                            {
                                MessageBox.Show($"WARNING: Failed to delete void polygon '{polygonNames[i]}' (code {ret})");
                            }
                            else
                            {
                            }
                        }
                    }
                }


                // Prepare exterior polygon coordinates
                int numPoints = section.ExteriorBoundary.Points.Count;
                double[] xCoords = new double[numPoints];
                double[] yCoords = new double[numPoints];
                double[] radiusCoords = new double[numPoints];

                for (int i = 0; i < numPoints; i++)
                {
                    xCoords[i] = section.ExteriorBoundary.Points[i].X;
                    yCoords[i] = section.ExteriorBoundary.Points[i].Y;
                    radiusCoords[i] = 0;
                }

                // If exterior exists, modify it with SetPolygon; otherwise create with AddNewPolygon
                if (hasExterior)
                {
                    ret = userSection.SetPolygon(
                        convertedName,
                        exteriorPolygonName,
                        concreteMaterial,     // Material
                        numPoints,
                        ref xCoords,
                        ref yCoords,
                        ref radiusCoords
                    );
                }
                else
                {
                    string polygonName = "Exterior";
                    ret = userSection.AddNewPolygon(
                        convertedName,
                        polygonName,
                        1,                    // PolygonType: 1 = Solid
                        concreteMaterial,     // Material
                        numPoints,
                        ref xCoords,
                        ref yCoords,
                        ref radiusCoords
                    );
                }

                if (ret != 0)
                {
                    MessageBox.Show($"ERROR: Failed to set exterior polygon (code {ret})");
                    return false;
                }


                // Create interior voids
                int voidIndex = 1;
                foreach (var voidPoly in section.InteriorVoids)
                {
                    int voidNumPoints = voidPoly.Points.Count;
                    double[] voidXCoords = new double[voidNumPoints];
                    double[] voidYCoords = new double[voidNumPoints];
                    double[] voidRadiusCoords = new double[voidNumPoints];

                    for (int i = 0; i < voidNumPoints; i++)
                    {
                        voidXCoords[i] = voidPoly.Points[i].X;
                        voidYCoords[i] = voidPoly.Points[i].Y;
                        voidRadiusCoords[i] = 0;
                    }

                    string voidPolygonName = $"Void_{voidIndex}";
                    // PolygonType: 2 = Opening/Void
                    ret = userSection.AddNewPolygon(
                        convertedName,
                        voidPolygonName,
                        2,                    // PolygonType: 2 = Opening
                        "",                   // Empty material for void
                        voidNumPoints,
                        ref voidXCoords,
                        ref voidYCoords,
                        ref voidRadiusCoords
                    );

                    if (ret != 0)
                    {
                        MessageBox.Show($"WARNING: Failed to create void '{voidPolygonName}' (code {ret})");
                    }
                    else
                    {
                    }

                    voidIndex++;
                }


                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"ERROR during import: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Gets an existing concrete material or tries common default names
        /// </summary>
        private string GetOrCreateConcreteMaterial()
        {
            try
            {
                // Try common CSiBridge default material names
                string[] commonNames = { "4000Psi", "C4000", "CONC", "Concrete", "4KSI", "C30" };

                int numMaterials = 0;
                string[] materialNames = null;

                int ret = _sapModel.PropMaterial.GetNameList(ref numMaterials, ref materialNames);

                if (ret == 0 && materialNames != null)
                {
                    // Check if any common concrete material exists
                    foreach (string commonName in commonNames)
                    {
                        foreach (string existingName in materialNames)
                        {
                            if (existingName != null && existingName.Equals(commonName, StringComparison.OrdinalIgnoreCase))
                            {
                                return existingName;
                            }
                        }
                    }

                    // Use first material found as fallback
                    if (materialNames.Length > 0 && materialNames[0] != null)
                    {
                        return materialNames[0];
                    }
                }

                // If no materials exist, try to create a basic concrete material
                ret = _sapModel.PropMaterial.SetMaterial("CONC", CSiBridge1.eMatType.Concrete);
                if (ret == 0)
                {
                    return "CONC";
                }

                // Last resort: return empty string and hope CSiBridge handles it
                return "";
            }
            catch
            {
                // If material lookup fails, return empty string
                return "";
            }
        }
    }
}
