namespace BridgeSectionTransfer.Console
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnExtractFromCivil3D;
        private System.Windows.Forms.Button btnImportToCSiBridge;
        private System.Windows.Forms.TextBox txtJsonPath;
        private System.Windows.Forms.TextBox txtStatus;
        private System.Windows.Forms.Label lblJsonPath;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnExtractFromCivil3D = new System.Windows.Forms.Button();
            this.btnImportToCSiBridge = new System.Windows.Forms.Button();
            this.txtJsonPath = new System.Windows.Forms.TextBox();
            this.txtStatus = new System.Windows.Forms.TextBox();
            this.lblJsonPath = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.SuspendLayout();
            //
            // groupBox1
            //
            this.groupBox1.Controls.Add(this.btnExtractFromCivil3D);
            this.groupBox1.Location = new System.Drawing.Point(12, 12);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(760, 80);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Step 1: Extract from Civil 3D";
            //
            // btnExtractFromCivil3D
            //
            this.btnExtractFromCivil3D.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.btnExtractFromCivil3D.Location = new System.Drawing.Point(20, 25);
            this.btnExtractFromCivil3D.Name = "btnExtractFromCivil3D";
            this.btnExtractFromCivil3D.Size = new System.Drawing.Size(720, 40);
            this.btnExtractFromCivil3D.TabIndex = 0;
            this.btnExtractFromCivil3D.Text = "Extract Bridge Deck Section from Civil 3D";
            this.btnExtractFromCivil3D.UseVisualStyleBackColor = true;
            this.btnExtractFromCivil3D.Click += new System.EventHandler(this.btnExtractFromCivil3D_Click);
            //
            // lblJsonPath
            //
            this.lblJsonPath.AutoSize = true;
            this.lblJsonPath.Location = new System.Drawing.Point(12, 105);
            this.lblJsonPath.Name = "lblJsonPath";
            this.lblJsonPath.Size = new System.Drawing.Size(86, 13);
            this.lblJsonPath.TabIndex = 1;
            this.lblJsonPath.Text = "Current Section:";
            //
            // txtJsonPath
            //
            this.txtJsonPath.Location = new System.Drawing.Point(12, 121);
            this.txtJsonPath.Name = "txtJsonPath";
            this.txtJsonPath.ReadOnly = true;
            this.txtJsonPath.Size = new System.Drawing.Size(760, 20);
            this.txtJsonPath.TabIndex = 2;
            //
            // groupBox2
            //
            this.groupBox2.Controls.Add(this.btnImportToCSiBridge);
            this.groupBox2.Location = new System.Drawing.Point(12, 155);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(760, 80);
            this.groupBox2.TabIndex = 4;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Step 2: Import to CSiBridge";
            //
            // btnImportToCSiBridge
            //
            this.btnImportToCSiBridge.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.btnImportToCSiBridge.Location = new System.Drawing.Point(20, 25);
            this.btnImportToCSiBridge.Name = "btnImportToCSiBridge";
            this.btnImportToCSiBridge.Size = new System.Drawing.Size(720, 40);
            this.btnImportToCSiBridge.TabIndex = 0;
            this.btnImportToCSiBridge.Text = "Import Section to CSiBridge";
            this.btnImportToCSiBridge.UseVisualStyleBackColor = true;
            this.btnImportToCSiBridge.Click += new System.EventHandler(this.btnImportToCSiBridge_Click);
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(12, 250);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(40, 13);
            this.lblStatus.TabIndex = 5;
            this.lblStatus.Text = "Status:";
            //
            // txtStatus
            //
            this.txtStatus.BackColor = System.Drawing.Color.Black;
            this.txtStatus.Font = new System.Drawing.Font("Consolas", 9F);
            this.txtStatus.ForeColor = System.Drawing.Color.Lime;
            this.txtStatus.Location = new System.Drawing.Point(12, 266);
            this.txtStatus.Multiline = true;
            this.txtStatus.Name = "txtStatus";
            this.txtStatus.ReadOnly = true;
            this.txtStatus.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtStatus.Size = new System.Drawing.Size(760, 200);
            this.txtStatus.TabIndex = 6;
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 481);
            this.Controls.Add(this.txtStatus);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.txtJsonPath);
            this.Controls.Add(this.lblJsonPath);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Bridge Section Transfer Tool";
            this.groupBox1.ResumeLayout(false);
            this.groupBox2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
