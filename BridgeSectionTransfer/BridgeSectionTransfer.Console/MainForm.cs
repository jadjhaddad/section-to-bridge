using System;
using System.IO;
using System.Windows.Forms;
using BridgeSectionTransfer.Core.Models;
using BridgeSectionTransfer.Core.Services;

namespace BridgeSectionTransfer.Console
{
    public partial class MainForm : Form
    {
        private string _tempJsonPath = "";

        public MainForm()
        {
            InitializeComponent();

            // Use a consistent temp file location
            string tempDir = Path.Combine(Path.GetTempPath(), "BridgeSectionTransfer");
            if (!Directory.Exists(tempDir))
            {
                Directory.CreateDirectory(tempDir);
            }
            _tempJsonPath = Path.Combine(tempDir, "CurrentSection.json");

            LogStatus("Bridge Section Transfer Tool initialized");
            LogStatus($"Using temp file: {_tempJsonPath}");

            // Check if temp file exists from previous session
            if (File.Exists(_tempJsonPath))
            {
                LogStatus("Found existing temp file from previous session");
                LoadCurrentSection();
            }
        }

        private void btnExtractFromCivil3D_Click(object sender, EventArgs e)
        {
            try
            {
                LogStatus("Starting extraction from Civil 3D...");
                LogStatus($"Will save to: {_tempJsonPath}");

                // TODO: Implement Civil 3D extraction via COM
                // For now, show message that this needs Civil 3D to be running
                LogStatus("ERROR: Civil 3D extraction not yet implemented in standalone app.");
                LogStatus("");
                LogStatus("Workaround:");
                LogStatus("1. Load the plugin in Civil 3D using NETLOAD");
                LogStatus("2. Run the 'ExportDeckSection' command");
                LogStatus($"3. Save the file to: {_tempJsonPath}");
                LogStatus("4. Click 'Import to CSiBridge' button below");

                // Copy path to clipboard for convenience
                Clipboard.SetText(_tempJsonPath);
                LogStatus("");
                LogStatus("Temp file path copied to clipboard!");

                MessageBox.Show(
                    "Civil 3D extraction is not yet implemented in the standalone app.\n\n" +
                    $"Please save your exported JSON to:\n{_tempJsonPath}\n\n" +
                    "(Path has been copied to clipboard)\n\n" +
                    "Then click 'Import to CSiBridge' button.",
                    "Save to Temp Location",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnImportToCSiBridge_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(_tempJsonPath))
                {
                    LogStatus("ERROR: No temp file found!");
                    MessageBox.Show(
                        $"Temp file not found at:\n{_tempJsonPath}\n\n" +
                        "Please export from Civil 3D first.",
                        "File Not Found",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                LogStatus($"Loading JSON file: {_tempJsonPath}");

                // Load the JSON file
                var serializer = new DeckSectionJsonSerializer();
                DeckSection section = serializer.DeserializeFromFile(_tempJsonPath);

                LogStatus($"Loaded section: {section.Name}");
                LogStatus($"  Exterior vertices: {section.ExteriorBoundary.Points.Count}");
                LogStatus($"  Interior voids: {section.InteriorVoids.Count}");
                LogStatus($"  Area: {section.Area:F4}");

                // Connect to CSiBridge
                LogStatus("Connecting to CSiBridge...");

                // TODO: Implement CSiBridge import via COM
                // For now, show message
                LogStatus("ERROR: CSiBridge import via COM not yet implemented.");
                LogStatus("");
                LogStatus("Workaround:");
                LogStatus("1. Open CSiBridge with a model");
                LogStatus("2. Use File > Import > Plugin");
                LogStatus("3. Browse to: BridgeSectionTransfer.CSiBridge.dll");
                LogStatus($"4. Select the temp file: {_tempJsonPath}");

                MessageBox.Show(
                    "CSiBridge import via COM automation is not yet implemented.\n\n" +
                    "To import to CSiBridge:\n" +
                    "1. Open CSiBridge with a model\n" +
                    "2. Use File > Import > Plugin\n" +
                    "3. Select the BridgeSectionTransfer.CSiBridge.dll plugin\n" +
                    $"4. Choose the temp file:\n{_tempJsonPath}",
                    "Use CSiBridge Plugin",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR: {ex.Message}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadCurrentSection()
        {
            try
            {
                var serializer = new DeckSectionJsonSerializer();
                DeckSection section = serializer.DeserializeFromFile(_tempJsonPath);

                txtJsonPath.Text = $"{section.Name} (from temp file)";
                LogStatus($"Loaded section: {section.Name}");
                LogStatus($"  Exterior: {section.ExteriorBoundary.Points.Count} vertices");
                LogStatus($"  Voids: {section.InteriorVoids.Count}");
                LogStatus($"  Area: {section.Area:F4}");
                LogStatus("Ready to import to CSiBridge");
            }
            catch (Exception ex)
            {
                LogStatus($"ERROR loading temp file: {ex.Message}");
            }
        }

        private void LogStatus(string message)
        {
            txtStatus.AppendText($"{DateTime.Now:HH:mm:ss} - {message}\r\n");
            txtStatus.SelectionStart = txtStatus.Text.Length;
            txtStatus.ScrollToCaret();
        }
    }
}
