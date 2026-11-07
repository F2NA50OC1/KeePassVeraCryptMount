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
// VeraCryptMountForm.cs - the per-entry "VeraCrypt Mount volume" dialog
// -------------------------------------------------------------------------------------------------
// BUTTONS
//   Apply  - validates and saves the settings into the entry right away (callback to the plugin
//            class, which creates a history backup); the dialog stays open.
//   Mount  - validates; if valid the dialog closes with OK and the plugin saves and mounts exactly
//            what the dialog shows (CurrentSettings). Invalid input keeps the dialog open.
//   Cancel / Esc - closes without saving (anything saved with Apply stays saved).
//
// CONTROLS -> SETTINGS
//   All values go through MountSettings (field names: EntryStrings). The layout lives in
//   VeraCryptMountForm.Designer.cs (Visual Studio designer code, edited by hand in 2.0.0: tool tips
//   are set in the constructor below from LanguageTexts, there is no .resx).
//   Linux: no label / removable / beep in VeraCrypt for Linux. These controls are hidden or
//   disabled, but their stored values are kept unchanged when saving (the same database may be
//   used on Windows too).
//
// HISTORY
//   1.0.x: Mount used the previously saved settings; Apply + Cancel lost the changes; the PIM box
//          showed a message box for every wrong key; "Use PIM"/"TrueCrypt mode" toggled the other
//          controls blindly (could get out of sync); the entry's drive letter was replaced by
//          "Auto" when it was in use at that moment; the label of an entry was erased when saved
//          on Linux; a new icon handle was leaked each time the dialog opened.
//   2.0.0 (2026-11): all of the above fixed; KDF list = VeraCrypt 1.26.31's list (KdfAlgorithms);
//          Esc closes the dialog.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Windows.Forms;

    using KeePass.UI;

    using KeePassLib;
    using KeePassLib.Utility;

    internal partial class VeraCryptMountForm : Form
    {
        private readonly Action<MountSettings> applyCallback;
        private bool loading;

        /// <param name="applyCallback">Saves the settings when "Apply" is clicked.</param>
        internal VeraCryptMountForm(Action<MountSettings> applyCallback)
        {
            this.applyCallback = applyCallback;

            InitializeComponent();

            this.CancelButton = this.btnCancel;

            this.tipInfo.SetToolTip(this.hideDialogCheckBox, LanguageTexts.HideDialogToolTip);
            this.tipInfo.SetToolTip(this.labelTextBox, LanguageTexts.LabelToolTip);
            this.tipInfo.SetToolTip(this.autoTypeCheckBox, LanguageTexts.AutoTypeToolTip);
            this.tipInfo.SetToolTip(this.truecryptCheckBox, LanguageTexts.TrueCryptToolTip);
            this.tipInfo.SetToolTip(this.volumeTextBox, LanguageTexts.VolumeToolTip);
            this.tipInfo.SetToolTip(this.keyFilesTextBox, LanguageTexts.KeyFilesToolTip);
            this.tipInfo.SetToolTip(this.PIMtextBox, LanguageTexts.PimToolTip);
            this.tipInfo.SetToolTip(this.UsePIMcheckBox, LanguageTexts.PimToolTip);
            this.tipInfo.SetToolTip(this.PKCS5comboBox, LanguageTexts.KdfToolTip);
            this.tipInfo.SetToolTip(this.PKCS5label, LanguageTexts.KdfToolTip);

            // Image buttons: short text if a KeePass icon is missing (see Resources).
            this.button1.Text = Resources.FallbackText(this.button1.Image, "...");
            this.button2.Text = Resources.FallbackText(this.button2.Image, "...");
            this.btnReloadDrives.Text = Resources.FallbackText(this.btnReloadDrives.BackgroundImage, "R");

            this.PKCS5comboBox.Items.Clear();
            this.PKCS5comboBox.Items.AddRange(KdfAlgorithms.All);
            this.PKCS5comboBox.SelectedIndex = 0;

            this.FillDriveLetters(string.Empty);

            if (Platform.IsUnix)
            {
                this.labelTextBox.Visible = false;
                this.labelVolume.Visible = false;
                this.removableCheckBox.Enabled = false;
                this.beepCheckBox.Enabled = false;
            }
        }

        /// <summary>The settings shown in the dialog when it was closed with "Mount".</summary>
        internal MountSettings CurrentSettings { get; private set; }

        public void ReadFrom(PwEntry entry)
        {
            MountSettings s = MountSettings.ReadFrom(entry);
            this.loading = true;
            try
            {
                this.volumeTextBox.Text = s.Volume;
                this.keyFilesTextBox.Text = s.KeyFiles;
                this.labelTextBox.Text = s.Label; // also on Linux (hidden there), to keep the value
                this.FillDriveLetters(s.DriveLetter);

                this.readonlyCheckBox.Checked = s.ReadOnly;
                this.timestampCheckBox.Checked = s.Timestamp;
                this.removableCheckBox.Checked = s.Removable;
                this.beepCheckBox.Checked = s.Beep;
                this.explorerCheckBox.Checked = s.Explore;
                this.hideDialogCheckBox.Checked = s.HideDialog;
                this.autoTypeCheckBox.Checked = s.AutoType;
                this.truecryptCheckBox.Checked = s.TrueCryptMode;

                this.UsePIMcheckBox.Checked = s.Pim.Length > 0;
                this.PIMtextBox.Text = s.Pim;

                KdfChoice kdf = KdfAlgorithms.FromStoredValue(s.Kdf);
                if (kdf == null)
                {
                    // Keep an unknown stored value visible instead of silently replacing it.
                    kdf = new KdfChoice(s.Kdf, "Unknown: " + s.Kdf, null, null, null, false);
                    this.PKCS5comboBox.Items.Add(kdf);
                }

                this.PKCS5comboBox.SelectedItem = kdf;
                this.UpdateControlStates();
            }
            finally
            {
                this.loading = false;
            }

            this.btnApply.Enabled = false;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Resources.FormIcon != null)
            {
                this.Icon = Resources.FormIcon;
            }

            this.bannerPanel.BackgroundImage = BannerFactory.CreateBanner(
                this.bannerPanel.Width,
                this.bannerPanel.Height,
                BannerStyle.Default,
                null, // no icon: the plugin has none of its own
                LanguageTexts.BannerPluginTitleText,
                LanguageTexts.BannerMountFormDescriptionText);

            this.btnApply.Click += this.OnApplyButtonClicked;
            this.btnOK.Click += this.OnMountButtonClicked;

            GlobalWindowManager.AddWindow(this); // KeePass closes registered dialogs when it locks
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            this.btnApply.Click -= this.OnApplyButtonClicked;
            this.btnOK.Click -= this.OnMountButtonClicked;

            GlobalWindowManager.RemoveWindow(this);

            base.OnFormClosed(e);
        }

        /// <summary>Reads the controls; shows a message and returns null if something is invalid.</summary>
        private MountSettings TryGetSettings()
        {
            var s = new MountSettings();
            s.Volume = this.volumeTextBox.Text.Trim();
            s.KeyFiles = this.keyFilesTextBox.Text.Trim();
            s.Label = this.labelTextBox.Text.Trim();
            s.DriveLetter = this.SelectedDriveLetter();
            s.ReadOnly = this.readonlyCheckBox.Checked;
            s.Timestamp = this.timestampCheckBox.Checked;
            s.Removable = this.removableCheckBox.Checked;
            s.Beep = this.beepCheckBox.Checked;
            s.Explore = this.explorerCheckBox.Checked;
            s.HideDialog = this.hideDialogCheckBox.Checked;
            s.AutoType = this.autoTypeCheckBox.Checked;
            s.TrueCryptMode = this.truecryptCheckBox.Checked;

            var kdf = this.PKCS5comboBox.SelectedItem as KdfChoice;
            s.Kdf = (kdf == null) ? string.Empty : kdf.StoredValue;

            s.Pim = string.Empty;
            string pimText = this.PIMtextBox.Text.Trim();
            if (this.UsePIMcheckBox.Checked && pimText.Length > 0 && pimText.TrimStart('0').Length > 0)
            {
                int pim;
                if (!MountSettings.TryParsePim(pimText, out pim))
                {
                    this.ShowProblem(LanguageTexts.InvalidPim, this.PIMtextBox);
                    return null;
                }

                s.Pim = pim.ToString(CultureInfo.InvariantCulture);
            }

            if (!Platform.IsUnix && !VeraCryptArguments.IsValidLabel(s.Label))
            {
                this.ShowProblem(LanguageTexts.InvalidLabel, this.labelTextBox);
                return null;
            }

            if (s.Volume.Length == 0)
            {
                this.ShowProblem(LanguageTexts.NoVolumeSpecified, this.volumeTextBox);
                return null;
            }

            return s;
        }

        private void ShowProblem(string message, Control control)
        {
            MessageService.ShowWarning(LanguageTexts.MountErrorTitle, message);
            if (control != null && control.Visible && control.Enabled)
            {
                control.Focus();
            }
        }

        private void OnMountButtonClicked(object sender, EventArgs e)
        {
            MountSettings s = this.TryGetSettings();
            if (s == null)
            {
                this.DialogResult = DialogResult.None; // keep the dialog open
                return;
            }

            this.CurrentSettings = s;
        }

        private void OnApplyButtonClicked(object sender, EventArgs e)
        {
            MountSettings s = this.TryGetSettings();
            if (s == null)
            {
                return;
            }

            if (this.applyCallback != null)
            {
                this.applyCallback(s);
            }

            this.btnApply.Enabled = false;
        }

        private void OnAnyOptionChanged(object sender, EventArgs e)
        {
            if (!this.loading)
            {
                this.btnApply.Enabled = true;
            }
        }

        /// <summary>button1 = volume ("..." next to the volume box), button2 = key files.</summary>
        private void OnFileOpenDialogButtonClicked(object sender, EventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.CheckFileExists = true;
                dialog.RestoreDirectory = true;
                dialog.Multiselect = sender == this.button2;

                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                if (sender == this.button2)
                {
                    this.keyFilesTextBox.Text = string.Join(";", dialog.FileNames); // format of 1.0.x
                }
                else
                {
                    this.volumeTextBox.Text = dialog.FileName;
                }
            }
        }

        private void OnReloadDrivesButtonClicked(object sender, EventArgs e)
        {
            this.FillDriveLetters(this.SelectedDriveLetter());
        }

        /// <summary>Free drive letters, plus the entry's own letter even if it is in use right now.</summary>
        private void FillDriveLetters(string selectedLetter)
        {
            this.driveComboBox.BeginUpdate();
            try
            {
                this.driveComboBox.Items.Clear();
                this.driveComboBox.Items.Add(new AutoDriveLetterComboBoxItem());

                var letters = LogicalDriveLetters.FreeDriveLetters.ToList();
                string wanted = string.IsNullOrEmpty(selectedLetter) ? null : selectedLetter + ":\\";
                if (wanted != null && !letters.Contains(wanted))
                {
                    letters.Add(wanted);
                    letters.Sort(StringComparer.Ordinal);
                }

                foreach (string letter in letters)
                {
                    this.driveComboBox.Items.Add(letter);
                }

                if (wanted != null)
                {
                    this.driveComboBox.SelectedItem = wanted;
                }
                else
                {
                    this.driveComboBox.SelectedIndex = 0;
                }
            }
            finally
            {
                this.driveComboBox.EndUpdate();
            }
        }

        private string SelectedDriveLetter()
        {
            return MountSettings.NormalizeDriveLetter(this.driveComboBox.SelectedItem as string);
        }

        private void PIMtextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && (e.KeyChar < '0' || e.KeyChar > '9'))
            {
                e.Handled = true; // digits only
            }
        }

        private void PIMtextBox_TextChanged(object sender, EventArgs e)
        {
            // Pasted text: keep only the digits.
            string text = this.PIMtextBox.Text;
            var digits = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (c >= '0' && c <= '9')
                {
                    digits.Append(c);
                }
            }

            if (digits.Length != text.Length)
            {
                this.PIMtextBox.Text = digits.ToString(); // raises TextChanged once more, then stable
                this.PIMtextBox.SelectionStart = this.PIMtextBox.Text.Length;
            }

            this.OnAnyOptionChanged(sender, e);
        }

        private void UsePIM_CheckedChanged(object sender, EventArgs e)
        {
            this.UpdateControlStates();
            this.OnAnyOptionChanged(sender, e);
        }

        private void truecrypt_CheckedChanged(object sender, EventArgs e)
        {
            this.UpdateControlStates();
            this.OnAnyOptionChanged(sender, e);
        }

        /// <summary>Enabled/visible states derived from the check boxes.</summary>
        private void UpdateControlStates()
        {
            bool veraCryptMode = !this.truecryptCheckBox.Checked; // TrueCrypt mode: no KDF choice, no PIM
            this.PKCS5label.Enabled = veraCryptMode;
            this.PKCS5comboBox.Enabled = veraCryptMode;
            this.UsePIMcheckBox.Enabled = veraCryptMode;
            this.PIMtextBox.Enabled = veraCryptMode;
            this.PIMtextBox.Visible = this.UsePIMcheckBox.Checked;
        }
    }
}
