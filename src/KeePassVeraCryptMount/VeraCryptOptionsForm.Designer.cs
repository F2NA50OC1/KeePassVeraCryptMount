/*
  Plugin for KeePass Password Safe - The Open-Source Password Manager
  KeePassVeraCryptMount
  Copyright (C) 2015-2017 Frédéric Bourqui
  Copyright (C) 2026 KeePassVeraCryptMount contributors

  fork of:
  KeePassTrueCryptMount
  Copyright (C) 2010-2013 Patrick Schaller

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

// =================================================================================================
// VeraCryptOptionsForm.Designer.cs - layout of the options dialog (Windows Forms designer code)
// -------------------------------------------------------------------------------------------------
// Edited by hand in 2.0.0: no .resx file, wait time range 10-60 s (PluginOptions), texts updated,
// no links and no picture (1.0.x had "Donate" and "Help" links and a picture of the menu).
// Same notes as VeraCryptMountForm.Designer.cs.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    partial class VeraCryptOptionsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.txtVeraCryptExecutable = new System.Windows.Forms.TextBox();
            this.ofdVeraCryptExecutable = new System.Windows.Forms.OpenFileDialog();
            this.lblSeparator = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.tipRegistryButton = new System.Windows.Forms.ToolTip(this.components);
            this.btnRegistryResolve = new System.Windows.Forms.Button();
            this.picBanner = new System.Windows.Forms.PictureBox();
            this.btnOpenFileDialog = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.numericAutoTypeTimeout = new System.Windows.Forms.NumericUpDown();
            ((System.ComponentModel.ISupportInitialize)(this.picBanner)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericAutoTypeTimeout)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 77);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(137, 13);
            this.label1.TabIndex = 13;
            this.label1.Text = "VeraCrypt executable path";
            // 
            // txtVeraCryptExecutable
            // 
            this.txtVeraCryptExecutable.BackColor = System.Drawing.Color.Coral;
            this.txtVeraCryptExecutable.Location = new System.Drawing.Point(6, 93);
            this.txtVeraCryptExecutable.Name = "txtVeraCryptExecutable";
            this.txtVeraCryptExecutable.Size = new System.Drawing.Size(441, 21);
            this.txtVeraCryptExecutable.TabIndex = 12;
            this.txtVeraCryptExecutable.TextChanged += new System.EventHandler(this.OnVeraCryptPathTextChanged);
            // 
            // ofdVeraCryptExecutable
            // 
            this.ofdVeraCryptExecutable.FileName = "VeraCrypt.exe";
            this.ofdVeraCryptExecutable.Filter = "VeraCrypt program|VeraCrypt*.exe";
            this.ofdVeraCryptExecutable.RestoreDirectory = true;
            // 
            // lblSeparator
            // 
            this.lblSeparator.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lblSeparator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.lblSeparator.Location = new System.Drawing.Point(0, 234);
            this.lblSeparator.Name = "lblSeparator";
            this.lblSeparator.Size = new System.Drawing.Size(520, 11);
            this.lblSeparator.TabIndex = 15;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(454, 240);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 23);
            this.btnClose.TabIndex = 1;
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(372, 240);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 0;
            this.btnOk.Text = "&OK";
            this.btnOk.UseVisualStyleBackColor = true;
            this.btnOk.Click += new System.EventHandler(this.OnOkButtonClicked);
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(7, 125);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(522, 48);
            this.label2.TabIndex = 17;
            this.label2.Text = "Ctrl+M always opens the \"VeraCrypt Mount volume\" dialog for the selected entry, also for an entry without mount settings.";
            // 
            // tipRegistryButton
            // 
            this.tipRegistryButton.IsBalloon = true;
            this.tipRegistryButton.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.tipRegistryButton.ToolTipTitle = "Find VeraCrypt";
            // 
            // btnRegistryResolve
            // 
            this.btnRegistryResolve.Image = global::KeePassVeraCryptMount.Resources.FindIcon;
            this.btnRegistryResolve.Location = new System.Drawing.Point(494, 91);
            this.btnRegistryResolve.Name = "btnRegistryResolve";
            this.btnRegistryResolve.Size = new System.Drawing.Size(35, 23);
            this.btnRegistryResolve.TabIndex = 15;
            this.tipRegistryButton.SetToolTip(this.btnRegistryResolve, "Find the VeraCrypt program");
            this.btnRegistryResolve.UseVisualStyleBackColor = true;
            this.btnRegistryResolve.Click += new System.EventHandler(this.OnRegistryResolveButtonClicked);
            // 
            // picBanner
            // 
            this.picBanner.Location = new System.Drawing.Point(0, 0);
            this.picBanner.Name = "picBanner";
            this.picBanner.Size = new System.Drawing.Size(540, 70);
            this.picBanner.TabIndex = 19;
            this.picBanner.TabStop = false;
            // 
            // btnOpenFileDialog
            // 
            this.btnOpenFileDialog.Image = global::KeePassVeraCryptMount.Resources.FolderIcon;
            this.btnOpenFileDialog.Location = new System.Drawing.Point(453, 91);
            this.btnOpenFileDialog.Name = "btnOpenFileDialog";
            this.btnOpenFileDialog.Size = new System.Drawing.Size(35, 23);
            this.btnOpenFileDialog.TabIndex = 14;
            this.btnOpenFileDialog.UseVisualStyleBackColor = true;
            this.btnOpenFileDialog.Click += new System.EventHandler(this.OnButtonOpenFileDialogClick);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 180);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(235, 13);
            this.label4.TabIndex = 20;
            this.label4.Text = "Maximum wait for VeraCrypt's password window";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(69, 199);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(20, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = "ms";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(99, 197);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(310, 26);
            this.label6.TabIndex = 23;
            this.label6.Text = "Auto-Type starts as soon as the window is ready. Nothing is typed\r\nif it does not appear in this time (increase it on slow computers).";
            // 
            // numericAutoTypeTimeout
            // 
            this.numericAutoTypeTimeout.Increment = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numericAutoTypeTimeout.Location = new System.Drawing.Point(6, 197);
            this.numericAutoTypeTimeout.Maximum = new decimal(new int[] {
            60000,
            0,
            0,
            0});
            this.numericAutoTypeTimeout.Minimum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numericAutoTypeTimeout.Name = "numericAutoTypeTimeout";
            this.numericAutoTypeTimeout.Size = new System.Drawing.Size(63, 21);
            this.numericAutoTypeTimeout.TabIndex = 21;
            this.numericAutoTypeTimeout.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numericAutoTypeTimeout.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // VeraCryptOptionsForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(541, 272);
            this.Controls.Add(this.numericAutoTypeTimeout);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnRegistryResolve);
            this.Controls.Add(this.picBanner);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.lblSeparator);
            this.Controls.Add(this.btnOpenFileDialog);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtVeraCryptExecutable);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VeraCryptOptionsForm";
            this.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VeraCrypt Plugin Options";
            ((System.ComponentModel.ISupportInitialize)(this.picBanner)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericAutoTypeTimeout)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOpenFileDialog;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtVeraCryptExecutable;
        private System.Windows.Forms.OpenFileDialog ofdVeraCryptExecutable;
        private System.Windows.Forms.Label lblSeparator;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.PictureBox picBanner;
        private System.Windows.Forms.Button btnRegistryResolve;
        private System.Windows.Forms.ToolTip tipRegistryButton;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.NumericUpDown numericAutoTypeTimeout;
    }
}
