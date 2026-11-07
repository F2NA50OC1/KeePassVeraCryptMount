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
// VeraCryptMountForm.Designer.cs - layout of the mount dialog (Windows Forms designer code)
// -------------------------------------------------------------------------------------------------
// Edited by hand in 2.0.0: no .resx file (KeePass' icons come from Resources, tool tips are set in
// VeraCryptMountForm's constructor from LanguageTexts), label "PRF / KDF:", the KDF list is filled
// from KdfAlgorithms at run time. Event handler names used here must exist in VeraCryptMountForm.
// After editing the form in the Visual Studio designer, check that it did not add a .resx file or
// undo these edits (PLGX: every file must be listed in the .csproj).
// =================================================================================================

namespace KeePassVeraCryptMount
{
    partial class VeraCryptMountForm
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
            this.bannerPanel = new System.Windows.Forms.Panel();
            this.m_lblSeparator = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOK = new System.Windows.Forms.Button();
            this.mountOptionsGroupBox = new System.Windows.Forms.GroupBox();
            this.mountOptionsLayoutPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.readonlyCheckBox = new System.Windows.Forms.CheckBox();
            this.removableCheckBox = new System.Windows.Forms.CheckBox();
            this.timestampCheckBox = new System.Windows.Forms.CheckBox();
            this.beepCheckBox = new System.Windows.Forms.CheckBox();
            this.explorerCheckBox = new System.Windows.Forms.CheckBox();
            this.autoTypeCheckBox = new System.Windows.Forms.CheckBox();
            this.truecryptCheckBox = new System.Windows.Forms.CheckBox();
            this.PKCS5label = new System.Windows.Forms.Label();
            this.PKCS5comboBox = new System.Windows.Forms.ComboBox();
            this.UsePIMcheckBox = new System.Windows.Forms.CheckBox();
            this.PIMtextBox = new System.Windows.Forms.TextBox();
            this.volumeTextBox = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.driveComboBox = new System.Windows.Forms.ComboBox();
            this.tipInfo = new System.Windows.Forms.ToolTip(this.components);
            this.btnReloadDrives = new System.Windows.Forms.Button();
            this.hideDialogCheckBox = new System.Windows.Forms.CheckBox();
            this.keyFilesTextBox = new System.Windows.Forms.TextBox();
            this.labelTextBox = new System.Windows.Forms.TextBox();
            this.btnApply = new System.Windows.Forms.Button();
            this.tipWarning = new System.Windows.Forms.ToolTip(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.label2 = new System.Windows.Forms.Label();
            this.labelVolume = new System.Windows.Forms.Label();
            this.mountOptionsGroupBox.SuspendLayout();
            this.mountOptionsLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // bannerPanel
            // 
            this.bannerPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.bannerPanel.Location = new System.Drawing.Point(0, 0);
            this.bannerPanel.Name = "bannerPanel";
            this.bannerPanel.Size = new System.Drawing.Size(482, 70);
            this.bannerPanel.TabIndex = 0;
            // 
            // m_lblSeparator
            // 
            this.m_lblSeparator.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.m_lblSeparator.Location = new System.Drawing.Point(3, 356);
            this.m_lblSeparator.Name = "m_lblSeparator";
            this.m_lblSeparator.Size = new System.Drawing.Size(475, 2);
            this.m_lblSeparator.TabIndex = 40;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(311, 361);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "&Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(396, 361);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "&Mount";
            this.btnOK.UseVisualStyleBackColor = true;
            // 
            // mountOptionsGroupBox
            // 
            this.mountOptionsGroupBox.Controls.Add(this.mountOptionsLayoutPanel);
            this.mountOptionsGroupBox.Location = new System.Drawing.Point(3, 233);
            this.mountOptionsGroupBox.Name = "mountOptionsGroupBox";
            this.mountOptionsGroupBox.Size = new System.Drawing.Size(468, 114);
            this.mountOptionsGroupBox.TabIndex = 11;
            this.mountOptionsGroupBox.TabStop = false;
            this.mountOptionsGroupBox.Text = "Mount options";
            // 
            // mountOptionsLayoutPanel
            // 
            this.mountOptionsLayoutPanel.Controls.Add(this.readonlyCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.removableCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.timestampCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.beepCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.autoTypeCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.explorerCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.truecryptCheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.PKCS5label);
            this.mountOptionsLayoutPanel.Controls.Add(this.PKCS5comboBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.UsePIMcheckBox);
            this.mountOptionsLayoutPanel.Controls.Add(this.PIMtextBox);
            this.mountOptionsLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mountOptionsLayoutPanel.Location = new System.Drawing.Point(3, 17);
            this.mountOptionsLayoutPanel.Name = "mountOptionsLayoutPanel";
            this.mountOptionsLayoutPanel.Size = new System.Drawing.Size(462, 94);
            this.mountOptionsLayoutPanel.TabIndex = 11;
            // 
            // readonlyCheckBox
            // 
            this.readonlyCheckBox.Location = new System.Drawing.Point(3, 3);
            this.readonlyCheckBox.Name = "readonlyCheckBox";
            this.readonlyCheckBox.Size = new System.Drawing.Size(92, 24);
            this.readonlyCheckBox.TabIndex = 12;
            this.readonlyCheckBox.Text = "Readonly";
            this.tipInfo.SetToolTip(this.readonlyCheckBox, "Mount volume as read-only. ");
            this.readonlyCheckBox.UseVisualStyleBackColor = true;
            this.readonlyCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // removableCheckBox
            // 
            this.removableCheckBox.Location = new System.Drawing.Point(101, 3);
            this.removableCheckBox.Name = "removableCheckBox";
            this.removableCheckBox.Size = new System.Drawing.Size(123, 24);
            this.removableCheckBox.TabIndex = 13;
            this.removableCheckBox.Text = "Removable";
            this.tipInfo.SetToolTip(this.removableCheckBox, "Mount volume as removable medium.");
            this.removableCheckBox.UseVisualStyleBackColor = true;
            this.removableCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // timestampCheckBox
            // 
            this.timestampCheckBox.Location = new System.Drawing.Point(230, 3);
            this.timestampCheckBox.Name = "timestampCheckBox";
            this.timestampCheckBox.Size = new System.Drawing.Size(131, 24);
            this.timestampCheckBox.TabIndex = 14;
            this.timestampCheckBox.Text = "TimeStamp";
            this.tipInfo.SetToolTip(this.timestampCheckBox, "Do not Preserve TimeStamp of Archive (TimeStamp of Archive file is updated at eac" +
        "h save).\r\n ");
            this.timestampCheckBox.UseVisualStyleBackColor = true;
            this.timestampCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // beepCheckBox
            // 
            this.beepCheckBox.Location = new System.Drawing.Point(367, 3);
            this.beepCheckBox.Name = "beepCheckBox";
            this.beepCheckBox.Size = new System.Drawing.Size(55, 24);
            this.beepCheckBox.TabIndex = 17;
            this.beepCheckBox.Text = "Beep";
            this.tipInfo.SetToolTip(this.beepCheckBox, "Beep after a volume has been successfully mounted or dismounted. ");
            this.beepCheckBox.UseVisualStyleBackColor = true;
            this.beepCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // explorerCheckBox
            // 
            this.explorerCheckBox.Location = new System.Drawing.Point(101, 33);
            this.explorerCheckBox.Name = "explorerCheckBox";
            this.explorerCheckBox.Size = new System.Drawing.Size(123, 24);
            this.explorerCheckBox.TabIndex = 19;
            this.explorerCheckBox.Text = "Open Explorer";
            this.tipInfo.SetToolTip(this.explorerCheckBox, "Open an Explorer window after a volume has been mounted.");
            this.explorerCheckBox.UseVisualStyleBackColor = true;
            this.explorerCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // autoTypeCheckBox
            // 
            this.autoTypeCheckBox.Location = new System.Drawing.Point(3, 33);
            this.autoTypeCheckBox.Name = "autoTypeCheckBox";
            this.autoTypeCheckBox.Size = new System.Drawing.Size(92, 24);
            this.autoTypeCheckBox.TabIndex = 18;
            this.autoTypeCheckBox.Text = "Auto-Type";
            this.autoTypeCheckBox.UseVisualStyleBackColor = true;
            this.autoTypeCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // truecryptCheckBox
            // 
            this.truecryptCheckBox.Location = new System.Drawing.Point(230, 33);
            this.truecryptCheckBox.Name = "truecryptCheckBox";
            this.truecryptCheckBox.Size = new System.Drawing.Size(210, 24);
            this.truecryptCheckBox.TabIndex = 20;
            this.truecryptCheckBox.Text = "TrueCrypt Mode";
            this.truecryptCheckBox.UseVisualStyleBackColor = true;
            this.truecryptCheckBox.CheckedChanged += new System.EventHandler(this.truecrypt_CheckedChanged);
            // 
            // PKCS5label
            // 
            this.PKCS5label.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.PKCS5label.AutoSize = true;
            this.PKCS5label.Location = new System.Drawing.Point(3, 67);
            this.PKCS5label.Margin = new System.Windows.Forms.Padding(3, 0, 3, 3);
            this.PKCS5label.Name = "PKCS5label";
            this.PKCS5label.Size = new System.Drawing.Size(68, 13);
            this.PKCS5label.TabIndex = 40;
            this.PKCS5label.Text = "PRF / KDF:";
            this.PKCS5label.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tipInfo.SetToolTip(this.PKCS5label, "If Autodetection, VeraCrypt will try all possible PRF algorithms thus lengthening" +
        " the mount operation time.");
            // 
            // PKCS5comboBox
            // 
            this.PKCS5comboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.PKCS5comboBox.FormattingEnabled = true;
            // Items are filled in the constructor from KdfAlgorithms.All.
            this.PKCS5comboBox.Location = new System.Drawing.Point(77, 63);
            this.PKCS5comboBox.Name = "PKCS5comboBox";
            this.PKCS5comboBox.Size = new System.Drawing.Size(147, 21);
            this.PKCS5comboBox.DropDownWidth = 260;
            this.PKCS5comboBox.TabIndex = 21;
            this.tipInfo.SetToolTip(this.PKCS5comboBox, "If Autodetection, VeraCrypt will try all possible PRF algorithms thus lengthening" +
        " the mount operation time.");
            this.PKCS5comboBox.SelectedIndexChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // UsePIMcheckBox
            // 
            this.UsePIMcheckBox.Location = new System.Drawing.Point(230, 63);
            this.UsePIMcheckBox.Name = "UsePIMcheckBox";
            this.UsePIMcheckBox.Size = new System.Drawing.Size(81, 24);
            this.UsePIMcheckBox.TabIndex = 22;
            this.UsePIMcheckBox.Text = "Use PIM";
            this.UsePIMcheckBox.UseVisualStyleBackColor = true;
            this.UsePIMcheckBox.CheckedChanged += new System.EventHandler(this.UsePIM_CheckedChanged);
            // 
            // PIMtextBox
            // 
            this.PIMtextBox.Location = new System.Drawing.Point(316, 63);
            this.PIMtextBox.Margin = new System.Windows.Forms.Padding(2, 3, 3, 0);
            this.PIMtextBox.MaxLength = 7;
            this.PIMtextBox.Name = "PIMtextBox";
            this.PIMtextBox.Size = new System.Drawing.Size(60, 21);
            this.PIMtextBox.TabIndex = 23;
            this.PIMtextBox.UseSystemPasswordChar = true;
            this.PIMtextBox.Visible = false;
            this.PIMtextBox.TextChanged += new System.EventHandler(this.PIMtextBox_TextChanged);
            this.PIMtextBox.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.PIMtextBox_KeyPress);
            // 
            // volumeTextBox
            // 
            this.volumeTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.volumeTextBox.Location = new System.Drawing.Point(6, 105);
            this.volumeTextBox.Name = "volumeTextBox";
            this.volumeTextBox.Size = new System.Drawing.Size(421, 21);
            this.volumeTextBox.TabIndex = 4;
            this.volumeTextBox.TextChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 89);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(167, 13);
            this.label1.TabIndex = 40;
            this.label1.Text = "Volume File | Partition | Harddrive";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(6, 181);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 13);
            this.label3.TabIndex = 40;
            this.label3.Text = "Drive letter";
            // 
            // driveComboBox
            // 
            this.driveComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.driveComboBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.driveComboBox.FormattingEnabled = true;
            this.driveComboBox.Location = new System.Drawing.Point(9, 197);
            this.driveComboBox.Name = "driveComboBox";
            this.driveComboBox.Size = new System.Drawing.Size(121, 21);
            this.driveComboBox.TabIndex = 8;
            this.driveComboBox.SelectionChangeCommitted += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // tipInfo
            // 
            this.tipInfo.AutoPopDelay = 30000;
            this.tipInfo.InitialDelay = 500;
            this.tipInfo.IsBalloon = true;
            this.tipInfo.ReshowDelay = 1000;
            this.tipInfo.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Info;
            this.tipInfo.ToolTipTitle = "Helpful information";
            // 
            // btnReloadDrives
            // 
            this.btnReloadDrives.BackgroundImage = global::KeePassVeraCryptMount.Resources.ReloadIcon;
            this.btnReloadDrives.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
            this.btnReloadDrives.Location = new System.Drawing.Point(136, 195);
            this.btnReloadDrives.Name = "btnReloadDrives";
            this.btnReloadDrives.Size = new System.Drawing.Size(35, 24);
            this.btnReloadDrives.TabIndex = 9;
            this.tipInfo.SetToolTip(this.btnReloadDrives, "Refresh free drive letters selection");
            this.btnReloadDrives.UseVisualStyleBackColor = true;
            this.btnReloadDrives.Click += new System.EventHandler(this.OnReloadDrivesButtonClicked);
            // 
            // hideDialogCheckBox
            // 
            this.hideDialogCheckBox.AutoSize = true;
            this.hideDialogCheckBox.Location = new System.Drawing.Point(104, 365);
            this.hideDialogCheckBox.Name = "hideDialogCheckBox";
            this.hideDialogCheckBox.Size = new System.Drawing.Size(126, 17);
            this.hideDialogCheckBox.TabIndex = 24;
            this.hideDialogCheckBox.Text = "Hide dialog next time";
            this.hideDialogCheckBox.UseVisualStyleBackColor = true;
            this.hideDialogCheckBox.CheckedChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // keyFilesTextBox
            // 
            this.keyFilesTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.keyFilesTextBox.Location = new System.Drawing.Point(6, 150);
            this.keyFilesTextBox.Name = "keyFilesTextBox";
            this.keyFilesTextBox.Size = new System.Drawing.Size(421, 21);
            this.keyFilesTextBox.TabIndex = 6;
            this.keyFilesTextBox.TextChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // labelTextBox
            // 
            this.labelTextBox.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelTextBox.Location = new System.Drawing.Point(201, 198);
            this.labelTextBox.MaxLength = 32;
            this.labelTextBox.Name = "labelTextBox";
            this.labelTextBox.Size = new System.Drawing.Size(226, 21);
            this.labelTextBox.TabIndex = 10;
            this.labelTextBox.TextChanged += new System.EventHandler(this.OnAnyOptionChanged);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(9, 361);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(80, 23);
            this.btnApply.TabIndex = 25;
            this.btnApply.Text = "&Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            // 
            // tipWarning
            // 
            this.tipWarning.AutoPopDelay = 30000;
            this.tipWarning.InitialDelay = 500;
            this.tipWarning.IsBalloon = true;
            this.tipWarning.ReshowDelay = 1000;
            this.tipWarning.ToolTipIcon = System.Windows.Forms.ToolTipIcon.Warning;
            this.tipWarning.ToolTipTitle = "Security issue warning";
            // 
            // button1
            // 
            this.button1.Image = global::KeePassVeraCryptMount.Resources.FolderIcon;
            this.button1.Location = new System.Drawing.Point(433, 102);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(35, 24);
            this.button1.TabIndex = 5;
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.OnFileOpenDialogButtonClicked);
            // 
            // button2
            // 
            this.button2.Image = global::KeePassVeraCryptMount.Resources.FolderIcon;
            this.button2.Location = new System.Drawing.Point(433, 147);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(35, 24);
            this.button2.TabIndex = 7;
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.OnFileOpenDialogButtonClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 132);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(54, 13);
            this.label2.TabIndex = 40;
            this.label2.Text = "KeyFile(s)";
            // 
            // labelVolume
            // 
            this.labelVolume.AutoSize = true;
            this.labelVolume.Location = new System.Drawing.Point(198, 181);
            this.labelVolume.Name = "labelVolume";
            this.labelVolume.Size = new System.Drawing.Size(158, 13);
            this.labelVolume.TabIndex = 40;
            this.labelVolume.Text = " Windows explorer volume label";
            // 
            // VeraCryptMountForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(482, 393);
            this.Controls.Add(this.labelVolume);
            this.Controls.Add(this.labelTextBox);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.keyFilesTextBox);
            this.Controls.Add(this.btnReloadDrives);
            this.Controls.Add(this.hideDialogCheckBox);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.driveComboBox);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.volumeTextBox);
            this.Controls.Add(this.mountOptionsGroupBox);
            this.Controls.Add(this.m_lblSeparator);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.bannerPanel);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "VeraCryptMountForm";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "VeraCrypt Mount volume";
            this.mountOptionsGroupBox.ResumeLayout(false);
            this.mountOptionsLayoutPanel.ResumeLayout(false);
            this.mountOptionsLayoutPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel bannerPanel;
        private System.Windows.Forms.Label m_lblSeparator;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.GroupBox mountOptionsGroupBox;
        private System.Windows.Forms.FlowLayoutPanel mountOptionsLayoutPanel;
        private System.Windows.Forms.CheckBox readonlyCheckBox;
        private System.Windows.Forms.CheckBox removableCheckBox;
        private System.Windows.Forms.TextBox volumeTextBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox driveComboBox;
        private System.Windows.Forms.ToolTip tipInfo;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.CheckBox hideDialogCheckBox;
        private System.Windows.Forms.CheckBox beepCheckBox;
        private System.Windows.Forms.CheckBox explorerCheckBox;
        private System.Windows.Forms.CheckBox autoTypeCheckBox;
        private System.Windows.Forms.CheckBox truecryptCheckBox;
        private System.Windows.Forms.Button btnReloadDrives;
        private System.Windows.Forms.ToolTip tipWarning;
        private System.Windows.Forms.TextBox keyFilesTextBox;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox timestampCheckBox;
        private System.Windows.Forms.CheckBox UsePIMcheckBox;
        private System.Windows.Forms.Label PKCS5label;
        private System.Windows.Forms.ComboBox PKCS5comboBox;
        private System.Windows.Forms.TextBox PIMtextBox;
        private System.Windows.Forms.TextBox labelTextBox;
        private System.Windows.Forms.Label labelVolume;
    }
}
