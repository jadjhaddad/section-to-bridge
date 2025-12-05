using System;
using System.Windows.Forms;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

namespace BridgeSectionTransfer.CSiBridge
{
    public class Commands
    {
        /// <summary>
        /// Main entry point for importing a deck section into CSiBridge.
        /// This should be called from a CSiBridge plugin or external application.
        /// </summary>
        public static void ImportDeckSection()
        {
            try
            {
                // Step 1: Get JSON file path
                OpenFileDialog ofd = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    Title = "Select Bridge Deck Section JSON"
                };

                if (ofd.ShowDialog() != DialogResult.OK)
                {
                    MessageBox.Show("Import cancelled.", "CSiBridge Section Import", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Step 2: Load the section from JSON
                var serializer = new DeckSectionJsonSerializer();
                DeckSection section = serializer.DeserializeFromFile(ofd.FileName);

                MessageBox.Show($"Loaded section: {section.Name}\nArea: {section.Area:F4}\nVoids: {section.InteriorVoids.Count}",
                    "Section Loaded", MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Step 3: Connect to CSiBridge
                var importer = new CSiBridgeImporter();
                if (!importer.Connect())
                {
                    MessageBox.Show("Failed to connect to CSiBridge. Please ensure CSiBridge is running.",
                        "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Step 4: Get import options
                var options = new ImportOptions
                {
                    SetReferencePoint = true,
                    ClearExistingVoids = true,
                    TargetSectionName = section.Name,
                    CreateNewSection = true
                };

                // Step 5: Import the section
                bool success = importer.ImportSection(section, options);

                if (success)
                {
                    MessageBox.Show($"Successfully imported section '{section.Name}' into CSiBridge!",
                        "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to import section. Check the console for error details.",
                        "Import Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                importer.Disconnect();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during import: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                    "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
